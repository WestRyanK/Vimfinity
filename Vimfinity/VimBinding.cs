namespace Vimfinity;

internal class KeyCombo
{
	public Keys Key { get; private set; }
	public KeyModifierFlags Modifiers { get; private set; }

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

	public override int GetHashCode()
	{
		return Key.GetHashCode() ^ Modifiers.GetHashCode();
	}

	public override string ToString()
	{
		return $"{{{Key}, {Modifiers}}}";
	}
}

internal interface IBindingAction
{
	void Invoke();
}

internal class SendKeysActionBinding : IBindingAction
{
	public string Text { get; private set; }
    public SendKeysActionBinding(string text)
    {
		Text = text;
    }

	public void Invoke()
	{
		SendKeys.Send(Text);
	}
}

internal class RunCommandActionBinding : IBindingAction
{
	public string Command { get; private set; }
    public RunCommandActionBinding(string command)
    {
		Command = command;
    }

	public void Invoke()
	{
		System.Diagnostics.Process.Start(Command);
	}
}
