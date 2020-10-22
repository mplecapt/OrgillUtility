using System.Windows;

namespace OrgillUtil_v3 {
	/// <summary>
	/// Interaction logic for InputDialog.xaml
	/// </summary>
	public partial class InputDialog : Window {
		public bool Canceled = false;

		public InputDialog(string prompt, string title) {
			InitializeComponent();
			Prompt.Text = prompt;
			Title = title;
			Result.Focus();
		}

		public InputDialog(string prompt, string title, int placeholder) {
			InitializeComponent();
			Prompt.Text = prompt;
			Title = title;
			Result.Text = placeholder.ToString();
			Result.Focus();
			Result.Select(0, Result.Text.Length);
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) {
			Canceled = true;
			Close();
		}

		private void OK_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		public string GetResult() {
			return Result.Text;
		}

		public int GetInt() {
			if (int.TryParse(Result.Text, out var value))
				return value;
			return -1;
		}
	}
}
