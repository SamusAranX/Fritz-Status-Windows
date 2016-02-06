using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Fritz_Status {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		public MainWindow() {
			InitializeComponent();
		}

		private bool isUpdating = false;
		private async void button_Click(object sender, RoutedEventArgs e) {
			if(!isUpdating) {
				isUpdating = true;
				var overview = await FritzStatus.GetStatus();
				var boxInfo = await FritzStatus.GetBoxNameAndOS();

				if(overview.line[0].pic.IsNullOrWhiteSpace()) {
					Debug.WriteLine(overview.line[0].pic);
				} else {
					Debug.WriteLine(overview.line[0].state);
				}
				
				Debug.WriteLine(boxInfo.BoxName);
				Debug.WriteLine(boxInfo.BoxOS);
				isUpdating = false;
			} else {
				Debug.WriteLine("");
			}			
		}
	}
}
