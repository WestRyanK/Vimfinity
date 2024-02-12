namespace Vimfinity;

class Program
{
	public static void Main()
	{
		Splash splash = new(TimeSpan.FromSeconds(2));
		splash.Show();

		TrayIcon trayIcon = new();

		using Win32KeyboardHookManager hookManager = new();
		using KeyInterceptor interceptor = new VimKeyInterceptor(hookManager);
		Application.Run();
	}
}
