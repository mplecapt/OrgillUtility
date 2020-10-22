using System.Windows;
using System.Windows.Input;

namespace OrgillUtil_v3 {
	/// <summary>
	/// Interaction logic for LoginDialog.xaml
	/// </summary>
	public partial class LoginDialog : Window {
		private MainWindow caller;

		public LoginDialog(MainWindow caller) {
			InitializeComponent();
			textBoxEmail.Focus();
			this.caller = caller;
		}
		public void Login_Click(object sender, RoutedEventArgs args) {
			if (textBoxEmail.Text.Length == 0) {
				new Dialog("Enter a username.", "Warning").ShowDialog();
				textBoxEmail.Focus();
			} else if (passwordBox1.Password.Length == 0) {
				new Dialog("Enter a password.", "Warning").ShowDialog();
				passwordBox1.Password = "";
				passwordBox1.Focus();
			} else {
				Mouse.OverrideCursor = Cursors.Wait;
				if (caller.tryLogin(textBoxEmail.Text, passwordBox1.Password)) {
					Close();
				} else {
					new Dialog("Invalid login, please try again.", "Warning").ShowDialog();
					passwordBox1.Password = "";
					passwordBox1.Focus();
				}
				Mouse.OverrideCursor = Cursors.Arrow;
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) {
			caller.Close();
			Close();
		}
	}
}
