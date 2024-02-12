namespace Vimfinity;

class Program
{
	public static void Main()
	{
		TrayIcon trayIcon = new();

		using Win32KeyboardHookManager hookManager = new();
		using KeyInterceptor interceptor = new VimKeyInterceptor(hookManager);
		Application.Run();
	}
}
