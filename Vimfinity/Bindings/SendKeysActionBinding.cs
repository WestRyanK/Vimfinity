using System.Text.Json.Serialization;

namespace Vimfinity;

internal class SendKeysBindingAction : IBindingAction
{
	public string Text { get; private set; }
	[JsonConstructor]
    public SendKeysBindingAction(string text)
    {
		Text = text;
    }

	public void Invoke()
	{
		SendKeys.Send(Text);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not SendKeysBindingAction other)
		{
			return false;
		}

		return Text == other.Text;
	}

	public override int GetHashCode() => Text.GetHashCode();

	public override string ToString() => $"{{{nameof(SendKeysBindingAction)}: {Text}}}";
}

