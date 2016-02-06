using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace Fritz_Status {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private const int GWL_STYLE = -16;
		private const int WS_MAXIMIZEBOX = 0x10000;

		private void Window_SourceInitialized(object sender, EventArgs e) {
			// not needed after all
			//var hwnd = new WindowInteropHelper((Window)sender).Handle;
			//var value = GetWindowLong(hwnd, GWL_STYLE);
			//SetWindowLong(hwnd, GWL_STYLE, value & ~WS_MAXIMIZEBOX);
		}

		public MainWindow() {
			InitializeComponent();

			fbBoxName.Text = "\u26AB\uFE0E";
		}

		private bool isUpdating = false;
		private async void button_Click(object sender, RoutedEventArgs e) {
			if(!isUpdating) { 
				isUpdating = true;

				var fbLoadingIn = (Storyboard)TryFindResource("fbLoadingIn");
				fbLoadingIn.Begin();

				var overview = await FritzStatus.GetStatus();
				var boxInfo = await FritzStatus.GetBoxNameAndOS();

				if(!overview.line[0].pic.IsNullOrWhiteSpace()) {
					Debug.WriteLine(overview.line[0].pic);
				} else {
					Debug.WriteLine(overview.line[0].state);
				}
				Debug.WriteLine(overview.line[0].train_state);
				Debug.WriteLine(overview.line[0].time);
				Debug.WriteLine(overview.line[0].mode);

				Debug.WriteLine(boxInfo.BoxName);
				Debug.WriteLine(boxInfo.BoxOS);

				var fbLoadingOut = (Storyboard)TryFindResource("fbLoadingOut");
				fbLoadingIn.Stop();
				fbLoadingOut.Begin();

				this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;

				isUpdating = false;
			} else {
				Debug.WriteLine("Still updating");
			}			
		}
	}
}
