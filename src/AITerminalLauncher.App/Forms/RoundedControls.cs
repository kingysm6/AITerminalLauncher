using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AITerminalLauncher.App.Forms;

internal static class RoundedShape
{
    public static GraphicsPath Create(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        if (diameter <= 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }

        diameter = Math.Min(diameter, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Color ResolveBackgroundColor(Control control, Color fallback)
    {
        var parent = control.Parent;
        while (parent is not null)
        {
            if (parent.BackColor.A == 255)
            {
                return parent.BackColor;
            }

            parent = parent.Parent;
        }

        return fallback;
    }
}

/// <summary>Anti-aliased rounded card surface. Place it on a parent that has a
/// solid BackColor so the area outside the rounded corners blends in.</summary>
internal sealed class RoundedPanel : Panel
{
    private int _radius = 12;

    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            UpdateClipRegion();
            Invalidate();
        }
    }

    public Color FillColor { get; set; } = Color.White;
    public Color BorderColor { get; set; } = Color.Transparent;

    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UpdateClipRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(RoundedShape.ResolveBackgroundColor(this, FillColor));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedShape.Create(rect, Radius);
        using (var brush = new SolidBrush(FillColor))
        {
            g.FillPath(brush, path);
        }

        if (BorderColor.A > 0)
        {
            using var pen = new Pen(BorderColor, 1f);
            g.DrawPath(pen, path);
        }
    }

    private void UpdateClipRegion()
    {
        Region?.Dispose();
        if (Width <= 0 || Height <= 0)
        {
            Region = null;
            return;
        }

        using var path = RoundedShape.Create(new Rectangle(0, 0, Width, Height), Radius);
        Region = new Region(path);
    }
}

/// <summary>Anti-aliased rounded button. Place it on a parent with a solid
/// BackColor so the corners blend in.</summary>
internal sealed class RoundedButton : Button
{
    private int _radius = 8;
    public Color NormalColor { get; set; } = Color.White;
    public Color HoverColor { get; set; } = Color.WhiteSmoke;
    public Color BorderColor { get; set; } = Color.Transparent;

    private bool _hovered;

    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            UpdateClipRegion();
            Invalidate();
        }
    }

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateClipRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(RoundedShape.ResolveBackgroundColor(this, NormalColor));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedShape.Create(rect, Radius);
        using (var brush = new SolidBrush(_hovered ? HoverColor : NormalColor))
        {
            g.FillPath(brush, path);
        }

        if (BorderColor.A > 0)
        {
            using var pen = new Pen(BorderColor, 1f);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            rect,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void UpdateClipRegion()
    {
        Region?.Dispose();
        if (Width <= 0 || Height <= 0)
        {
            Region = null;
            return;
        }

        using var path = RoundedShape.Create(new Rectangle(0, 0, Width, Height), Radius);
        Region = new Region(path);
    }
}

internal sealed class RoundedLabel : Control
{
    private int _radius = 12;
    public Color FillColor { get; set; } = Color.White;
    public Color BorderColor { get; set; } = Color.Transparent;

    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            UpdateClipRegion();
            Invalidate();
        }
    }

    public RoundedLabel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateClipRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(RoundedShape.ResolveBackgroundColor(this, FillColor));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedShape.Create(rect, Radius);
        using (var brush = new SolidBrush(FillColor))
        {
            g.FillPath(brush, path);
        }

        if (BorderColor.A > 0)
        {
            using var pen = new Pen(BorderColor, 1f);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            rect,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void UpdateClipRegion()
    {
        Region?.Dispose();
        if (Width <= 0 || Height <= 0)
        {
            Region = null;
            return;
        }

        using var path = RoundedShape.Create(new Rectangle(0, 0, Width, Height), Radius);
        Region = new Region(path);
    }
}

internal sealed class RoundedSelect : Control
{
    private readonly List<string> _items = [];
    private ContextMenuStrip? _optionsMenu;
    private bool _hovered;
    private int _selectedIndex = -1;

    public RoundedSelect()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Height = 36;
        Width = 180;
        Font = UiTheme.BaseFont;
        ForeColor = UiTheme.TextPrimary;
    }

    public List<string> Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var nextIndex = value < -1 || value >= _items.Count ? -1 : value;
            if (_selectedIndex == nextIndex)
            {
                return;
            }

            _selectedIndex = nextIndex;
            Invalidate();
        }
    }

    public object? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
        set
        {
            var text = value as string;
            SelectedIndex = string.IsNullOrEmpty(text)
                ? _items.FindIndex(static item => item.Length == 0)
                : _items.FindIndex(item => string.Equals(item, text, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        ShowOptions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _optionsMenu?.Dispose();
            _optionsMenu = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(RoundedShape.ResolveBackgroundColor(this, UiTheme.Surface));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedShape.Create(rect, 18);
        using (var brush = new SolidBrush(_hovered ? UiTheme.HoverBack : UiTheme.Surface))
        {
            g.FillPath(brush, path);
        }

        using (var pen = new Pen(_hovered ? UiTheme.Accent : UiTheme.Border, 1f))
        {
            g.DrawPath(pen, path);
        }

        var selectedText = SelectedItem as string;
        if (string.IsNullOrEmpty(selectedText))
        {
            selectedText = "未选择";
        }

        var textRect = new Rectangle(14, 0, Math.Max(0, Width - 44), Height);
        TextRenderer.DrawText(
            g,
            selectedText,
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        using var arrowPen = new Pen(UiTheme.Accent, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        var centerX = Width - 22;
        var centerY = Height / 2;
        g.DrawLines(
            arrowPen,
            new Point[]
            {
                new Point(centerX - 5, centerY - 2),
                new Point(centerX, centerY + 3),
                new Point(centerX + 5, centerY - 2),
            });
    }

    private void ShowOptions()
    {
        if (_items.Count == 0)
        {
            return;
        }

        if (_optionsMenu is { Visible: true })
        {
            return;
        }

        var menu = new ContextMenuStrip
        {
            Font = Font,
            ShowImageMargin = false,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextPrimary,
        };
        _optionsMenu = menu;
        menu.Closed += (_, _) =>
        {
            BeginInvoke(new Action(() =>
            {
                if (_optionsMenu == menu)
                {
                    _optionsMenu = null;
                }

                menu.Dispose();
            }));
        };

        for (var index = 0; index < _items.Count; index++)
        {
            var itemIndex = index;
            var itemText = string.IsNullOrEmpty(_items[index]) ? "未选择" : _items[index];
            var menuItem = new ToolStripMenuItem(itemText)
            {
                Checked = index == SelectedIndex,
            };
            menuItem.Click += (_, _) => SelectedIndex = itemIndex;
            menu.Items.Add(menuItem);
        }

        menu.Show(this, new Point(0, Height + 4));
    }
}

internal sealed class HotkeyKeyInput : Control
{
    private bool _hovered;
    private bool _focused;
    private string _hotkeyKey = string.Empty;

    public HotkeyKeyInput()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable
            | ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Height = 36;
        Width = 138;
        Font = UiTheme.BaseFont;
        ForeColor = UiTheme.TextPrimary;
        TabStop = true;
    }

    public event EventHandler<HotkeyKeyCapturedEventArgs>? KeyCaptured;

    public string HotkeyKey
    {
        get => _hotkeyKey;
        set
        {
            _hotkeyKey = NormalizeDisplayKey(value);
            Invalidate();
        }
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return true;
    }

    protected override void OnClick(EventArgs e)
    {
        Focus();
        base.OnClick(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        _focused = true;
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _focused = false;
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (TryNormalizeKey(e.KeyCode, out var key))
        {
            HotkeyKey = key;
            KeyCaptured?.Invoke(this, new HotkeyKeyCapturedEventArgs(key, e.Modifiers));
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        base.OnKeyDown(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(RoundedShape.ResolveBackgroundColor(this, UiTheme.Surface));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedShape.Create(rect, 18);
        using (var brush = new SolidBrush(_focused ? UiTheme.SelectionBack : _hovered ? UiTheme.HoverBack : UiTheme.Surface))
        {
            g.FillPath(brush, path);
        }

        using (var pen = new Pen(_focused || _hovered ? UiTheme.Accent : UiTheme.Border, 1f))
        {
            g.DrawPath(pen, path);
        }

        var text = string.IsNullOrWhiteSpace(HotkeyKey)
            ? "按键输入"
            : HotkeyKey;
        var textColor = string.IsNullOrWhiteSpace(HotkeyKey)
            ? UiTheme.TextSecondary
            : ForeColor;

        var textRect = new Rectangle(14, 0, Math.Max(0, Width - 28), Height);
        TextRenderer.DrawText(
            g,
            text,
            Font,
            textRect,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static string NormalizeDisplayKey(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static bool TryNormalizeKey(Keys keyCode, out string key)
    {
        key = string.Empty;
        if (keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return false;
        }

        if (keyCode >= Keys.A && keyCode <= Keys.Z)
        {
            key = keyCode.ToString().ToUpperInvariant();
            return true;
        }

        if (keyCode >= Keys.D0 && keyCode <= Keys.D9)
        {
            key = ((int)(keyCode - Keys.D0)).ToString();
            return true;
        }

        if (keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9)
        {
            key = $"NUMPAD{(int)(keyCode - Keys.NumPad0)}";
            return true;
        }

        if (keyCode >= Keys.F1 && keyCode <= Keys.F24)
        {
            key = keyCode.ToString().ToUpperInvariant();
            return true;
        }

        key = keyCode switch
        {
            Keys.Space => "SPACE",
            Keys.Tab => "TAB",
            Keys.Escape => "ESC",
            Keys.Enter => "ENTER",
            Keys.Back => "BACKSPACE",
            Keys.Delete => "DELETE",
            Keys.Insert => "INSERT",
            Keys.Home => "HOME",
            Keys.End => "END",
            Keys.PageUp => "PAGEUP",
            Keys.PageDown => "PAGEDOWN",
            Keys.Up => "UP",
            Keys.Down => "DOWN",
            Keys.Left => "LEFT",
            Keys.Right => "RIGHT",
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.Oemtilde => "`",
            _ => string.Empty,
        };

        return key.Length > 0;
    }
}

internal sealed class HotkeyKeyCapturedEventArgs(string key, Keys modifiers) : EventArgs
{
    public string Key { get; } = key;
    public Keys Modifiers { get; } = modifiers;
}
