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

	private KeysRecord _keysRecord = new();

	public VimKeyInterceptor(IKeyboardHookManager keyboardHookManager) : base(keyboardHookManager) { }

	protected override HookAction Intercept(KeysArgs args)
	{
		return VimIntercept(args, DateTime.UtcNow);
	}

	internal HookAction VimIntercept(KeysArgs args, DateTime nowUtc)
	{
		TimeSpan? vimKeyDownDuration = _keysRecord.GetKeyDownDuration(VimKey, nowUtc);
		_keysRecord.Record(args, nowUtc);
		KeyModifierFlags modifiers = _keysRecord.GetKeyModifiersDown();

		if (_keysRecord.IsKeyDown(VimKey))
		{
			if (args.PressedState == KeyPressedState.Down && TryGetOutputForInput(modifiers, args.Key, out string? output))
			{
				OutputAction?.Invoke(output);
				return HookAction.SwallowKey;
			}
		}

		if (args.Key == VimKey)
		{
			TimeSpan? timeSinceLastBindingEvent = GetTimeSinceLastBindingEvent(nowUtc);
			bool wasBindingPressed = timeSinceLastBindingEvent < vimKeyDownDuration;
			if (!wasBindingPressed && args.PressedState == KeyPressedState.Up && vimKeyDownDuration < VimKeyDownMinDuration)
			{
				OutputAction?.Invoke(VimKey.ToSendKeysString());
			}
			return HookAction.SwallowKey;
		}

		return HookAction.ForwardKey;
	}

	private TimeSpan? GetTimeSinceLastBindingEvent(DateTime nowUtc)
	{
		IEnumerable<TimeSpan?> upDurations = VimBindings.Keys.Select(k => _keysRecord.GetKeyUpDuration(k.Item2, nowUtc));
		IEnumerable<TimeSpan?> downDurations = VimBindings.Keys.Select(k => _keysRecord.GetKeyDownDuration(k.Item2, nowUtc));

		List<TimeSpan> durations =
			upDurations
			.Concat(downDurations)
			.OfType<TimeSpan>()
			.ToList();

		return durations.Any() ? durations.Min() : null;
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
