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
		Debug.WriteLine($"Key: {args.Key} State: {args.PressedState}");
		return args;
	}
}
