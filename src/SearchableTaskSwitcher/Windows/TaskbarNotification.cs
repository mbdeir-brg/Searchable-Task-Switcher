using System;
using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace RunningPrograms
{
    internal sealed class TaskbarNotification : IDisposable
    {
        private readonly Forms.ContextMenuStrip menu;
        private readonly Icon glyph;
        private readonly Forms.NotifyIcon icon;

        internal TaskbarNotification(string title, Stream iconSource, Action show, Action editShortcut, Action exit)
        {
            using (var sourceIcon = new Icon(iconSource, Forms.SystemInformation.SmallIconSize))
            {
                glyph = (Icon)sourceIcon.Clone();
            }

            menu = new Forms.ContextMenuStrip { ShowImageMargin = false };
            menu.Items.Add("Show", null, (sender, args) => show());
            menu.Items.Add("Shortcut…", null, (sender, args) => editShortcut());
            menu.Items.Add("Exit", null, (sender, args) => exit());

            icon = new Forms.NotifyIcon { Icon = glyph, Text = title, ContextMenuStrip = menu };
            icon.DoubleClick += (sender, args) => show();
        }

        internal bool Visible
        {
            get => icon.Visible;
            set => icon.Visible = value;
        }

        internal void ApplyAppearance(bool dark)
        {
            if (dark)
            {
                menu.Renderer = new Forms.ToolStripProfessionalRenderer(new DarkMenuColors()) { RoundedEdges = false };
            }
            else
            {
                menu.RenderMode = Forms.ToolStripRenderMode.System;
            }

            menu.BackColor = dark ? DarkMenuColors.Surface : SystemColors.Menu;
            foreach (Forms.ToolStripItem item in menu.Items)
            {
                item.ForeColor = dark ? Color.FromArgb(242, 243, 245) : SystemColors.MenuText;
            }
        }

        public void Dispose()
        {
            icon.Dispose();
            glyph.Dispose();
            menu.Dispose();
        }

        private sealed class DarkMenuColors : Forms.ProfessionalColorTable
        {
            internal static readonly Color Surface = Color.FromArgb(42, 44, 49);
            public override Color ToolStripDropDownBackground => Surface;
            public override Color MenuBorder => Color.FromArgb(102, 106, 116);
            public override Color MenuItemSelected => Color.FromArgb(40, 76, 114);
            public override Color MenuItemBorder => MenuItemSelected;
        }
    }
}
