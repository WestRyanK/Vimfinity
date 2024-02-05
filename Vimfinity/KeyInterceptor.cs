namespace Vimfinity;

internal class KeyInterceptor : IDisposable
{

	private static HookAction Hook(HookArgs args)
	{
		//Debug.WriteLine($"Input Key: {args.Key} State: {args.PressedState}");
		if (args.PressedState == KeyPressedState.Down && _InputKeysToOutput.TryGetValue(args.Key, out var output))
		{
			//Debug.WriteLine($"Output: {output}");
			SendKeys.Send(output);
			return HookAction.SwallowKey;
		}
		return HookAction.ForwardKey;
	}

	private static readonly IDictionary<Keys, string> _InputKeysToOutput = new Dictionary<Keys, string>() {
		{ Keys.H, "{Left}" },
		{ Keys.J, "{Down}" },
		{ Keys.K, "{Up}" },
		{ Keys.L, "{Right}" },
		{ Keys.X, "{Backspace}" },
	};


    public KeyInterceptor()
    {
		KeyboardHookManager.AddHook(Hook);
    }

    public void Dispose()
	{
		KeyboardHookManager.RemoveHook();
	}
}
