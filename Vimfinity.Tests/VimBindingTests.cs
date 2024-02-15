using System.Windows.Forms;

namespace Vimfinity.Tests;
public class VimBindingTests
{
	[Fact]
	public void KeyCombo_Equal_Test()
	{
		KeyCombo combo1 = new(Keys.A, KeyModifierFlags.None);
		KeyCombo combo2 = new(Keys.A, KeyModifierFlags.None);

		Assert.Equal(combo1, combo1);
		Assert.Equal(combo1, combo2);
	}

	[Fact]
	public void KeyCombo_NotEqual_Test()
	{
		KeyCombo combo = new(Keys.A, KeyModifierFlags.Shift);

		Assert.False(combo.Equals(null));
		Assert.NotEqual(combo, new KeyCombo(Keys.A, KeyModifierFlags.None));
		Assert.NotEqual(combo, new KeyCombo(Keys.A, KeyModifierFlags.Control));
		Assert.NotEqual(combo, new KeyCombo(Keys.A, KeyModifierFlags.Control | KeyModifierFlags.Shift));
		Assert.NotEqual(combo, new KeyCombo(Keys.B, KeyModifierFlags.Shift));
	}

	[Fact]
	public void SendKeysBindingAction_Test()
	{
		SendKeysBindingAction binding = new("some text");
		Assert.Equal("some text", binding.Text);
	}

	[Fact]
	public void SendKeysBindingAction_Equals_Test()
	{
		SendKeysBindingAction binding = new("text 1");
		Assert.Equal(binding, new SendKeysBindingAction("text 1"));
		Assert.NotEqual(binding, new SendKeysBindingAction("text 2"));
		Assert.NotEqual((IBindingAction)binding, new RunCommandBindingAction("text 1"));
	}

	[Fact]
	public void RunCommandBindingAction_Test()
	{
		RunCommandBindingAction binding = new("notepad.exe", "file.txt");
		Assert.Equal("notepad.exe", binding.Command);
		Assert.Equal("file.txt", binding.Arguments);
	}

	[Fact]
	public void RunCommandBindingAction_Equals_Test()
	{
		RunCommandBindingAction binding = new("command 1");
		Assert.Equal(binding, new RunCommandBindingAction("command 1"));
		Assert.NotEqual(binding, new RunCommandBindingAction("command 1", "argument1"));
		Assert.NotEqual(binding, new RunCommandBindingAction("command 2"));
		Assert.NotEqual((IBindingAction)binding, new SendKeysBindingAction("command 1"));
	}

	[Fact]
	public void RunCommandBindingAction_EqualsWithArguments_Test()
	{
		RunCommandBindingAction binding = new("command 1", "arguments1");
		Assert.Equal(binding, new RunCommandBindingAction("command 1", "arguments1"));
		Assert.NotEqual(binding, new RunCommandBindingAction("command 2", "arguments1"));
		Assert.NotEqual(binding, new RunCommandBindingAction("command 1"));
		Assert.NotEqual(binding, new RunCommandBindingAction("command 2"));
		Assert.NotEqual((IBindingAction)binding, new SendKeysBindingAction("command 1"));
	}
}

