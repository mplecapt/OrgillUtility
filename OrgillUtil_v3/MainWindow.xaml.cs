using System;
using System.Windows;
using System.Windows.Documents;

using System.Windows.Media;

namespace OrgillUtil_v3 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public OrgillHandler handler;
		public Processor proc;

		public MainWindow() {
			InitializeComponent();
			handler = new OrgillHandler();
			handler.Init();
#if DEBUG
			ExpanderControl.IsExpanded = true;
#endif
			new LoginDialog(this).ShowDialog();
		}

		private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) {
			SizeToContent = SizeToContent.Height;
		}

		public bool tryLogin(string user, string pass) {
			if (handler.Login(user, pass)) {
				proc = new Processor(this, handler);
				proc.Start();
				return true;
			} else {
				return false;
			}
		}

		/****** Console Utility ******/
		public void print(string msg) {
			print(msg, Color.FromRgb(184, 184, 184));
		}

		public void println(string msg) {
			print(msg + "\n");
		}

		public void print(string msg, Color color) {
			console.Inlines.Add(new Run { Text = time(), Foreground = new SolidColorBrush(Colors.Green) });
			console.Inlines.Add(new Run { Text = msg, Foreground = new SolidColorBrush(color) });
			richTextBox.ScrollToEnd();
		}

		public void println(string msg, Color color) {
			print(msg + "\n", color);
		}

		/**** End Console Utility ****/
		public static string time() {
			DateTime dt = DateTime.Now;
			return dt.ToString("[hh:mm:ss tt] ");
		}
	}
}
