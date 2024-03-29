﻿using System.Windows.Forms;

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

internal class LogBindingAction : IBindingAction
{
	public List<string> Log { get; set; }
	public string Text { get; set; }

    public LogBindingAction(List<string> log, string text)
    {
		Log = log;
		Text = text;
    }

    public void Invoke()
	{
		Log.Add(Text);
	}
}

public class VimKeyInterceptorTests
{
	private const string _Settings1 = "Test";
	private const string _Settings2 = "Test2";
	private VimKeyInterceptor CreateInterceptor(out List<string> outputLog)
	{
		List<string> log = new();
		Settings settings = new()
		{
			Layers = new Dictionary<string, LayerSettings>()
			{
				{ _Settings1, new() {
						LayerKey = Keys.OemSemicolon,
						LayerKeyTappedTimeout = TimeSpan.FromTicks(100),
						ModifierReleasedRecentlyTimeout = TimeSpan.FromTicks(50),
						VimBindings = new Dictionary<KeyCombo, IBindingAction>()
						{
							{ new(Keys.J, KeyModifierFlags.Unspecified), new LogBindingAction(log, "J") },
							{ new(Keys.X, KeyModifierFlags.Shift), new LogBindingAction(log, "X") },
							{ new(Keys.X, KeyModifierFlags.None), new LogBindingAction(log, "x") },
						},
					}
				},
				{ _Settings2, new() {
						LayerKey = Keys.OemPeriod,
						LayerKeyTappedTimeout = TimeSpan.FromTicks(100),
						ModifierReleasedRecentlyTimeout = TimeSpan.FromTicks(50),
						VimBindings = new Dictionary<KeyCombo, IBindingAction>()
						{
							{ new(Keys.J, KeyModifierFlags.Unspecified), new LogBindingAction(log, "J2") },
							{ new(Keys.X, KeyModifierFlags.Shift), new LogBindingAction(log, "X2") },
							{ new(Keys.X, KeyModifierFlags.None), new LogBindingAction(log, "x2") },
						},
					}
				}
			}
		};
		
		var interceptor = new VimKeyInterceptor(settings, new TestableKeyboardHookManager());
		interceptor.LayerKeyReleasedAction = log.Add;

		outputLog = log;
		return interceptor;
	}

	private void GetSettings(VimKeyInterceptor interceptor, out LayerSettings settings1, out LayerSettings settings2)
	{
		settings1 = interceptor.Settings.Layers[_Settings1];
		settings2 = interceptor.Settings.Layers[_Settings2];
	}

	private string BindingText(VimKeyInterceptor interceptor, Keys key, KeyModifierFlags modifier)
	{
		return (interceptor.Settings.Layers.First().Value.VimBindings[new KeyCombo(key, modifier)] as LogBindingAction)!.Text;
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

		using (var interceptor = new VimKeyInterceptor(new(), hookManager))
		{
			Assert.True(hookManager.IsHooked);
		}
		Assert.False(hookManager.IsHooked);
	}

	[Fact]
	public void ForwardNoVim_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);

		var keys = interceptor.Settings.Layers.Select(x => x.Value.LayerKey).ToHashSet();
		AssertHookAction(interceptor, KeyPressedState.Down, new DateTime(0), HookAction.ForwardKey, keys);
		AssertHookAction(interceptor, KeyPressedState.Up, new DateTime(0), HookAction.ForwardKey, keys);
		Assert.Empty(outputLog);
	}

	[Fact]
	public void LayerKeyHeld_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);
	}

	[Fact]
	public void LayerKeyTapped_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(90));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(settings.LayerKey.ToSendKeysString(), outputLog.Last());
	}

	[Fact]
	public void SecondLayerKeyTapped_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out _, out var settings2);
		HookAction action;

		action = interceptor.Intercept(new(settings2.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings2.LayerKey, KeyPressedState.Up), new DateTime(90));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(settings2.LayerKey.ToSendKeysString(), outputLog.Last());
	}

	[Fact]
	public void Binding_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(0));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.None), outputLog.Last());

		// Releasing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.None), outputLog.Last());

		// Pressing bound key after layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(150));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.None), outputLog.Last());

		// Releasing bound key after layer key min hold duration.
		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
	}

	[Fact]
	public void QuickReleaseLayerKeyAfterBindingDown_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		// Releasing bound key
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Up), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(110));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(120));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.None), outputLog.Last());

		// Releasing layer key before layer key min hold duration, but after a bound key was pressed.
		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(160));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
	}

	[Fact]
	public void QuickReleaseLayerKeyAfterBindingUp_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(110));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key before layer key min hold duration.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(120));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.None), outputLog.Last());

		// Releasing bound key
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Up), new DateTime(130));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Releasing layer key before layer key min hold duration, but after a bound key was pressed.
		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(160));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
	}

	[Fact]
	public void BindingWithModifier_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key without modifier.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.None), outputLog.Last());

		// Pressing modifier.
		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Pressing bound key with modifier.
		action = interceptor.Intercept(new(Keys.X, KeyPressedState.Down), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.X, KeyModifierFlags.Shift), outputLog.Last());

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
	}

	[Fact]
	public void VimBindingWithUnspecifiedModifier_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		// Pressing bound key without modifier.
		action = interceptor.Intercept(new(Keys.J, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.J, KeyModifierFlags.Unspecified), outputLog.Last());

		// Pressing modifier.
		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Equal(1, outputLog.Count);

		// Pressing bound key with modifier.
		action = interceptor.Intercept(new(Keys.J, KeyPressedState.Down), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(2, outputLog.Count);
		Assert.Equal(BindingText(interceptor, Keys.J, KeyModifierFlags.Unspecified), outputLog.Last());

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
		Assert.Equal(BindingText(interceptor, Keys.J, KeyModifierFlags.Unspecified), outputLog.Last());

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(200));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(3, outputLog.Count);
	}

	[Fact]
	public void NonBindingWithLayerKey_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(10));
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
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal($"{Keys.ShiftKey.ToSendKeysString()}{settings.LayerKey.ToSendKeysString()}", outputLog.Last());
	}

	[Fact]
	public void ReleaseShiftBeforeLayerKeyDown_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(20));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(30));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(40));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal($"{Keys.ShiftKey.ToSendKeysString()}{settings.LayerKey.ToSendKeysString()}", outputLog.Last());
	}

	[Fact]
	public void ReleaseShiftLongBeforeLayerKeyRelease_Test()
	{
		VimKeyInterceptor interceptor = CreateInterceptor(out List<string> outputLog);
		GetSettings(interceptor, out var settings, out _);
		HookAction action;

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Down), new DateTime(20));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(Keys.LShiftKey, KeyPressedState.Up), new DateTime(30));
		Assert.Equal(HookAction.ForwardKey, action);
		Assert.Empty(outputLog);

		action = interceptor.Intercept(new(settings.LayerKey, KeyPressedState.Up), new DateTime(100));
		Assert.Equal(HookAction.SwallowKey, action);
		Assert.Equal(1, outputLog.Count);
		Assert.Equal(settings.LayerKey.ToSendKeysString(), outputLog.Last());
	}

}
