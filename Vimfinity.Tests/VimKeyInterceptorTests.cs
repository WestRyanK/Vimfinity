using System.Windows.Forms;

namespace Vimfinity.Tests;

internal class TestableKeyboardHookManager : IKeyboardHookManager
{
	public bool IsHooked { get; private set; }
	public void AddHook(Func<KeysArgs, HookAction> hook)
	{
		IsHooked = true;
	}

	public void RemoveHook()
	{
		IsHooked = false;
	}
}

public class VimKeyInterceptorTests
{
	private VimKeyInterceptor CreateInterceptor(out List<string> outputLog)
	{
		var interceptor = new VimKeyInterceptor(new TestableKeyboardHookManager());
		List<string> log = new();
		interceptor.OutputAction = log.Add;
		interceptor.LayerKeyTappedTimeout = TimeSpan.FromTicks(100);
		interceptor.ModifierReleasedRecentlyTimeout = TimeSpan.FromTicks(50);
		interceptor.VimBindings = new Dictionary<(KeyModifierFlags, Keys), string>()
		{
			{ (KeyModifierFlags.Unspecified, Keys.J), "{Down}" },
			{ (KeyModifierFlags.Shift, Keys.X), "{Backspace}" },
			{ (KeyModifierFlags.None, Keys.X), "{Delete}" },
		};
		interceptor.LayerKey = Keys.OemSemicolon;

		outputLog = log;
		return interceptor;
	}

	private void AssertHookAction(VimKeyInterceptor interceptor, KeyPressedState state, DateTime time, HookAction expectedAction, ISet<Keys> excludedKeys)
	{
		foreach (var key in Enum.GetValues<Keys>().Where(k => !excludedKeys.Contains(k)))
		{
			HookAction action = interceptor.Intercept(new(key, state), time);
			Assert.Equal(expectedAction, action);
		}
	}

	[Fact]
	public void Hooked_Test()
	{
		TestableKeyboardHookManager hookManager = new();

		Assert.False(hookManager.IsHooked);

		using (var interceptor = new VimKeyInterceptor(hookManager))
		{
			Assert.True(hookManager.IsHooked);
		}
		Assert.False(hookManager.IsHooked);
	}

	[Fact]
	public void ForwardNoVim_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);

		AssertHookAction(interceptor, KeyPressedState.Down, new DateTime(0), HookAction.ForwardKey, new HashSet<Keys> { interceptor.LayerKey });
		AssertHookAction(interceptor, KeyPressedState.Up, new DateTime(0), HookAction.ForwardKey, new HashSet<Keys> { interceptor.LayerKey });
		Assert.Empty(outputLog);
	}

	[Fact]
	public void LayerKeyHeld_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);
	}

	[Fact]
	public void LayerKeyTapped_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(90));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.LayerKey.ToSendKeysString(), outputLog.Last());
	}

	[Fact]
	public void Binding_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(0));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Pressing bound key after layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(150));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing bound key after layer key min hold duration.
		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
	}

	[Fact]
	public void QuickReleaseLayerKeyAfterBindingDown_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		// Releasing bound key
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Up), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(110));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(120));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing layer key before layer key min hold duration, but after a bound key was pressed.
		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(160));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
	}

	[Fact]
	public void QuickReleaseLayerKeyAfterBindingUp_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(110));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(120));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing bound key
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Up), new DateTime(130));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Releasing layer key before layer key min hold duration, but after a bound key was pressed.
		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(160));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
	}

	[Fact]
	public void BindingWithModifier_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key without modifier.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Pressing modifier.
		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Pressing bound key with modifier.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Shift, Keys.X)], outputLog.Last());

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
	}

	[Fact]
	public void VimBindingWithUnspecifiedModifier_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key without modifier.
		action = interceptor.Intercept(new(Keys.J, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Unspecified, Keys.J)], outputLog.Last());

		// Pressing modifier.
		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Pressing bound key with modifier.
		action = interceptor.Intercept(new(Keys.J, KeyPressedState.Down), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Unspecified, Keys.J)], outputLog.Last());

		// Releasing modifier.
		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(50));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(2, outputLog.Count);

		// Pressing other modifier.
		action = interceptor.Intercept(new(Keys.RControlKey, KeyPressedState.Down), new DateTime(60));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(2, outputLog.Count);

		// Pressing bound key with modifier.
		action = interceptor.Intercept(new(Keys.J, KeyPressedState.Down), new DateTime(70));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(3, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Unspecified, Keys.J)], outputLog.Last());

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(3, outputLog.Count);
	}

	[Fact]
	public void NonBindingWithLayerKey_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing non-bound key
		action = interceptor.Intercept(new(Keys.A, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);
	}

	[Fact]
	public void ReleaseShiftBeforeLayerKeyRelease_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal($"{Keys.ShiftKey.ToSendKeysString()}{interceptor.LayerKey.ToSendKeysString()}", outputLog.Last());
	}

	[Fact]
	public void ReleaseShiftBeforeLayerKeyDown_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(20));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal($"{Keys.ShiftKey.ToSendKeysString()}{interceptor.LayerKey.ToSendKeysString()}", outputLog.Last());
	}

	[Fact]
	public void ReleaseShiftLongBeforeLayerKeyRelease_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(interceptor.LayerKey, KeyPressedState.Up), new DateTime(100));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.LayerKey.ToSendKeysString(), outputLog.Last());
	}

}
