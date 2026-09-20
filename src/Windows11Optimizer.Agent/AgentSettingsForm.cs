namespace Windows11Optimizer.Agent;

internal sealed class AgentSettingsForm : Form
{
    private readonly TextBox _mini = new();
    private readonly TextBox _full = new();
    private readonly CheckBox _startup = new();
    private readonly Label _status = new();
    private readonly Func<AgentSettings, (bool Success, string Message)> _apply;

    public AgentSettingsForm(
        AgentSettings settings,
        Func<AgentSettings, (bool Success, string Message)> apply)
    {
        _apply = apply;

        Text = "Agente · Windows11Optimizer";
        Width = 520;
        Height = 300;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 5
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        table.Controls.Add(new Label
        {
            Text = "Mini Focus Boost",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);

        _mini.Text = settings.MiniFocusHotkey;
        _mini.Dock = DockStyle.Fill;
        table.Controls.Add(_mini, 1, 0);

        table.Controls.Add(new Label
        {
            Text = "Interfaz completa",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);

        _full.Text = settings.FullUiHotkey;
        _full.Dock = DockStyle.Fill;
        table.Controls.Add(_full, 1, 1);

        _startup.Text = "Iniciar el agente ligero con Windows";
        _startup.Checked = settings.StartWithWindows;
        _startup.AutoSize = true;
        table.SetColumnSpan(_startup, 2);
        table.Controls.Add(_startup, 0, 2);

        _status.AutoSize = true;
        _status.Text =
            "Formato: Ctrl+Alt+Space. Si una combinación está ocupada, no se guardará.";
        table.SetColumnSpan(_status, 2);
        table.Controls.Add(_status, 0, 3);

        var save = new Button
        {
            Text = "Guardar",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };

        save.Click += (_, _) => SaveSettings();
        table.SetColumnSpan(save, 2);
        table.Controls.Add(save, 0, 4);

        Controls.Add(table);
        AgentVisualTheme.Apply(this);
    }

    private void SaveSettings()
    {
        var settings = new AgentSettings
        {
            MiniFocusHotkey = _mini.Text.Trim(),
            FullUiHotkey = _full.Text.Trim(),
            StartWithWindows = _startup.Checked
        };

        var result = _apply(settings);
        _status.Text = result.Message;

        if (result.Success)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
