using System.Drawing;
using System.Windows.Forms;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.Core.Config;
using AITerminalLauncher.Core.Validation;

namespace AITerminalLauncher.App.Forms;

public sealed class SettingsForm : Form
{
    private readonly FlowLayoutPanel _toolCardsPanel;
    private readonly RoundedSelect _preferredTerminalSelect;
    private readonly RoundedSelect _fallbackTerminalSelect;
    private readonly RoundedButton _launchAtLoginToggleButton;
    private RoundedButton _enabledToggleButton = null!;
    private bool _launchAtLoginEnabled;
    private string? _selectedToolId;

    public SettingsForm(AppConfig config)
    {
        EditableConfig = ConfigCloneHelper.Clone(config);

        Text = "AI Terminal Launcher 设置";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(960, 640);
        UiTheme.ApplyFormChrome(this);

        _toolCardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        _toolCardsPanel.Resize += (_, _) => ResizeToolCards();

        _preferredTerminalSelect = new RoundedSelect
        {
            Width = 220,
        };
        _preferredTerminalSelect.Items.AddRange(["wt", "powershell"]);

        _fallbackTerminalSelect = new RoundedSelect
        {
            Width = 220,
        };
        _fallbackTerminalSelect.Items.AddRange(["powershell", "wt"]);

        _launchAtLoginToggleButton = new RoundedButton
        {
            Width = 138,
            Height = 36,
            Margin = new Padding(0, 7, 0, 7),
        };
        _launchAtLoginToggleButton.Click += (_, _) => ToggleLaunchAtLoginSetting();

        BuildLayout();
        UiTheme.ApplyDeepControlStyles(this);
        LoadConfigValues();
        RefreshToolList();
    }

    public AppConfig EditableConfig { get; }

    public AppConfig? SavedConfig { get; private set; }

    private void BuildLayout()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20),
            BackColor = UiTheme.Background,
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        outer.Controls.Add(BuildTitleCard(), 0, 0);
        outer.Controls.Add(BuildBody(), 0, 1);
        outer.Controls.Add(BuildSettingsCard(), 0, 2);
        outer.Controls.Add(BuildFooter(), 0, 3);

        Controls.Add(outer);
    }

    private static RoundedPanel BuildTitleCard()
    {
        var card = new RoundedPanel
        {
            FillColor = UiTheme.Surface,
            Radius = 24,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
        };
        card.Controls.Add(new Label
        {
            Text = "AI TERMINAL LAUNCHER",
            AutoSize = true,
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.TextPrimary,
            BackColor = Color.Transparent,
            Location = new Point(18, 12),
        });
        card.Controls.Add(new Label
        {
            Text = "CLI 控制矩阵 / 快捷键路由 / 终端偏好",
            AutoSize = true,
            Font = UiTheme.SubtitleFont,
            ForeColor = UiTheme.TextSecondary,
            BackColor = Color.Transparent,
            Location = new Point(20, 40),
        });
        return card;
    }

    private Control BuildBody()
    {
        return BuildToolListCard();
    }

    private RoundedPanel BuildToolListCard()
    {
        var card = new RoundedPanel
        {
            FillColor = UiTheme.Surface,
            Radius = 24,
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(BuildToolCommandBar(), 0, 0);
        layout.Controls.Add(_toolCardsPanel, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildToolCommandBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 8),
        };

        bar.Controls.Add(CreateToolbarButton("添加", (_, _) => AddTool(), primary: true));
        bar.Controls.Add(CreateToolbarButton("编辑", (_, _) => EditSelectedTool()));
        bar.Controls.Add(CreateToolbarButton("删除", (_, _) => RemoveSelectedTool()));
        _enabledToggleButton = CreateToolbarButton("启用", (_, _) => ToggleSelectedTool(static tool => tool.Enabled = !tool.Enabled));
        bar.Controls.Add(_enabledToggleButton);
        bar.Controls.Add(CreateToolbarButton("托盘", (_, _) => ToggleSelectedTool(static tool => tool.ShowInTrayMenu = !tool.ShowInTrayMenu)));
        bar.Controls.Add(CreateToolbarButton("右键", (_, _) => ToggleSelectedTool(static tool => tool.ShowInContextMenu = !tool.ShowInContextMenu)));
        return bar;
    }

    private RoundedPanel BuildSettingsCard()
    {
        var card = new RoundedPanel
        {
            FillColor = UiTheme.Surface,
            Radius = 24,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 12),
            Margin = new Padding(0, 0, 0, 12),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            BackColor = Color.Transparent,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        AddInlineSetting(table, 0, "首选终端", _preferredTerminalSelect);
        AddInlineSetting(table, 2, "备用终端", _fallbackTerminalSelect);
        AddInlineSetting(table, 4, "启动", _launchAtLoginToggleButton);
        card.Controls.Add(table);
        return card;
    }

    private Control BuildFooter()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = UiTheme.Background,
        };

        var saveButton = CreateActionButton("保存", (_, _) => SaveSettings());
        UiTheme.StylePrimaryButton(saveButton);
        saveButton.Width = 120;
        saveButton.Margin = new Padding(8, 8, 0, 0);

        var cancelButton = new RoundedButton
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
        };
        UiTheme.StyleSecondaryButton(cancelButton);
        cancelButton.Width = 100;
        cancelButton.Margin = new Padding(0, 8, 0, 0);

        footer.Controls.Add(saveButton);
        footer.Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        return footer;
    }

    private static RoundedButton CreateActionButton(string text, EventHandler onClick)
    {
        var button = new RoundedButton
        {
            Text = text,
        };
        UiTheme.StyleSecondaryButton(button);
        button.Width = 162;
        button.Margin = new Padding(0, 0, 0, 10);
        button.Click += onClick;
        return button;
    }

    private static RoundedButton CreateToolbarButton(string text, EventHandler onClick, bool primary = false)
    {
        var button = CreateActionButton(text, onClick);
        if (primary)
        {
            UiTheme.StylePrimaryButton(button);
        }

        button.Width = primary ? 88 : 78;
        button.Margin = new Padding(0, 0, 9, 0);
        return button;
    }

    private static void AddInlineSetting(TableLayoutPanel panel, int labelColumn, string labelText, Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = UiTheme.TextSecondary,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 11, 12, 11),
        }, labelColumn, 0);
        control.Margin = new Padding(0, 7, 0, 7);
        control.Anchor = AnchorStyles.Left;
        panel.Controls.Add(control, labelColumn + 1, 0);
    }

    private void LoadConfigValues()
    {
        _preferredTerminalSelect.SelectedItem = EditableConfig.Terminal.Preferred;
        _fallbackTerminalSelect.SelectedItem = EditableConfig.Terminal.Fallback;
        _launchAtLoginEnabled = EditableConfig.Startup.LaunchAtLogin;
        UpdateLaunchAtLoginToggle();
    }

    private void ToggleLaunchAtLoginSetting()
    {
        _launchAtLoginEnabled = !_launchAtLoginEnabled;
        UpdateLaunchAtLoginToggle();
    }

    private void UpdateLaunchAtLoginToggle()
    {
        _launchAtLoginToggleButton.Text = _launchAtLoginEnabled ? "开机启动 ON" : "开机启动 OFF";
        if (_launchAtLoginEnabled)
        {
            UiTheme.StylePrimaryButton(_launchAtLoginToggleButton);
            return;
        }

        UiTheme.StyleSecondaryButton(_launchAtLoginToggleButton);
    }

    private void RefreshToolList()
    {
        if (EditableConfig.Tools.Count > 0 && GetSelectedTool() is null)
        {
            _selectedToolId = EditableConfig.Tools[0].Id;
        }

        _toolCardsPanel.SuspendLayout();
        _toolCardsPanel.Controls.Clear();

        foreach (var tool in EditableConfig.Tools)
        {
            _toolCardsPanel.Controls.Add(BuildToolCard(tool));
        }

        _toolCardsPanel.ResumeLayout();
        ResizeToolCards();
        UpdateToolCommandBarState();
    }

    private RoundedPanel BuildToolCard(ToolConfig tool)
    {
        var selected = string.Equals(tool.Id, _selectedToolId, StringComparison.OrdinalIgnoreCase);
        var card = new RoundedPanel
        {
            FillColor = selected ? UiTheme.SelectionBack : UiTheme.Surface,
            BorderColor = selected ? UiTheme.Accent : UiTheme.Border,
            Radius = 22,
            Height = 92,
            Width = GetToolCardWidth(),
            Margin = new Padding(0, 0, 8, 10),
            Padding = new Padding(16, 12, 16, 12),
            Tag = tool.Id,
            Cursor = Cursors.Hand,
        };

        var nameLabel = CreateCardLabel(tool.DisplayName, UiTheme.TitleFont, UiTheme.TextPrimary, new Point(16, 12));
        var idLabel = CreateCardLabel($"ID  {tool.Id}", UiTheme.SubtitleFont, UiTheme.TextSecondary, new Point(18, 42));
        var commandLabel = CreateCardLabel($"命令  {tool.Command}", UiTheme.SubtitleFont, UiTheme.TextSecondary, new Point(18, 64));
        var hotkeyPill = CreateStatusPill(FormatHotkey(tool), selected ? UiTheme.Accent : UiTheme.SelectionBack, selected ? UiTheme.AccentText : UiTheme.Accent, new Point(360, 14), 138);
        var enabledPill = CreateStatusPill(tool.Enabled ? "启用" : "停用", tool.Enabled ? UiTheme.SelectionBack : UiTheme.HoverBack, tool.Enabled ? UiTheme.Accent : UiTheme.TextSecondary, new Point(520, 14), 70);
        var trayPill = CreateStatusPill(tool.ShowInTrayMenu ? "托盘" : "无托盘", UiTheme.HoverBack, tool.ShowInTrayMenu ? UiTheme.TextPrimary : UiTheme.TextSecondary, new Point(600, 14), 80);
        var menuPill = CreateStatusPill(tool.ShowInContextMenu ? "右键" : "无右键", UiTheme.HoverBack, tool.ShowInContextMenu ? UiTheme.TextPrimary : UiTheme.TextSecondary, new Point(690, 14), 80);

        card.Controls.Add(nameLabel);
        card.Controls.Add(idLabel);
        card.Controls.Add(commandLabel);
        card.Controls.Add(hotkeyPill);
        card.Controls.Add(enabledPill);
        card.Controls.Add(trayPill);
        card.Controls.Add(menuPill);
        WireToolCardSelection(card, tool.Id);
        return card;
    }

    private static Label CreateCardLabel(string text, Font font, Color color, Point location)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            Location = location,
        };
    }

    private static RoundedLabel CreateStatusPill(string text, Color fillColor, Color textColor, Point location, int width)
    {
        return new RoundedLabel
        {
            Text = text,
            Font = UiTheme.SubtitleFont,
            ForeColor = textColor,
            FillColor = fillColor,
            BorderColor = Color.Transparent,
            Radius = 14,
            Width = width,
            Height = 28,
            Location = location,
            BackColor = Color.Transparent,
        };
    }

    private void WireToolCardSelection(Control control, string toolId)
    {
        control.Click += (_, _) => SelectTool(toolId);
        control.DoubleClick += (_, _) =>
        {
            SelectTool(toolId);
            EditSelectedTool();
        };

        foreach (Control child in control.Controls)
        {
            WireToolCardSelection(child, toolId);
        }
    }

    private void SelectTool(string toolId)
    {
        if (string.Equals(_selectedToolId, toolId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _selectedToolId = toolId;
        RefreshToolList();
    }

    private void UpdateToolCommandBarState()
    {
        if (_enabledToggleButton is null)
        {
            return;
        }

        var selectedTool = GetSelectedTool();
        _enabledToggleButton.Text = selectedTool?.Enabled == true ? "停用" : "启用";
        _enabledToggleButton.Invalidate();
    }

    private void ResizeToolCards()
    {
        var width = GetToolCardWidth();
        foreach (Control control in _toolCardsPanel.Controls)
        {
            control.Width = width;
        }
    }

    private int GetToolCardWidth()
    {
        var scrollbarWidth = _toolCardsPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        return Math.Max(760, _toolCardsPanel.ClientSize.Width - scrollbarWidth - 12);
    }

    private static string FormatHotkey(ToolConfig tool)
    {
        if (!tool.Hotkey.Enabled)
        {
            return "未启用";
        }

        var modifiers = string.Join("+", tool.Hotkey.Modifiers);
        return string.IsNullOrWhiteSpace(modifiers)
            ? tool.Hotkey.Key
            : $"{modifiers}+{tool.Hotkey.Key}";
    }

    private void AddTool()
    {
        using var form = new ToolEditorForm(EditableConfig);
        if (form.ShowDialog(this) != DialogResult.OK || form.EditedTool is null)
        {
            return;
        }

        EditableConfig.Tools.Add(form.EditedTool);
        RefreshToolList();
    }

    private void EditSelectedTool()
    {
        var tool = GetSelectedTool();
        if (tool is null)
        {
            MessageBox.Show(
                "请先选择一个工具。",
                "编辑工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var selectedIndex = EditableConfig.Tools.FindIndex(existingTool => string.Equals(existingTool.Id, tool.Id, StringComparison.OrdinalIgnoreCase));
        using var form = new ToolEditorForm(EditableConfig, ConfigCloneHelper.Clone(tool));
        if (form.ShowDialog(this) != DialogResult.OK || form.EditedTool is null)
        {
            return;
        }

        EditableConfig.Tools[selectedIndex] = form.EditedTool;
        RefreshToolList();
    }

    private void RemoveSelectedTool()
    {
        var tool = GetSelectedTool();
        if (tool is null)
        {
            MessageBox.Show(
                "请先选择一个工具。",
                "删除工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirmation = MessageBox.Show(
            $"要删除工具“{tool.DisplayName}”吗？",
            "删除工具",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        EditableConfig.Tools.RemoveAll(existingTool => string.Equals(existingTool.Id, tool.Id, StringComparison.OrdinalIgnoreCase));
        RefreshToolList();
    }

    private void ToggleSelectedTool(Action<ToolConfig> update)
    {
        var tool = GetSelectedTool();
        if (tool is null)
        {
            MessageBox.Show(
                "请先选择一个工具。",
                "更新工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        update(tool);
        RefreshToolList();
    }

    private ToolConfig? GetSelectedTool()
    {
        return EditableConfig.Tools.FirstOrDefault(tool => string.Equals(tool.Id, _selectedToolId, StringComparison.OrdinalIgnoreCase));
    }

    private void SaveSettings()
    {
        try
        {
            EditableConfig.Terminal.Preferred = _preferredTerminalSelect.SelectedItem as string ?? EditableConfig.Terminal.Preferred;
            EditableConfig.Terminal.Fallback = _fallbackTerminalSelect.SelectedItem as string ?? EditableConfig.Terminal.Fallback;
            EditableConfig.Startup.LaunchAtLogin = _launchAtLoginEnabled;

            ConfigValidator.Validate(EditableConfig);

            SavedConfig = ConfigCloneHelper.Clone(EditableConfig);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "设置无效",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
