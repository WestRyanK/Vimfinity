using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Vimfinity;

internal abstract class KeyInterceptor : IDisposable
{
	protected abstract HookAction Intercept(KeysArgs args);

	public KeyInterceptor()
	{
		KeyboardHookManager.AddHook(Intercept);
	}

	public void Dispose()
	{
		KeyboardHookManager.RemoveHook();
	}
}

internal class VimKeyInterceptor : KeyInterceptor
{
	public TimeSpan VimKeyDownMinDuration { get; set; } = TimeSpan.FromSeconds(.15f);
	public Keys VimKey { get; set; } = Keys.OemSemicolon;

	private static readonly IDictionary<(KeyModifierFlags, Keys), string> _InputKeysToOutput = new Dictionary<(KeyModifierFlags, Keys), string>() {
		{ (KeyModifierFlags.Unspecified, Keys.H), "{Left}" },
		{ (KeyModifierFlags.Unspecified, Keys.J), "{Down}" },
		{ (KeyModifierFlags.Unspecified, Keys.K), "{Up}" },
		{ (KeyModifierFlags.Unspecified, Keys.L), "{Right}" },
		{ (KeyModifierFlags.Shift, Keys.X), "{Backspace}" },
		{ (KeyModifierFlags.None, Keys.X), "{Delete}" },
	};

	private KeysState _keysState = new();

	protected override HookAction Intercept(KeysArgs args)
	{
		TimeSpan? vimKeyDownDuration = _keysState.GetKeyDownDuration(VimKey);
		_keysState.Record(args);
		KeyModifierFlags modifiers = _keysState.GetKeyModifiersDown();

		if (vimKeyDownDuration >= VimKeyDownMinDuration)
		{
			if (args.PressedState == KeyPressedState.Down && TryGetOutputForInput(modifiers, args.Key, out string? output))
			{
				SendKeys.Send(output);
				return HookAction.SwallowKey;
			}
		}

		if (args.Key == VimKey)
		{
			Debug.WriteLine($"Key {args.Key} State {args.PressedState} Duration {vimKeyDownDuration}");
			if (args.PressedState == KeyPressedState.Up && vimKeyDownDuration < VimKeyDownMinDuration)
			{
				SendKeys.Send(VimKey.ToSendKeysString());
			}
			return HookAction.SwallowKey;
		}

		return HookAction.ForwardKey;
	}

	private bool TryGetOutputForInput(KeyModifierFlags modifiers, Keys key, [NotNullWhen(true)] out string? output)
	{
		if (_InputKeysToOutput.TryGetValue((modifiers, key), out string? o1))
		{
			output = o1;
			return true;
		}
		if (_InputKeysToOutput.TryGetValue((KeyModifierFlags.Unspecified, key), out string? o2))
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
	private Dictionary<Keys, DateTime> _keysToDownStartUtc = new();

	public void Record(KeysArgs args, DateTime nowUtc)
	{
		if (args.PressedState == KeyPressedState.Up && _keysToDownStartUtc.ContainsKey(args.Key))
		{
			_keysToDownStartUtc.Remove(args.Key);
		}
		else if (args.PressedState == KeyPressedState.Down && !_keysToDownStartUtc.ContainsKey(args.Key))
		{
			_keysToDownStartUtc[args.Key] = nowUtc;
		}
	}

	public void Record(KeysArgs args)
	{
		Record(args, DateTime.UtcNow);
	}

	public TimeSpan? GetKeyDownDuration(Keys key, DateTime nowUtc)
	{
		if (GetKeyDownStart(key) is not DateTime downStartUtc)
		{
			return null;
		}

		return nowUtc - downStartUtc;
	}

	public TimeSpan? GetKeyDownDuration(Keys key)
	{
		return GetKeyDownDuration(key, DateTime.UtcNow);
	}

	public bool IsKeyDown(Keys key) => GetKeyDownStart(key) != null;

	public KeyModifierFlags GetKeyModifiersDown()
	{
		KeyModifierFlags modifiers = KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Control) ? KeyModifierFlags.Control : KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Shift) ? KeyModifierFlags.Shift : KeyModifierFlags.None;
		modifiers |= IsKeyDown(Keys.Alt) ? KeyModifierFlags.Alt : KeyModifierFlags.None;
		return modifiers;
	}

	private DateTime? GetKeyDownStart(Keys key)
	{
		DateTime? getOldestDownStart(IEnumerable<Keys> keys)
		{
			var downStarts = keys
				.Select(k => _keysToDownStartUtc.TryGetValue(k, out DateTime start) ? start : (DateTime?)null)
				.OfType<DateTime>()
				.ToList();

			return downStarts.Any() ? downStarts.Min() : null;
		}

		return key switch
		{
			Keys.Modifiers => getOldestDownStart([Keys.LMenu, Keys.RMenu, Keys.LShiftKey, Keys.RShiftKey, Keys.LControlKey, Keys.RControlKey]),
			Keys.Alt => getOldestDownStart([Keys.LMenu, Keys.RMenu]),
			Keys.ShiftKey or Keys.Shift => getOldestDownStart([Keys.LShiftKey, Keys.RShiftKey]),
			Keys.ControlKey or Keys.Control => getOldestDownStart([Keys.LControlKey, Keys.RControlKey]),
			_ => getOldestDownStart([key]),
		};
	}
}
