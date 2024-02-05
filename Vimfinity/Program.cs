using System.Diagnostics;

namespace Vimfinity;

class Program
{
	public static void Main()
	{
		Debug.WriteLine("Hello World");
		KeyboardHookManager.AddHook(Hook);
		Application.Run();
		KeyboardHookManager.RemoveHook();
		Debug.WriteLine("Goodbye cruel world");
	}

	private static HookArgs Hook(HookArgs args)
	{
		//Debug.WriteLine($"Input Key: {args.Key} State: {args.PressedState}");
		if (args.PressedState == KeyPressedState.Down && _InputKeysToOutput.TryGetValue(args.Key, out var output))
		{
			//Debug.WriteLine($"Output: {output}");
			SendKeys.Send(output);
			return HookArgs.Swallow;
		}
		return args;
	}

	private static readonly IDictionary<Keys, string> _InputKeysToOutput = new Dictionary<Keys, string>() {
		{ Keys.H, "{Left}" },
		{ Keys.J, "{Down}" },
		{ Keys.K, "{Up}" },
		{ Keys.L, "{Right}" },
		{ Keys.X, "{Backspace}" },
	};
}
