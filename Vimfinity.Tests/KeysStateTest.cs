using System.Windows.Forms;

namespace Vimfinity.Tests;

public class KeysStateTest
{
	private void AssertKeyDown(KeysState state, Keys key, DateTime time, long expectedDownDurationTicks)
	{
		TimeSpan? downDuration = state.GetKeyDownDuration(key, time);
		Assert.NotNull(downDuration);
		Assert.Equal(expectedDownDurationTicks, downDuration.Value.Ticks);
		Assert.True(state.IsKeyDown(key));
	}

	private void AssertKeysUp(KeysState state, ISet<Keys> excludedKeys)
	{
		foreach (var key in Enum.GetValues<Keys>().Where(k => !excludedKeys.Contains(k)))
		{
			Assert.Null(state.GetKeyDownDuration(key));
			Assert.False(state.IsKeyDown(key));
		}
	}

	[Fact]
	public void NoKeysInitiallyDown_Test()
	{
		KeysState state = new();

		AssertKeysUp(state, new HashSet<Keys>());
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());
	}

	[Fact]
	public void Record_Test()
	{
		KeysState state = new();
		state.Record(new(Keys.A, KeyPressedState.Down), new DateTime(10));

		AssertKeyDown(state, Keys.A, new DateTime(30), 20);
		AssertKeysUp(state, excludedKeys: new HashSet<Keys> { Keys.A });
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.B, KeyPressedState.Down), new DateTime(50));

		AssertKeyDown(state, Keys.A, new DateTime(30), 20);
		AssertKeyDown(state, Keys.B, new DateTime(110), 60);
		AssertKeysUp(state, excludedKeys: new HashSet<Keys> { Keys.A, Keys.B });
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.B, KeyPressedState.Up), new DateTime(120));

		AssertKeyDown(state, Keys.A, new DateTime(30), 20);
		AssertKeysUp(state, excludedKeys: new HashSet<Keys> { Keys.A });
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.A, KeyPressedState.Up), new DateTime(130));

		AssertKeysUp(state, excludedKeys: new HashSet<Keys>());
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());
	}

	[Fact]
	public void RecordMultiple_Test()
	{
		KeysState state = new();
		state.Record(new(Keys.A, KeyPressedState.Down), new DateTime(10));
		state.Record(new(Keys.A, KeyPressedState.Down), new DateTime(20));
		state.Record(new(Keys.A, KeyPressedState.Down), new DateTime(30));
		state.Record(new(Keys.A, KeyPressedState.Down), new DateTime(40));

		AssertKeyDown(state, Keys.A, new DateTime(30), 20);
		AssertKeysUp(state, excludedKeys: new HashSet<Keys> { Keys.A });
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.A, KeyPressedState.Up), new DateTime(100));

		AssertKeysUp(state, excludedKeys: new HashSet<Keys>());
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.A, KeyPressedState.Up), new DateTime(110));
		state.Record(new(Keys.A, KeyPressedState.Up), new DateTime(120));
		state.Record(new(Keys.A, KeyPressedState.Up), new DateTime(130));

		AssertKeysUp(state, excludedKeys: new HashSet<Keys>());
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());
	}

	[Fact]
	public void RecordModifiers_Test()
	{
		KeysState state = new();

		AssertKeysUp(state, excludedKeys: new HashSet<Keys>());
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));

		AssertKeyDown(state, Keys.Modifiers, new DateTime(30), 20);
		AssertKeyDown(state, Keys.LShiftKey, new DateTime(30), 20);
		AssertKeyDown(state, Keys.ShiftKey, new DateTime(30), 20);
		AssertKeyDown(state, Keys.Shift, new DateTime(30), 20);
		AssertKeysUp(state, new HashSet<Keys> { Keys.LShiftKey, Keys.ShiftKey, Keys.Shift, Keys.Modifiers });
		Assert.Equal(KeyModifierFlags.Shift, state.GetKeyModifiersDown());

		state.Record(new(Keys.LControlKey, KeyPressedState.Down), new DateTime(20));

		AssertKeyDown(state, Keys.Modifiers, new DateTime(30), 20);
		AssertKeyDown(state, Keys.LControlKey, new DateTime(50), 30);
		AssertKeyDown(state, Keys.ControlKey, new DateTime(50), 30);
		AssertKeyDown(state, Keys.Control, new DateTime(50), 30);
		AssertKeysUp(state, new HashSet<Keys> { Keys.LShiftKey, Keys.ShiftKey, Keys.Shift, Keys.LControlKey, Keys.ControlKey, Keys.Control, Keys.Modifiers });
		Assert.Equal(KeyModifierFlags.Shift | KeyModifierFlags.Control, state.GetKeyModifiersDown());

		state.Record(new(Keys.LMenu, KeyPressedState.Down), new DateTime(60));

		AssertKeyDown(state, Keys.Modifiers, new DateTime(30), 20);
		AssertKeyDown(state, Keys.LMenu, new DateTime(120), 60);
		AssertKeyDown(state, Keys.Menu, new DateTime(120), 60);
		AssertKeyDown(state, Keys.Alt, new DateTime(120), 60);
		AssertKeysUp(state, new HashSet<Keys> { Keys.LShiftKey, Keys.ShiftKey, Keys.Shift, Keys.LControlKey, Keys.ControlKey, Keys.Control, Keys.LMenu, Keys.Menu, Keys.Alt, Keys.Modifiers });
		Assert.Equal(KeyModifierFlags.Shift | KeyModifierFlags.Control | KeyModifierFlags.Alt, state.GetKeyModifiersDown());
	}

	[Fact]
	public void RecordLeftRightModifiers_Test()
	{
		KeysState state = new();

		AssertKeysUp(state, excludedKeys: new HashSet<Keys>());
		Assert.Equal(KeyModifierFlags.None, state.GetKeyModifiersDown());

		state.Record(new(Keys.LShiftKey, KeyPressedState.Down), new DateTime(10));

		AssertKeyDown(state, Keys.Modifiers, new DateTime(30), 20);
		AssertKeyDown(state, Keys.LShiftKey, new DateTime(30), 20);
		AssertKeyDown(state, Keys.ShiftKey, new DateTime(30), 20);
		AssertKeyDown(state, Keys.Shift, new DateTime(30), 20);
		AssertKeysUp(state, new HashSet<Keys> { Keys.LShiftKey, Keys.ShiftKey, Keys.Shift, Keys.Modifiers });
		Assert.Equal(KeyModifierFlags.Shift, state.GetKeyModifiersDown());

		state.Record(new(Keys.RShiftKey, KeyPressedState.Down), new DateTime(5));

		AssertKeyDown(state, Keys.Modifiers, new DateTime(30), 25);
		AssertKeyDown(state, Keys.LShiftKey, new DateTime(30), 20);
		AssertKeyDown(state, Keys.RShiftKey, new DateTime(30), 25);
		AssertKeyDown(state, Keys.ShiftKey, new DateTime(30), 25);
		AssertKeyDown(state, Keys.Shift, new DateTime(30), 25);
		AssertKeysUp(state, new HashSet<Keys> { Keys.LShiftKey, Keys.RShiftKey, Keys.ShiftKey, Keys.Shift, Keys.Modifiers });
		Assert.Equal(KeyModifierFlags.Shift, state.GetKeyModifiersDown());
	}
}