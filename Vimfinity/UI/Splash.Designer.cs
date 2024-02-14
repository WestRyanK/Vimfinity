namespace Vimfinity;

partial class Splash
{
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
		titleLabel = new Label();
		logoPictureBox = new PictureBox();
		((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
		SuspendLayout();
		// 
		// titleLabel
		// 
		titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		titleLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
		titleLabel.ForeColor = Color.White;
		titleLabel.Location = new Point(0, 179);
		titleLabel.Name = "titleLabel";
		titleLabel.Size = new Size(250, 40);
		titleLabel.TabIndex = 1;
		titleLabel.Text = "Vimfinity";
		titleLabel.TextAlign = ContentAlignment.MiddleCenter;
		// 
		// logoPictureBox
		// 
		logoPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		logoPictureBox.Image = Properties.Resources.VimfinityLogo;
		logoPictureBox.Location = new Point(0, 29);
		logoPictureBox.Name = "logoPictureBox";
		logoPictureBox.Size = new Size(250, 150);
		logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
		logoPictureBox.TabIndex = 2;
		logoPictureBox.TabStop = false;
		// 
		// Splash
		// 
		AutoScaleDimensions = new SizeF(96F, 96F);
		AutoScaleMode = AutoScaleMode.Dpi;
		BackColor = Color.FromArgb(32, 32, 32);
		ClientSize = new Size(250, 250);
		Controls.Add(logoPictureBox);
		Controls.Add(titleLabel);
		FormBorderStyle = FormBorderStyle.None;
		Name = "Splash";
		StartPosition = FormStartPosition.CenterScreen;
		Text = "Splash";
		((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
		ResumeLayout(false);
	}

	#endregion

	private Label titleLabel;
	private PictureBox logoPictureBox;
}