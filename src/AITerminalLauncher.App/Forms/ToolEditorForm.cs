using System.Drawing;
using System.Windows.Forms;
using AITerminalLauncher.App.Services;
using AITerminalLauncher.Core.Config;
using AITerminalLauncher.Core.Validation;

namespace AITerminalLauncher.App.Forms;

public sealed class ToolEditorForm : Form
{
    private readonly AppConfig _validationConfig;
    private readonly string? _originalToolId;
    private readonly TextBox _idTextBox;
    private readonly TextBox _displayNameTextBox;
    private readonly TextBox _commandTextBox;
    private readonly TextBox _argsTextBox;
    private readonly CheckBox _enabledCheckBox;
    private readonly CheckBox _showInTrayMenuCheckBox;
    private readonly CheckBox _showInContextMenuCheckBox;
    private readonly CheckBox _hotkeyEnabledCheckBox;
    private readonly HotkeyKeyInput _hotkeyKeyInput;
    private readonly CheckBox _altModifierCheckBox;
    private readonly CheckBox _controlModifierCheckBox;
    private readonly CheckBox _shiftModifierCheckBox;
    private readonly CheckBox _windowsModifierCheckBox;

    public ToolEditorForm(AppConfig validationConfig, ToolConfig? tool = null)
    {
        _validationConfig = ConfigCloneHelper.Clone(validationConfig);
        _originalToolId = tool?.Id;

        Text = tool is null ? "添加工具" : $"编辑工具 - {tool.DisplayName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(600, 624);
        UiTheme.ApplyFormChrome(this);

        _idTextBox = new TextBox();
        _displayNameTextBox = new TextBox();
        _commandTextBox = new TextBox();
        _argsTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };

        _enabledCheckBox = new CheckBox { Text = "启用", AutoSize = true, BackColor = Color.Transparent };
        _showInTrayMenuCheckBox = new CheckBox { Text = "在托盘菜单中显示", AutoSize = true, BackColor = Color.Transparent };
        _showInContextMenuCheckBox = new CheckBox { Text = "在右键菜单中显示", AutoSize = true, BackColor = Color.Transparent };
        _hotkeyEnabledCheckBox = new CheckBox { Text = "启用快捷键", AutoSize = true, BackColor = Color.Transparent };
        _hotkeyKeyInput = new HotkeyKeyInput
        {
            Width = 138,
        };
        _hotkeyKeyInput.KeyCaptured += (_, e) => ApplyCapturedHotkeyModifiers(e.Modifiers);
        _altModifierCheckBox = new CheckBox { Text = "Alt", AutoSize = true, BackColor = Color.Transparent };
        _controlModifierCheckBox = new CheckBox { Text = "Ctrl", AutoSize = true, BackColor = Color.Transparent };
        _shiftModifierCheckBox = new CheckBox { Text = "Shift", AutoSize = true, BackColor = Color.Transparent };
        _windowsModifierCheckBox = new CheckBox { Text = "Win", AutoSize = true, BackColor = Color.Transparent };

        BuildLayout();
        UiTheme.ApplyDeepControlStyles(this);
        LoadTool(tool);
        UpdateHotkeyControlState();

        _hotkeyEnabledCheckBox.CheckedChanged += (_, _) => UpdateHotkeyControlState();
    }

    public ToolConfig? EditedTool { get; private set; }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 22, 24, 20),
            BackColor = UiTheme.Background,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(root, "ID", UiTheme.CreateInputBox(_idTextBox));
        AddRow(root, "显示名称", UiTheme.CreateInputBox(_displayNameTextBox));
        AddRow(root, "命令", UiTheme.CreateInputBox(_commandTextBox));
        AddRow(root, "参数", UiTheme.CreateInputBox(_argsTextBox, 96));

        var flagsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
        };
        flagsPanel.Controls.Add(_enabledCheckBox);
        flagsPanel.Controls.Add(_showInTrayMenuCheckBox);
        flagsPanel.Controls.Add(_showInContextMenuCheckBox);
        AddRow(root, "显示位置", flagsPanel);

        var hotkeyPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
        };
        hotkeyPanel.Controls.Add(_hotkeyEnabledCheckBox);
        hotkeyPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "按键",
            ForeColor = UiTheme.TextSecondary,
            BackColor = Color.Transparent,
            Margin = new Padding(12, 8, 0, 0),
        });
        hotkeyPanel.Controls.Add(_hotkeyKeyInput);
        hotkeyPanel.Controls.Add(_controlModifierCheckBox);
        hotkeyPanel.Controls.Add(_altModifierCheckBox);
        hotkeyPanel.Controls.Add(_shiftModifierCheckBox);
        hotkeyPanel.Controls.Add(_windowsModifierCheckBox);
        AddRow(root, "快捷键", hotkeyPanel);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = UiTheme.Background,
            Margin = new Padding(0, 14, 0, 0),
        };

        var saveButton = new RoundedButton
        {
            Text = "保存",
        };
        UiTheme.StylePrimaryButton(saveButton);
        saveButton.Width = 120;
        saveButton.Margin = new Padding(8, 0, 0, 0);
        saveButton.Click += (_, _) => SaveTool();

        var cancelButton = new RoundedButton
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
        };
        UiTheme.StyleSecondaryButton(cancelButton);
        cancelButton.Width = 100;

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(buttonPanel, 0, root.RowCount);
        root.SetColumnSpan(buttonPanel, 2);
        root.RowCount++;

        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = UiTheme.TextSecondary,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 13, 10, 13),
        }, 0, table.RowCount);
        control.Margin = new Padding(0, 6, 0, 6);
        control.Dock = DockStyle.Top;
        table.Controls.Add(control, 1, table.RowCount);
        table.RowCount++;
    }

    private void LoadTool(ToolConfig? tool)
    {
        var value = tool ?? new ToolConfig
        {
            Enabled = true,
            ShowInContextMenu = true,
            ShowInTrayMenu = true,
            Hotkey = new HotkeyConfig(),
        };

        _idTextBox.Text = value.Id;
        _displayNameTextBox.Text = value.DisplayName;
        _commandTextBox.Text = value.Command;
        _argsTextBox.Lines = value.Args.ToArray();
        _enabledCheckBox.Checked = value.Enabled;
        _showInTrayMenuCheckBox.Checked = value.ShowInTrayMenu;
        _showInContextMenuCheckBox.Checked = value.ShowInContextMenu;
        _hotkeyEnabledCheckBox.Checked = value.Hotkey.Enabled;
        _hotkeyKeyInput.HotkeyKey = value.Hotkey.Key;
        _controlModifierCheckBox.Checked = value.Hotkey.Modifiers.Contains("Control", StringComparer.OrdinalIgnoreCase);
        _altModifierCheckBox.Checked = value.Hotkey.Modifiers.Contains("Alt", StringComparer.OrdinalIgnoreCase);
        _shiftModifierCheckBox.Checked = value.Hotkey.Modifiers.Contains("Shift", StringComparer.OrdinalIgnoreCase);
        _windowsModifierCheckBox.Checked = value.Hotkey.Modifiers.Contains("Windows", StringComparer.OrdinalIgnoreCase);
    }

    private void UpdateHotkeyControlState()
    {
        var enabled = _hotkeyEnabledCheckBox.Checked;
        _hotkeyKeyInput.Enabled = enabled;
        _altModifierCheckBox.Enabled = enabled;
        _controlModifierCheckBox.Enabled = enabled;
        _shiftModifierCheckBox.Enabled = enabled;
        _windowsModifierCheckBox.Enabled = enabled;
    }

    private void SaveTool()
    {
        try
        {
            var tool = BuildTool();
            ValidateTool(tool);

            EditedTool = tool;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "工具无效",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private ToolConfig BuildTool()
    {
        return new ToolConfig
        {
            Id = _idTextBox.Text.Trim(),
            DisplayName = _displayNameTextBox.Text.Trim(),
            Command = _commandTextBox.Text.Trim(),
            Args = _argsTextBox.Lines
                .Select(static line => line.Trim())
                .Where(static line => !string.IsNullOrWhiteSpace(line))
                .ToList(),
            Enabled = _enabledCheckBox.Checked,
            ShowInTrayMenu = _showInTrayMenuCheckBox.Checked,
            ShowInContextMenu = _showInContextMenuCheckBox.Checked,
            Hotkey = new HotkeyConfig
            {
                Enabled = _hotkeyEnabledCheckBox.Checked,
                Key = _hotkeyKeyInput.HotkeyKey.Trim(),
                Modifiers = BuildModifiers(),
            },
        };
    }

    private List<string> BuildModifiers()
    {
        var modifiers = new List<string>();
        if (_controlModifierCheckBox.Checked)
        {
            modifiers.Add("Control");
        }

        if (_altModifierCheckBox.Checked)
        {
            modifiers.Add("Alt");
        }

        if (_shiftModifierCheckBox.Checked)
        {
            modifiers.Add("Shift");
        }

        if (_windowsModifierCheckBox.Checked)
        {
            modifiers.Add("Windows");
        }

        return modifiers;
    }

    private void ApplyCapturedHotkeyModifiers(Keys modifiers)
    {
        _controlModifierCheckBox.Checked = modifiers.HasFlag(Keys.Control);
        _altModifierCheckBox.Checked = modifiers.HasFlag(Keys.Alt);
        _shiftModifierCheckBox.Checked = modifiers.HasFlag(Keys.Shift);
    }

    private void ValidateTool(ToolConfig tool)
    {
        var config = ConfigCloneHelper.Clone(_validationConfig);
        var existingIndex = string.IsNullOrWhiteSpace(_originalToolId)
            ? -1
            : config.Tools.FindIndex(existingTool => string.Equals(existingTool.Id, _originalToolId, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            config.Tools[existingIndex] = ConfigCloneHelper.Clone(tool);
        }
        else
        {
            config.Tools.Add(ConfigCloneHelper.Clone(tool));
        }

        ConfigValidator.Validate(config);
    }
}
