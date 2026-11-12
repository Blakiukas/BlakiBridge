namespace BlakiBridge;

public sealed class IdentifyForm : Form
{
    public IdentifyForm(Screen screen, int number)
    {
        Bounds = screen.Bounds;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(20,20,20);
        Opacity = .92;
        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = number.ToString(),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 96, FontStyle.Bold)
        };
        Controls.Add(label);
        var timer = new System.Windows.Forms.Timer { Interval = 2200 };
        timer.Tick += (_, _) => { timer.Stop(); Close(); };
        Shown += (_, _) => timer.Start();
    }
}
