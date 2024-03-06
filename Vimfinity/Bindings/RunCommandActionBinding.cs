using System.Text.Json.Serialization;

namespace Vimfinity;
internal class RunCommandBindingAction : IBindingAction
{
	public string Command { get; private set; }
	public string? Arguments { get; private set; }
	[JsonConstructor]
    public RunCommandBindingAction(string command, string? arguments = null)
    {
		Command = command;
		Arguments = arguments;
    }

	public void Invoke()
	{
		System.Diagnostics.Process.Start(Command, Arguments ?? string.Empty);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not RunCommandBindingAction other)
		{
			return false;
		}

		return
			Command == other.Command &&
			Arguments == other.Arguments;
	}

	public override int GetHashCode() => Command.GetHashCode() ^ Arguments?.GetHashCode() ?? 0;

	public override string ToString() => $"{{{nameof(RunCommandBindingAction)}: {Command}, {Arguments} }}";
}
