using System;
using System.Windows;
using System.Windows.Input;

namespace RunningPrograms
{
    public partial class ShortcutWindow : ThemedWindow
    {
        private readonly Func<ShortcutKey, string> save;
        private ShortcutKey shortcut;

        internal ShortcutWindow(ShortcutKey shortcut, string error, Func<ShortcutKey, string> save)
        {
            InitializeComponent();
            this.shortcut = shortcut;
            this.save = save;
            ShortcutInput.Text = shortcut.ToString();
            SetError(error);
            Loaded += (sender, args) => ShortcutInput.Focus();
        }

        private void ShortcutInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys modifiers = Keyboard.Modifiers;
            if ((modifiers == ModifierKeys.None && (key == Key.Enter || key == Key.Escape || key == Key.Tab)) ||
                (modifiers == ModifierKeys.Shift && key == Key.Tab))
            {
                return;
            }

            e.Handled = true;
            if (ShortcutKey.IsValid(key, modifiers))
            {
                shortcut = new ShortcutKey(key, modifiers);
                ShortcutInput.Text = shortcut.ToString();
                SetError(null);
            }
            else if (!ShortcutKey.IsModifierKey(key))
            {
                SetError("Choose a function key other than F12, or use Ctrl, Alt, or Shift with another key.");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string error = save(shortcut);
            if (error == null)
            {
                DialogResult = true;
            }
            else
            {
                SetError(error);
                ShortcutInput.Focus();
            }
        }

        private void SetError(string error)
        {
            ShortcutError.Text = error;
            ShortcutError.Visibility = error == null ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
