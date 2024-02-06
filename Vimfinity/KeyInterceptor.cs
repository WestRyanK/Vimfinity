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
	public TimeSpan LayerKeyTappedTimeout { get; set; } = TimeSpan.FromSeconds(.2f);
	public TimeSpan ModifierReleasedRecentlyTimeout { get; set; } = TimeSpan.FromSeconds(.1f);

	public Keys LayerKey { get; set; } = Keys.OemSemicolon;
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
		return Intercept(args, DateTime.UtcNow);
	}

	internal HookAction Intercept(KeysArgs args, DateTime nowUtc)
	{
		TimeSpan? layerKeyDownDuration = _keysRecord.GetKeyDownDuration(LayerKey, nowUtc);
		_keysRecord.Record(args, nowUtc);
		KeyModifierFlags modifiers = _keysRecord.GetKeyModifiersDown();

		if (_keysRecord.IsKeyDown(LayerKey))
		{
			if (args.PressedState == KeyPressedState.Down && TryGetVimBinding(modifiers, args.Key, out string? output))
			{
				OutputAction?.Invoke(output);
				return HookAction.SwallowKey;
			}
		}

		if (args.Key == LayerKey)
		{
			TimeSpan? timeSinceLastBindingKeyEvent = GetTimeSinceLastBindingKeyEvent(nowUtc);
			bool wasBindingPressed = timeSinceLastBindingKeyEvent < layerKeyDownDuration;
			if (!wasBindingPressed && args.PressedState == KeyPressedState.Up && layerKeyDownDuration < LayerKeyTappedTimeout)
			{
				IEnumerable<Keys> recentModifiers = GetRecentlyReleasedModifiers(ModifierReleasedRecentlyTimeout, nowUtc);
				string modifiersString = string.Join(null, recentModifiers.Select(k => k.ToSendKeysString()));
				OutputAction?.Invoke($"{modifiersString}{LayerKey.ToSendKeysString()}");
			}
			return HookAction.SwallowKey;
		}

		return HookAction.ForwardKey;
	}

	private IEnumerable<Keys> GetRecentlyReleasedModifiers(TimeSpan maxTimeSinceRelease, DateTime nowUtc)
	{
		return KeysExtensions.ModifierKeys
			.Where(k => _keysRecord.GetKeyUpDuration(k, nowUtc) < maxTimeSinceRelease);
	}

	private TimeSpan? GetTimeSinceLastBindingKeyEvent(DateTime nowUtc)
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

	private bool TryGetVimBinding(KeyModifierFlags modifiers, Keys key, [NotNullWhen(true)] out string? output)
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
