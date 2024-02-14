namespace Vimfinity;

class Program
{
	public static void Main(string[] args)
	{
		CommandLineArgs commandArgs = new(args);

		if (!commandArgs.NoSplash)
		{
			Splash splash = new(TimeSpan.FromSeconds(2));
			splash.Show();
		}

		TrayIcon trayIcon = new();

		using Win32KeyboardHookManager hookManager = new();
		using KeyInterceptor interceptor = new VimKeyInterceptor(new(), hookManager);
		Application.Run();
	}
}

class CommandLineArgs
{
	public bool NoSplash { get; private set; } = false;

    public CommandLineArgs(string[] args)
    {
		NoSplash = args.Any(arg => string.Equals(arg, "-nosplash", StringComparison.OrdinalIgnoreCase));
    }
}
