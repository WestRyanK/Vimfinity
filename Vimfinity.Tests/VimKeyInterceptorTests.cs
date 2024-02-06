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
		interceptor.VimKeyDownMinDuration = TimeSpan.FromTicks(100);
		interceptor.MaxTimeSinceModifierRecentlyReleased = TimeSpan.FromTicks(50);
		interceptor.VimBindings = new Dictionary<(KeyModifierFlags, Keys), string>()
		{
			{ (KeyModifierFlags.Unspecified, Keys.J), "{Down}" },
			{ (KeyModifierFlags.Shift, Keys.X), "{Backspace}" },
			{ (KeyModifierFlags.None, Keys.X), "{Delete}" },
		};
		interceptor.VimKey = Keys.OemSemicolon;

		outputLog = log;
		return interceptor;
	}

	private void AssertHookAction(VimKeyInterceptor interceptor, KeyPressedState state, DateTime time, HookAction expectedAction, ISet<Keys> excludedKeys)
	{
		foreach (var key in Enum.GetValues<Keys>().Where(k => !excludedKeys.Contains(k)))
		{
			HookAction action = interceptor.VimIntercept(new(key, state), time);
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

		AssertHookAction(interceptor, KeyPressedState.Down, new DateTime(0), HookAction.ForwardKey, new HashSet<Keys> { interceptor.VimKey });
		AssertHookAction(interceptor, KeyPressedState.Up, new DateTime(0), HookAction.ForwardKey, new HashSet<Keys> { interceptor.VimKey });
		Assert.Empty(outputLog);
	}

	[Fact]
	public void VimKeyHeld_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);
	}

	[Fact]
	public void VimKeyTapped_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(90));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimKey.ToSendKeysString(), outputLog.Last());
	}

	[Fact]
	public void VimBinding_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(0));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing Vim-bound key before vim key min hold duration.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing Vim-bound key before vim key min hold duration.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Pressing Vim-bound key after vim key min hold duration.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(150));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing Vim-bound key after vim key min hold duration.
		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
	}

	[Fact]
	public void QuickReleaseVimKeyAfterVimBindingDown_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		// Releasing Vim-bound key
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Up), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(110));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing Vim-bound key before vim key min hold duration.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(120));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing Vim key before vim key min hold duration, but after a vim-bound key was pressed.
		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(160));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
	}

	[Fact]
	public void QuickReleaseVimKeyAfterVimBindingUp_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(110));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing Vim-bound key before vim key min hold duration.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(120));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Releasing Vim-bound key
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Up), new DateTime(130));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Releasing Vim key before vim key min hold duration, but after a vim-bound key was pressed.
		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(160));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
	}

	[Fact]
	public void VimBindingWithModifier_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing Vim-bound key without modifier.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.None, Keys.X)], outputLog.Last());

		// Pressing modifier.
		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Pressing Vim-bound key with modifier.
		action = interceptor.VimIntercept(new(Keys.X, KeyPressedState.Down), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Shift, Keys.X)], outputLog.Last());

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
	}

	[Fact]
	public void VimBindingWithUnspecifiedModifier_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing Vim-bound key without modifier.
		action = interceptor.VimIntercept(new(Keys.J, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Unspecified, Keys.J)], outputLog.Last());

		// Pressing modifier.
		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Pressing Vim-bound key with modifier.
		action = interceptor.VimIntercept(new(Keys.J, KeyPressedState.Down), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Unspecified, Keys.J)], outputLog.Last());

		// Releasing modifier.
		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(50));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(2, outputLog.Count);

		// Pressing other modifier.
		action = interceptor.VimIntercept(new(Keys.RControlKey, KeyPressedState.Down), new DateTime(60));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(2, outputLog.Count);

		// Pressing Vim-bound key with modifier.
		action = interceptor.VimIntercept(new(Keys.J, KeyPressedState.Down), new DateTime(70));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(3, outputLog.Count);
		Assert.Equal(interceptor.VimBindings[(KeyModifierFlags.Unspecified, Keys.J)], outputLog.Last());

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(3, outputLog.Count);
	}

	[Fact]
	public void NonVimBindingWithVimKey_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing non-vim-binding key
		action = interceptor.VimIntercept(new(Keys.A, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);
	}

	[Fact]
	public void ReleaseShiftBeforeVimKeyRelease_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal($"{Keys.ShiftKey.ToSendKeysString()}{interceptor.VimKey.ToSendKeysString()}", outputLog.Last());
	}

	[Fact]
	public void ReleaseShiftBeforeVimKeyDown_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(20));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal($"{Keys.ShiftKey.ToSendKeysString()}{interceptor.VimKey.ToSendKeysString()}", outputLog.Last());
	}

	[Fact]
	public void ReleaseShiftLongBeforeVimKeyRelease_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		HookAction action;

		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.VimIntercept(new(interceptor.VimKey, KeyPressedState.Up), new DateTime(100));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(interceptor.VimKey.ToSendKeysString(), outputLog.Last());
	}

}
