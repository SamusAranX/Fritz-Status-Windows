using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Fritz_Status {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private const string FB_BULLET = "\u26AB\uFE0E";
		private const string FB_TRI_DOWN = "▼";
		private const string FB_TRI_UP = "▲";

		private bool isFirstUpdate = true;
		private FritzStatus fritzStatus;

		private DispatcherTimer refreshTimer;
		
		// Colors need fixing. I don't know what FromScRgb is for, but it sure as heck doesn't work with RGB floats.
		private Dictionary<FritzConnectionStatus, Color> StatusColors = new Dictionary<FritzConnectionStatus, Color> {
			{FritzConnectionStatus.NotConnected, Color.FromScRgb(1f, 1f, 0.23f, 0.19f) },
			{FritzConnectionStatus.Training, Color.FromScRgb(1f, 1f, 0.58f, 0f) },
			{FritzConnectionStatus.Connected, Color.FromScRgb(1f, 0.3f, 0.85f, 0.39f) },
			{FritzConnectionStatus.Unknown, Color.FromScRgb(1f, 0.73f, 0.73f, 0.73f) }
		};

		// ProgressBar animations
		private Storyboard fbLoadingIn, fbLoadingOut;

		public MainWindow() {
			InitializeComponent();

			fbLoadingIn = (Storyboard)FindResource("fbLoadingIn");
			fbLoadingOut = (Storyboard)FindResource("fbLoadingOut");

			fritzStatus = new FritzStatus();
		}

		private bool _isUpdating = false;
		private bool isUpdating {
			get { return _isUpdating; }
			set {
				_isUpdating = value;
				// This will spam the Output window with a warning, just ignore it
				if (value) {
					fbLoadingOut.Stop();
					fbLoadingIn.Begin();
				} else {
					fbLoadingIn.Stop();
					fbLoadingOut.Begin();
				}
			}
		}
		
		private async void Window_Loaded(object sender, RoutedEventArgs e) {
			refreshTimer = new DispatcherTimer();
			refreshTimer.Tick += new EventHandler(refreshTimer_Tick);
			refreshTimer.Interval = new TimeSpan(0, 0, 15);

			Debug.WriteLine("Refreshing for the first time");
			await refreshInfo();

			Debug.WriteLine("Starting the DispatcherTimer");
			refreshTimer.Start();
		}

		private async void refreshTimer_Tick(object sender, EventArgs e) {
			Debug.WriteLine("Tick!");
			await refreshInfo();
			Debug.WriteLine("Tock!");
		}

		private async void button_Click(object sender, RoutedEventArgs e) {
			await refreshInfo(true);		
		}

		private async Task refreshInfo(bool manual = false) {
			var manualModStr = manual ? "manual " : ""; // Add "manual" to "Attempting refresh!" when manually refreshing
			Debug.WriteLine($"Attempting {manualModStr}refresh!"); // Interpolated strings are fucking awesome, by the way

			if (!isUpdating) {
				isUpdating = true;
				
				DslOverview status = null;
				BoxInfo boxInfo = new BoxInfo { IsNull = true };


				if (isFirstUpdate) {
					var sid = await fritzStatus.GetSessionID();
					if (sid == null) {
						MessageBox.Show("The request either timed out or simply failed.\nTry restarting the app first, and if that doesn't work, see if the box is faulty.", "Couldn't get session ID", MessageBoxButton.OK, MessageBoxImage.Error);
						Application.Current.Shutdown();
					}

					fritzStatus.SessionID = sid;

				}

				boxInfo = await fritzStatus.GetBoxInfo();
				status = await fritzStatus.GetStatus();

				if (!boxInfo.IsNull && status != null) {
					Debug.WriteLine("We're done refreshing!");

					switch (status.line[0].GetConnectionStatus()) {
						case FritzConnectionStatus.Connected:
							this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
							break;
						case FritzConnectionStatus.Training:
							this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
							break;
						case FritzConnectionStatus.NotConnected:
							this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
							break;
						case FritzConnectionStatus.Unknown:
							this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
							break;
					}

					// Set the image and set isFirstUpdate to false
					if (isFirstUpdate) {
						try {
							fbImage.Source = new BitmapImage(new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Box Images/fritzbox{boxInfo.BoxNumber}.png", UriKind.Absolute));
						} catch {
							fbImage.Source = new BitmapImage(new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Box Images/generic.png", UriKind.Absolute));
						}

						isFirstUpdate = false;
					}

					// Set the box name field
					Run r = new Run($"{FB_BULLET} ");
					r.Foreground = new SolidColorBrush(StatusColors[status.line[0].GetConnectionStatus()]);
					fbBoxName.Inlines.Clear();
					fbBoxName.Inlines.Add(r);
					fbBoxName.Inlines.Add(boxInfo.BoxName);

					// Set the connection info field
					var trainStateString = status.line[0].train_state;
					if (status.line[0].GetConnectionStatus() == FritzConnectionStatus.Connected) {
						trainStateString += $" {status.line[0].time}";
					}
					fbConnInfo.Text = trainStateString;

					// Set the connection speed field
					var speedString = $"{FB_TRI_DOWN} {status.ds_rate} – {status.us_rate} {FB_TRI_UP}";
					fbConnSpeed.Text = speedString;
				}

				isUpdating = false;
			} else {
				Debug.WriteLine("There's still a refresh going on");
			}
			
		}
	}
}
