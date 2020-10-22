using System.Windows;

namespace OrgillUtil_v3 {
	/// <summary>
	/// Interaction logic for Dialog.xaml
	/// </summary>
	public partial class Dialog : Window {
		public Dialog(string msg, string title) {
			InitializeComponent();
			Title = title;
			Message.Content = msg;
		}

		private void OK_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}
