using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
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

		private const int BOX_INFO_REFRESH_THRESHOLD = 20;
		private int boxInfoRefreshCounter = 19;
		private BoxInfo oldBoxInfo = new BoxInfo { IsNull = true };

		private DispatcherTimer refreshTimer;
		
		private Dictionary<FritzConnectionStatus, Color> StatusColors = new Dictionary<FritzConnectionStatus, Color> {
			{FritzConnectionStatus.NotConnected, Color.FromRgb(255, 58, 48) },
			{FritzConnectionStatus.Training, Color.FromRgb(255, 149, 0) },
			{FritzConnectionStatus.Connected, Color.FromRgb(76, 217, 100) },
			{FritzConnectionStatus.Unknown, Color.FromRgb(185, 185, 187) }
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
			var shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
			await refreshInfo(true, shiftPressed);		
		}

		private async Task refreshInfo(bool manual = false, bool shift = false) {
			var manualModStr = manual ? "manual " : ""; // Add "manual" to "Attempting refresh!" when manually refreshing
			var shiftModStr = shift ? " (holding Shift key)" : "";
			Debug.WriteLine($"Attempting {manualModStr}refresh!{shiftModStr}"); // Interpolated strings are fucking awesome, by the way

			if (!isUpdating) {
				isUpdating = true;

				boxInfoRefreshCounter++;
				var boxInfoThresholdReached = boxInfoRefreshCounter == BOX_INFO_REFRESH_THRESHOLD;
				if (boxInfoThresholdReached)
					boxInfoRefreshCounter = 0;
				
				DslOverview status = null;

				if (isFirstUpdate) {
					var sid = await fritzStatus.GetSessionID();
					if (sid == FritzStatus.INVALID_SID) {
						MessageBox.Show("Fritz!Status currently does not support password protection. To use Fritz!Status, you must remove the password protection of your FRITZ!Box. Sorry about that.", "FRITZ!Box is password protected", MessageBoxButton.OK, MessageBoxImage.Error);
						Application.Current.Shutdown();
					} else if (sid == null) {
						MessageBox.Show("The request either timed out or simply failed.\nTry restarting the app first, and if that doesn't work, see if the box is faulty.", "Couldn't get session ID", MessageBoxButton.OK, MessageBoxImage.Error);
						Application.Current.Shutdown();
					}

					fritzStatus.SessionID = sid;

					isFirstUpdate = false;
				}

				if(oldBoxInfo.IsNull || boxInfoThresholdReached) {
					oldBoxInfo = await fritzStatus.GetBoxInfo();
				}
				status = await fritzStatus.GetStatus();

				if (!oldBoxInfo.IsNull && status != null) {
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

					// Set the image
					try {
						fbImage.Source = new BitmapImage(new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Box Images/fritzbox{oldBoxInfo.BoxNumber}.png", UriKind.Absolute));
					} catch {
						fbImage.Source = new BitmapImage(new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Box Images/generic.png", UriKind.Absolute));
					}

					// Set the box name field
					Run r = new Run($"{FB_BULLET} ");
					r.Foreground = new SolidColorBrush(StatusColors[status.line[0].GetConnectionStatus()]);
					fbBoxName.Inlines.Clear();
					fbBoxName.Inlines.Add(r);
					fbBoxName.Inlines.Add(oldBoxInfo.BoxName);

					// Set the connection info field
					var trainStateString = status.line[0].train_state;
					if (status.line[0].GetConnectionStatus() == FritzConnectionStatus.Connected) {
						trainStateString += $" {status.line[0].time}";
					}
					fbConnInfo.Text = trainStateString;

					// Set the connection speed field
					var speedString = $"{FB_TRI_DOWN} {status.ds_rate} – {status.us_rate} {FB_TRI_UP}";
					fbConnSpeed.Text = speedString;
				} else {
					Debug.WriteLine("Something went wrong while updating");
					Debug.WriteLine(oldBoxInfo.IsNull);
					Debug.WriteLine(status == null);
				}

				isUpdating = false;
			} else {
				Debug.WriteLine("There's still a refresh going on");
			}
			
		}
	}
}
