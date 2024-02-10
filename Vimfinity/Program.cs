namespace Vimfinity;

class Program
{
	public static void Main()
	{
		NotifyIcon trayIcon = new();
		trayIcon.Text = Application.ProductName;
		trayIcon.Icon = new Icon(Properties.Resources.vimfinity, 40, 40);
		trayIcon.Visible = true;

		ContextMenuStrip menu = new();
		ToolStripItem exitItem = menu.Items.Add("Exit");
		exitItem.Click += ExitItem_Click;
		trayIcon.ContextMenuStrip = menu;

		using Win32KeyboardHookManager hookManager = new();
		using KeyInterceptor interceptor = new VimKeyInterceptor(hookManager);
		Application.Run();
	}

	private static void ExitItem_Click(object? sender, EventArgs e)
	{
		Application.Exit();
	}
}
