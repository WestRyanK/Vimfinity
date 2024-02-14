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
	public Settings Settings { get; set; } = new();

	public Action<string>? LayerKeyReleasedAction { get; set; } = SendKeys.Send;

	private KeysRecord _keysRecord = new();

	public VimKeyInterceptor(Settings settings, IKeyboardHookManager keyboardHookManager) : base(keyboardHookManager)
	{
		Settings = settings;
	}

	protected override HookAction Intercept(KeysArgs args)

	{
		return Intercept(args, DateTime.UtcNow);
	}

	internal HookAction Intercept(KeysArgs args, DateTime nowUtc)
	{
		TimeSpan? layerKeyDownDuration = _keysRecord.GetKeyDownDuration(Settings.LayerKey, nowUtc);
		_keysRecord.Record(args, nowUtc);
		KeyModifierFlags modifiers = _keysRecord.GetKeyModifiersDown();

		if (_keysRecord.IsKeyDown(Settings.LayerKey))
		{
			if (args.PressedState == KeyPressedState.Down && TryGetVimBinding(modifiers, args.Key, out IBindingAction? action))
			{
				action.Invoke();
				return HookAction.SwallowKey;
			}
		}

		if (args.Key == Settings.LayerKey)
		{
			TimeSpan? timeSinceLastBindingKeyEvent = GetTimeSinceLastBindingKeyEvent(nowUtc);
			bool wasBindingPressed = timeSinceLastBindingKeyEvent < layerKeyDownDuration;
			if (!wasBindingPressed && args.PressedState == KeyPressedState.Up && layerKeyDownDuration <	Settings.LayerKeyTappedTimeout)
			{
				IEnumerable<Keys> recentModifiers = GetRecentlyReleasedModifiers(Settings.ModifierReleasedRecentlyTimeout, nowUtc);
				string modifiersString = string.Join(null, recentModifiers.Select(k => k.ToSendKeysString()));
				LayerKeyReleasedAction?.Invoke($"{modifiersString}{Settings.LayerKey.ToSendKeysString()}");
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
		IEnumerable<TimeSpan?> upDurations = Settings.VimBindings.Keys.Select(k => _keysRecord.GetKeyUpDuration(k.Key, nowUtc));
		IEnumerable<TimeSpan?> downDurations = Settings.VimBindings.Keys.Select(k => _keysRecord.GetKeyDownDuration(k.Key, nowUtc));

		List<TimeSpan> durations =
			upDurations
			.Concat(downDurations)
			.OfType<TimeSpan>()
			.ToList();

		return durations.Any() ? durations.Min() : null;
	}

	private bool TryGetVimBinding(KeyModifierFlags modifiers, Keys key, [NotNullWhen(true)] out IBindingAction? action)
	{
		if (Settings.VimBindings.TryGetValue(new(key, modifiers), out IBindingAction? a1))
		{
			action = a1;
			return true;
		}
		if (Settings.VimBindings.TryGetValue(new(key, KeyModifierFlags.Unspecified), out IBindingAction? a2))
		{
			action = a2;
			return true;
		}

		action = default;
		return false;
	}
}
