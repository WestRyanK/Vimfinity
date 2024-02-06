using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Vimfinity;

internal abstract class KeyInterceptor : IDisposable
{
	protected abstract HookAction Intercept(KeysArgs args);
	private IKeyboardHookManager _keyboardHookManager;

	public KeyInterceptor(IKeyboardHookManager keyboardHookManager)
	{
		_keyboardHookManager = keyboardHookManager;
		_keyboardHookManager.AddHook(Intercept);
	}

	public void Dispose()
	{
		_keyboardHookManager.RemoveHook();
	}
}

internal class VimKeyInterceptor : KeyInterceptor
{
	public TimeSpan VimKeyDownMinDuration { get; set; } = TimeSpan.FromSeconds(.2f);

	public Keys VimKey { get; set; } = Keys.OemSemicolon;
	public Action<string>? OutputAction { get; set; } = SendKeys.Send;

	public IDictionary<(KeyModifierFlags, Keys), string> VimBindings { get; set; } = new Dictionary<(KeyModifierFlags, Keys), string>() {
		{ (KeyModifierFlags.Unspecified, Keys.H), "{Left}" },
		{ (KeyModifierFlags.Unspecified, Keys.J), "{Down}" },
		{ (KeyModifierFlags.Unspecified, Keys.K), "{Up}" },
		{ (KeyModifierFlags.Unspecified, Keys.L), "{Right}" },
		{ (KeyModifierFlags.Shift, Keys.X), "{Backspace}" },
		{ (KeyModifierFlags.None, Keys.X), "{Delete}" },
	};

	private KeysState _keysState = new();
	private bool _vimBindingPressed = false;

	public VimKeyInterceptor(IKeyboardHookManager keyboardHookManager) : base(keyboardHookManager) { }

	protected override HookAction Intercept(KeysArgs args)
	{
		return VimIntercept(args, DateTime.UtcNow);
	}

	internal HookAction VimIntercept(KeysArgs args, DateTime nowUtc)
	{
		TimeSpan? vimKeyDownDuration = _keysState.GetKeyDownDuration(VimKey, nowUtc);
		_keysState.Record(args, nowUtc);
		KeyModifierFlags modifiers = _keysState.GetKeyModifiersDown();

		if (_keysState.IsKeyDown(VimKey))
		{
			if (args.PressedState == KeyPressedState.Down && TryGetOutputForInput(modifiers, args.Key, out string? output))
			{
				_vimBindingPressed = true;
				OutputAction?.Invoke(output);
				return HookAction.SwallowKey;
			}
		}

		if (args.Key == VimKey)
		{
			if (!_vimBindingPressed && args.PressedState == KeyPressedState.Up && vimKeyDownDuration < VimKeyDownMinDuration)
			{
				OutputAction?.Invoke(VimKey.ToSendKeysString());
			}
			_vimBindingPressed = false;
			return HookAction.SwallowKey;
		}

		return HookAction.ForwardKey;
	}

	private bool TryGetOutputForInput(KeyModifierFlags modifiers, Keys key, [NotNullWhen(true)] out string? output)
	{
		if (VimBindings.TryGetValue((modifiers, key), out string? o1))
		{
			output = o1;
			return true;
		}
		if (VimBindings.TryGetValue((KeyModifierFlags.Unspecified, key), out string? o2))
		{
			output = o2;
			return true;
		}

		output = default;
		return false;
	}
}

internal class KeysState
{
	private Dictionary<Keys, DateTime> _downStartTimesUtc = new();
	private Dictionary<Keys, DateTime> _upTimesUtc = new();

	public void Record(KeysArgs args, DateTime nowUtc)
	{
		if (args.PressedState == KeyPressedState.Up)
		{
			_upTimesUtc[args.Key] = nowUtc;

			if (_downStartTimesUtc.ContainsKey(args.Key))
			{
				_downStartTimesUtc.Remove(args.Key);
			}
		}
		else if (args.PressedState == KeyPressedState.Down && !_downStartTimesUtc.ContainsKey(args.Key))
		{
			_downStartTimesUtc[args.Key] = nowUtc;
		}
	}

	public void Record(KeysArgs args)
	{
		Record(args, DateTime.UtcNow);
	}

	public TimeSpan? GetKeyDownDuration(Keys key, DateTime nowUtc)
	{
		if (GetKeyEvent(_downStartTimesUtc, key, true) is not DateTime downStartUtc)
		{
			return null;
		}

		return nowUtc - downStartUtc;
	}

	public TimeSpan? GetKeyDownDuration(Keys key)
	{
		return GetKeyDownDuration(key, DateTime.UtcNow);
	}

	public TimeSpan? GetKeyUpDuration(Keys key, DateTime nowUtc)
	{
		if (GetKeyEvent(_upTimesUtc, key, false) is not DateTime upTimeUtc)
		{
			return null;
		}

		return nowUtc - upTimeUtc;
	}

	public bool IsKeyDown(Keys key) => GetKeyEvent(_downStartTimesUtc, key, true) != null;

	public KeyModifierFlags GetKeyModifiersDown()
	{
		KeyModifierFlags modifiers = KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Control) ? KeyModifierFlags.Control : KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Shift) ? KeyModifierFlags.Shift : KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Alt) ? KeyModifierFlags.Alt : KeyModifierFlags.None;
		return modifiers;
	}

	private DateTime? GetKeyEvent(Dictionary<Keys, DateTime> events, Keys key, bool useOldest)
	{
		DateTime? getEvent(IEnumerable<Keys> keys)
		{
			var selectedEvents = keys
				.Select(k => events.TryGetValue(k, out DateTime eventTime) ? eventTime : (DateTime?)null)
				.OfType<DateTime>()
				.ToList();

			if (selectedEvents.Any())
			{
				return useOldest ? selectedEvents.Min() : selectedEvents.Max();
			}
			return null;
		}

		return key switch
		{
			Keys.Modifiers => getEvent([Keys.LMenu, Keys.RMenu, Keys.LShiftKey, Keys.RShiftKey, Keys.LControlKey, Keys.RControlKey]),
			Keys.Menu or Keys.Alt => getEvent([Keys.LMenu, Keys.RMenu]),
			Keys.ShiftKey or Keys.Shift => getEvent([Keys.LShiftKey, Keys.RShiftKey]),
			Keys.ControlKey or Keys.Control => getEvent([Keys.LControlKey, Keys.RControlKey]),
			_ => getEvent([key]),
		};
	}
}
