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
	public Action<string>? LayerKeyReleasedAction { get; set; } = SendKeys.Send;

	public static readonly IReadOnlyDictionary<KeyCombo, IBindingAction> DefaultVimBindings = new Dictionary<KeyCombo, IBindingAction>()
	{
		{ new(Keys.H, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{Left}") },
		{ new(Keys.J, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{Down}") },
		{ new(Keys.K, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{Up}") },
		{ new(Keys.L, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{Right}") },
		{ new(Keys.N, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{Home}") },
		{ new(Keys.M, KeyModifierFlags.Unspecified), new SendKeysActionBinding("{End}") },
		{ new(Keys.X, KeyModifierFlags.Shift), new SendKeysActionBinding("{Backspace}") },
		{ new(Keys.X, KeyModifierFlags.None), new SendKeysActionBinding("{Delete}") },
	};

	public IReadOnlyDictionary<KeyCombo, IBindingAction> VimBindings { get; set; } = DefaultVimBindings;

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
			if (args.PressedState == KeyPressedState.Down && TryGetVimBinding(modifiers, args.Key, out IBindingAction? action))
			{
				action.Invoke();
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
				LayerKeyReleasedAction?.Invoke($"{modifiersString}{LayerKey.ToSendKeysString()}");
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
		IEnumerable<TimeSpan?> upDurations = VimBindings.Keys.Select(k => _keysRecord.GetKeyUpDuration(k.Key, nowUtc));
		IEnumerable<TimeSpan?> downDurations = VimBindings.Keys.Select(k => _keysRecord.GetKeyDownDuration(k.Key, nowUtc));

		List<TimeSpan> durations =
			upDurations
			.Concat(downDurations)
			.OfType<TimeSpan>()
			.ToList();

		return durations.Any() ? durations.Min() : null;
	}

	private bool TryGetVimBinding(KeyModifierFlags modifiers, Keys key, [NotNullWhen(true)] out IBindingAction? action)
	{
		if (VimBindings.TryGetValue(new(key, modifiers), out IBindingAction? a1))
		{
			action = a1;
			return true;
		}
		if (VimBindings.TryGetValue(new(key, KeyModifierFlags.Unspecified), out IBindingAction? a2))
		{
			action = a2;
			return true;
		}

		action = default;
		return false;
	}
}
