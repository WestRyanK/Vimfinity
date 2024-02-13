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

#pragma warning disable xUnit2000 // Constants and literals should be the expected argument
		Assert.NotEqual(combo, null);
#pragma warning restore xUnit2000 // Constants and literals should be the expected argument
		Assert.NotEqual(combo, new KeyCombo(Keys.A, KeyModifierFlags.None));
		Assert.NotEqual(combo, new KeyCombo(Keys.A, KeyModifierFlags.Control));
		Assert.NotEqual(combo, new KeyCombo(Keys.A, KeyModifierFlags.Control | KeyModifierFlags.Shift));
		Assert.NotEqual(combo, new KeyCombo(Keys.B, KeyModifierFlags.Shift));
	}

	[Fact]
	public void SendKeysActionBinding_Test()
	{
		SendKeysActionBinding binding = new("some text");
		Assert.Equal("some text", binding.Text);
	}

	[Fact]
	public void SendKeysActionBinding_Equals_Test()
	{
		SendKeysActionBinding binding = new("text 1");
		Assert.Equal(binding, new SendKeysActionBinding("text 1"));
		Assert.NotEqual(binding, new SendKeysActionBinding("text 2"));
		Assert.NotEqual((IBindingAction)binding, new RunCommandActionBinding("text 1"));
	}

	[Fact]
	public void RunCommandActionBinding_Test()
	{
		RunCommandActionBinding binding = new("notepad.exe");
		Assert.Equal("notepad.exe", binding.Command);
	}

	[Fact]
	public void RunCommandActionBinding_Equals_Test()
	{
		RunCommandActionBinding binding = new("command 1");
		Assert.Equal(binding, new RunCommandActionBinding("command 1"));
		Assert.NotEqual(binding, new RunCommandActionBinding("command 2"));
		Assert.NotEqual((IBindingAction)binding, new SendKeysActionBinding("command 1"));
	}
}
