using System.Drawing;
using System.Windows.Forms;

namespace AITerminalLauncher.App.Forms;

/// <summary>
/// Centralized rounded light theme shared by the settings and tool-editor
/// forms: soft blue-gray canvas, white cards, calm cyan accents, comfortable
/// round controls, and clear contrast for repeated configuration work.
/// </summary>
internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(0xF5, 0xF8, 0xFB);
    public static readonly Color Surface = Color.White;
    public static readonly Color TextPrimary = Color.FromArgb(0x15, 0x23, 0x33);
    public static readonly Color TextSecondary = Color.FromArgb(0x70, 0x7E, 0x8F);
    public static readonly Color Border = Color.FromArgb(0xD9, 0xE6, 0xEF);
    public static readonly Color Accent = Color.FromArgb(0x14, 0x9E, 0xCA);
    public static readonly Color AccentHover = Color.FromArgb(0x0E, 0x83, 0xAD);
    public static readonly Color AccentText = Color.White;
    public static readonly Color SelectionBack = Color.FromArgb(0xE7, 0xF7, 0xFC);
    public static readonly Color HoverBack = Color.FromArgb(0xEF, 0xF5, 0xF9);

    public static Font BaseFont { get; } = new Font("Segoe UI", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
    public static Font TitleFont { get; } = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
    public static Font SubtitleFont { get; } = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

    public static void ApplyFormChrome(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font = BaseFont;
    }

    public static void ApplyDeepControlStyles(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is CheckBox checkBox)
            {
                StyleCheckBox(checkBox);
            }

            ApplyDeepControlStyles(control);
        }
    }

    public static void StylePrimaryButton(RoundedButton button)
    {
        button.NormalColor = Accent;
        button.HoverColor = AccentHover;
        button.BorderColor = Color.Transparent;
        button.ForeColor = AccentText;
        button.Radius = 18;
        button.Font = BaseFont;
        SizeButton(button);
    }

    public static void StyleSecondaryButton(RoundedButton button)
    {
        button.NormalColor = Surface;
        button.HoverColor = HoverBack;
        button.BorderColor = Border;
        button.ForeColor = TextPrimary;
        button.Radius = 18;
        button.Font = BaseFont;
        SizeButton(button);
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.FlatStyle = FlatStyle.Flat;
        checkBox.ForeColor = TextPrimary;
        checkBox.BackColor = Color.Transparent;
        checkBox.FlatAppearance.BorderColor = Border;
        checkBox.FlatAppearance.CheckedBackColor = Accent;
        checkBox.FlatAppearance.MouseOverBackColor = HoverBack;
        checkBox.Font = BaseFont;
    }

    /// <summary>
    /// Wraps an input control (e.g. a TextBox) in a white rounded card so it
    /// reads as a modern rounded input instead of a boxy system field.
    /// </summary>
    public static RoundedPanel CreateInputBox(Control input, int height = 36)
    {
        if (input is TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.None;
        }

        input.BackColor = Surface;
        input.ForeColor = TextPrimary;
        input.Font = BaseFont;
        input.Dock = DockStyle.Fill;

        var panel = new RoundedPanel
        {
            FillColor = Surface,
            BorderColor = Border,
            Radius = 18,
            Width = input.Width + 24,
            Height = height,
            Padding = new Padding(12, 8, 12, 8),
        };
        panel.Controls.Add(input);
        return panel;
    }

    private static void SizeButton(RoundedButton button)
    {
        button.AutoSize = false;
        button.Height = 36;
        button.MinimumSize = new Size(96, 36);
        button.Cursor = Cursors.Hand;
    }
}
