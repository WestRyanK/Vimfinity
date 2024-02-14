namespace Vimfinity;

class Program
{
	public static void Main(string[] args)
	{
		CommandLineArgs commandArgs = new(args);

		IFilePersisitenceProvider persistence = new JsonFilePersistenceProvider();
		IPathProvider pathProvider = new PathProvider();

		Settings settings;
		try
		{
			settings = persistence.Load<Settings>(pathProvider.SettingsPath);
		}
		catch (FileNotFoundException)
		{
			settings = new();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading settings:\n{ex}");
			settings = new();
		}

		if (commandArgs.CreateSettingsFile)
		{
			persistence.Save(settings, pathProvider.SettingsPath);
		}

		if (!commandArgs.NoSplash)
		{
			Splash splash = new(TimeSpan.FromSeconds(2));
			splash.Show();
		}

		TrayIcon trayIcon = new();

		using Win32KeyboardHookManager hookManager = new();
		using KeyInterceptor interceptor = new VimKeyInterceptor(settings, hookManager);
		Application.Run();
	}
}

class CommandLineArgs
{
	public const string NoSplashSwitchName = "NoSplash";
	public bool NoSplash { get; private set; } = false;
	public const string CreateSettingsFileSwitchName = "CreateSettingsFile";
	public bool CreateSettingsFile { get; private set; } = false;

    public CommandLineArgs(string[] args)
    {
		NoSplash = HasSwitch(args, NoSplashSwitchName);
		CreateSettingsFile = HasSwitch(args, CreateSettingsFileSwitchName);
    }

	public static bool HasSwitch(string[] args, string switchName) => args.Any(arg => string.Equals(arg, $"-{switchName}", StringComparison.OrdinalIgnoreCase));
}
