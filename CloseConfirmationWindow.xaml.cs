using System.Windows;

namespace RunningPrograms
{
    public partial class CloseConfirmationWindow : ThemedWindow
    {
        internal CloseConfirmationWindow(string question, string preview)
        {
            InitializeComponent();
            Question.Text = question;
            Preview.Text = preview;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
