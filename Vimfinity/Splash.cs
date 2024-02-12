using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vimfinity;
public partial class Splash : Form
{
	public Splash()
	{
		InitializeComponent();

		_ = CloseAfterDuration(TimeSpan.FromSeconds(2));
	}

	public Splash(TimeSpan duration)
	{
		InitializeComponent();

		_ = CloseAfterDuration(duration);
	}

	private async Task CloseAfterDuration(TimeSpan duration)
	{
		await Task.Delay(duration);
		Close();
	}
}
