using System.Text.Json.Serialization;

namespace Vimfinity;

internal class KeyCombo
{
	[JsonConverter(typeof(JsonStringEnumConverter<Keys>))]
	public Keys Key { get; private set; }
	[JsonConverter(typeof(JsonStringEnumConverter<KeyModifierFlags>))]
	public KeyModifierFlags Modifiers { get; private set; }

	[JsonConstructor]
	public KeyCombo(Keys key, KeyModifierFlags modifiers)
	{
		Key = key;
		Modifiers = modifiers;
	}

	public override bool Equals(object? obj)
	{
		if (obj is not KeyCombo other)
		{
			return false;
		}

		return
			this.Key == other.Key &&
			this.Modifiers == other.Modifiers;
	}

	public override int GetHashCode() => Key.GetHashCode() ^ Modifiers.GetHashCode();

	public override string ToString() => $"{{{Key}, {Modifiers}}}";
}

[JsonDerivedType(typeof(SendKeysActionBinding), nameof(SendKeysActionBinding))]
[JsonDerivedType(typeof(RunCommandActionBinding), nameof(RunCommandActionBinding))]
internal interface IBindingAction
{
	void Invoke();
}

internal class SendKeysActionBinding : IBindingAction
{
	public string Text { get; private set; }
	[JsonConstructor]
    public SendKeysActionBinding(string text)
    {
		Text = text;
    }

	public void Invoke()
	{
		SendKeys.Send(Text);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not SendKeysActionBinding other)
		{
			return false;
		}

		return Text == other.Text;
	}

	public override int GetHashCode() => Text.GetHashCode();

	public override string ToString() => $"{{{nameof(SendKeysActionBinding)}: {Text}}}";
}

internal class RunCommandActionBinding : IBindingAction
{
	public string Command { get; private set; }
	[JsonConstructor]
    public RunCommandActionBinding(string command)
    {
		Command = command;
    }

	public void Invoke()
	{
		System.Diagnostics.Process.Start(Command);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not RunCommandActionBinding other)
		{
			return false;
		}

		return Command == other.Command;
	}

	public override int GetHashCode() => Command.GetHashCode();

	public override string ToString() => $"{{{nameof(RunCommandActionBinding)}: {Command}}}";
}
