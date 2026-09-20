using Windows11Optimizer.Core;

namespace Windows11Optimizer.Agent;

internal sealed class MiniFocusForm : Form
{
    private readonly FocusBoostService _focus;
    private readonly ListView _processes = new();
    private readonly Label _status = new();
    private readonly Button _start = new();
    private readonly Button _stop = new();

    public MiniFocusForm(FocusBoostService focus)
    {
        _focus = focus;

        Text = "Mini Focus Boost · Windows11Optimizer";
        Width = 660;
        Height = 500;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 380);

        _status.Dock = DockStyle.Top;
        _status.Height = 44;
        _status.Padding = new Padding(12, 12, 12, 6);

        _processes.Dock = DockStyle.Fill;
        _processes.View = View.Details;
        _processes.FullRowSelect = true;
        _processes.MultiSelect = false;
        _processes.Columns.Add("Proceso", 260);
        _processes.Columns.Add("PID", 75);
        _processes.Columns.Add("RAM", 90);
        _processes.Columns.Add("Prioridad", 110);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8)
        };

        var refresh = new Button { Text = "Actualizar", AutoSize = true };
        _start.Text = "Iniciar Focus Boost";
        _start.AutoSize = true;
        _stop.Text = "Detener";
        _stop.AutoSize = true;

        refresh.Click += (_, _) => RefreshProcesses();
        _start.Click += (_, _) => StartSelected();
        _stop.Click += (_, _) => StopFocus();

        buttons.Controls.Add(refresh);
        buttons.Controls.Add(_start);
        buttons.Controls.Add(_stop);

        Controls.Add(_processes);
        Controls.Add(buttons);
        Controls.Add(_status);

        AgentVisualTheme.Apply(this);

        Shown += (_, _) =>
        {
            RefreshProcesses();
            RefreshStatus();
        };
    }

    private void RefreshProcesses()
    {
        var rows = _focus.GetCandidateProcesses();

        _processes.BeginUpdate();
        _processes.Items.Clear();

        foreach (var row in rows.Where(x => x.IsAllowedTarget))
        {
            var item = new ListViewItem(row.DisplayName)
            {
                Tag = row
            };

            item.SubItems.Add(row.Pid.ToString());
            item.SubItems.Add($"{row.WorkingSetMb:N0} MB");
            item.SubItems.Add(row.Priority);
            _processes.Items.Add(item);
        }

        _processes.EndUpdate();
        RefreshStatus();
    }

    private void StartSelected()
    {
        if (_processes.SelectedItems.Count == 0 ||
            _processes.SelectedItems[0].Tag is not FocusProcessInfo selected)
        {
            MessageBox.Show(
                this,
                "Selecciona primero un proceso activo.",
                "Focus Boost",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            _focus.Start(selected.Pid);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Focus Boost",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void StopFocus()
    {
        try
        {
            _focus.Restore();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Focus Boost",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void RefreshStatus()
    {
        var status = _focus.GetStatus();
        _status.Text = status.IsActive
            ? $"{status.DisplayText}. Prioridad moderada y reversible."
            : "Elige un proceso. Nunca se usa prioridad Alta o Tiempo real.";

        _start.Enabled = !status.IsActive;
        _stop.Enabled = status.IsActive;
    }
}
