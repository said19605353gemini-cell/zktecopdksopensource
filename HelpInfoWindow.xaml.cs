using System.Windows;
using System.Windows.Input;

namespace ZkPdksApp
{
    public partial class HelpInfoWindow : Window
    {
        public HelpInfoWindow(string title, string description, string tip = null, Window owner = null)
        {
            InitializeComponent();
            if (owner != null)
            {
                Owner = owner;
            }

            txtTitle.Text = title;
            txtDescription.Text = description;

            // Dile duyarlı alt başlık ve buton
            string sub = LocalizationService.Get("HelpSubtitle");
            txtSubtitle.Text = !string.IsNullOrEmpty(sub) && sub != "HelpSubtitle" ? sub : "Bu özellik nasıl çalışır ve ne işe yarar?";

            string btnCloseText = LocalizationService.Get("HelpBtnClose");
            if (btnBottomClose != null && !string.IsNullOrEmpty(btnCloseText) && btnCloseText != "HelpBtnClose")
            {
                btnBottomClose.Content = btnCloseText;
            }

            if (!string.IsNullOrWhiteSpace(tip))
            {
                txtTip.Text = tip;
                pnlTip.Visibility = Visibility.Visible;
            }
            else
            {
                pnlTip.Visibility = Visibility.Collapsed;
            }

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        public static void ShowHelp(Window owner, string title, string description, string tip = null)
        {
            var win = new HelpInfoWindow(title, description, tip, owner);
            win.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
