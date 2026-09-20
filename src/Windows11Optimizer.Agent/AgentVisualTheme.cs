namespace Windows11Optimizer.Agent;

internal static class AgentVisualTheme
{
    private static readonly Color Background = Color.FromArgb(12, 14, 18);
    private static readonly Color Surface = Color.FromArgb(21, 24, 30);
    private static readonly Color SurfaceAlt = Color.FromArgb(28, 32, 39);
    private static readonly Color Foreground = Color.FromArgb(246, 247, 249);
    private static readonly Color Muted = Color.FromArgb(167, 175, 187);
    private static readonly Color Border = Color.FromArgb(44, 50, 60);
    private static readonly Color Accent = Color.FromArgb(94, 161, 255);

    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Foreground;
        form.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

        ApplyControlTree(form);
    }

    public static void Apply(ContextMenuStrip menu)
    {
        menu.BackColor = Surface;
        menu.ForeColor = Foreground;
        menu.ShowImageMargin = false;
        menu.RenderMode = ToolStripRenderMode.Professional;
        menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());

        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = Surface;
            item.ForeColor = Foreground;
        }
    }

    private static void ApplyControlTree(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Border;
                    button.FlatAppearance.BorderSize = 1;
                    button.BackColor = SurfaceAlt;
                    button.ForeColor = Foreground;
                    button.Padding = new Padding(8, 3, 8, 3);
                    break;

                case TextBox textBox:
                    textBox.BackColor = Surface;
                    textBox.ForeColor = Foreground;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListView listView:
                    listView.BackColor = Surface;
                    listView.ForeColor = Foreground;
                    listView.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case Label label:
                    label.ForeColor = Foreground;
                    label.BackColor = Color.Transparent;
                    break;

                case CheckBox checkBox:
                    checkBox.ForeColor = Foreground;
                    checkBox.BackColor = Color.Transparent;
                    break;

                case Panel panel:
                    panel.BackColor = Background;
                    break;

                case TableLayoutPanel table:
                    table.BackColor = Background;
                    break;

                case FlowLayoutPanel flow:
                    flow.BackColor = Background;
                    break;
            }

            if (control.HasChildren)
                ApplyControlTree(control);
        }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Surface;
        public override Color MenuItemSelected => SurfaceAlt;
        public override Color MenuItemBorder => Border;
        public override Color MenuBorder => Border;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color MenuItemPressedGradientBegin => SurfaceAlt;
        public override Color MenuItemPressedGradientMiddle => SurfaceAlt;
        public override Color MenuItemPressedGradientEnd => SurfaceAlt;
        public override Color MenuItemSelectedGradientBegin => SurfaceAlt;
        public override Color MenuItemSelectedGradientEnd => SurfaceAlt;
        public override Color ToolStripBorder => Border;
        public override Color ToolStripGradientBegin => Surface;
        public override Color ToolStripGradientMiddle => Surface;
        public override Color ToolStripGradientEnd => Surface;
        public override Color ButtonSelectedBorder => Accent;
    }
}
