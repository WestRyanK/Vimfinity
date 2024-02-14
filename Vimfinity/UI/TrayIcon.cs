namespace Vimfinity;

internal class TrayIcon
{
    public TrayIcon()
    {
        NotifyIcon trayIcon = new();
        trayIcon.Text = Application.ProductName;
        trayIcon.Icon = new Icon(Properties.Resources.VimfinityIcon, 40, 40);
        trayIcon.Visible = true;

        ContextMenuStrip menu = new();
        ToolStripItem exitItem = menu.Items.Add("Exit");
        exitItem.Click += ExitItem_Click;
        trayIcon.ContextMenuStrip = menu;
    }

    private static void ExitItem_Click(object? sender, EventArgs e)
    {
        Application.Exit();
    }
}
