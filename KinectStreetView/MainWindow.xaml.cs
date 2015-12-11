using System;
//using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using KinectStreetView.Extensions;

namespace KinectStreetView {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		KinectSensor sensor = null;
		MultiSourceFrameReader frameReader = null;
		KinectBackgroundRemoval.BackgroundRemovalTool bgRemover = null;
		int picWidth = 0, picHeight = 0;
		WriteableBitmap bmp;

		/// <summary>
		/// Timer for timed exposure
		/// </summary>
		System.Timers.Timer photoTimer = new System.Timers.Timer(1000) {
			AutoReset = false,
		};
		/// <summary>
		/// Exposure timeout (seconds)
		/// </summary>
		const int ExpSeconds = 5;

		const int PreviewSeconds = 3;
		System.Timers.Timer photoPreviewTimer = new System.Timers.Timer(PreviewSeconds * 1000) {
			AutoReset = false,
		};

		string path;

		Point translate = new Point(0, 0);
		double scale = 1;
		public MainWindow() {
			InitializeComponent();
			wbStreetView.Source = new Uri(System.IO.Path.GetFullPath("sv.html"));

			sensor = KinectSensor.GetDefault();
			frameReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
			bgRemover = new KinectBackgroundRemoval.BackgroundRemovalTool(sensor.CoordinateMapper);
			path = System.Configuration.ConfigurationManager.AppSettings["path"];
			if (string.IsNullOrWhiteSpace(path)) {
				path = System.IO.Path.GetTempPath();
			}
		}

		private void btnGo_Click(object sender, RoutedEventArgs e) {
			wbStreetView.InvokeScript("moveForward");
		}

		private void btnRightLink_Click(object sender, RoutedEventArgs e) {
			wbStreetView.InvokeScript("turnToLink", "right");
		}

		private void btnLeftLink_Click(object sender, RoutedEventArgs e) {
			wbStreetView.InvokeScript("turnToLink", "left");
		}

		private void btnToggleMap_Click(object sender, RoutedEventArgs e) {
			wbStreetView.InvokeScript("setMapVisible");
		}

		private void btnHideImage_Click(object sender, RoutedEventArgs e) {
			//wbStreetView.Visibility = wbStreetView.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
			imgForeground.RenderTransform = new TranslateTransform(100, 100);
			var pos = System.Windows.Input.Mouse.GetPosition(imgForeground);
		}

		private void wbStreetView_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e) {
			//System.Windows.Resources.StreamResourceInfo sriCurs = Application.GetResourceStream(new Uri("hand.cur", UriKind.Relative));
			//Cursor = new System.Windows.Input.Cursor(sriCurs.Stream);

			frameReader.MultiSourceFrameArrived += FrameReader_MultiSourceFrameArrived;
			sensor.Open();
			pup.Height = wbStreetView.ActualHeight * 0.75;
			pup.Width = wbStreetView.ActualWidth;
			pup.VerticalOffset = wbStreetView.ActualHeight * 0.25;
			pup.IsOpen = true;
			KinectController.GoForward += KinectController_GoForward;
			KinectController.TakePhoto += KinectController_TakePhoto;
			photoTimer.Elapsed += PhotoTimer_Elapsed;
			photoPreviewTimer.Elapsed += PhotoPreviewTimer_Elapsed;
			this.MaximizeToSecondaryMonitor();
			KinectController.LeftHandMoved += KinectController_LeftHandMoved;
			KinectController.Zoom += KinectController_Zoom;
		}

		private void KinectController_Zoom(double scale) {
			this.scale = scale;
			transform();
		}

		private void KinectController_LeftHandMoved(int x, int y) {
			translate = new Point(x, y);
			transform();
		}

		void transform() {
			var m = Matrix.Identity;
			m.Scale(scale, scale);
			m.Translate(translate.X - cForeground.ActualWidth / 2, translate.Y - cForeground.ActualHeight / 2);
			imgForeground.RenderTransformOrigin = new Point(0.5, 0.5);
			imgForeground.RenderTransform = new MatrixTransform(m);
		}

		private void PhotoPreviewTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			Dispatcher.Invoke(new Action(() => {
				imgPreview.Visibility = Visibility.Hidden;
			}));
		}

		int secondsLeft;
		private void PhotoTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			if (--secondsLeft >= 0) {
				Dispatcher.Invoke(new Action(() => {
					tbCountdown.Text = secondsLeft.ToString();
					if (secondsLeft == 0) {
						tbCountdown.Visibility = Visibility.Hidden;
					}
				}));
				photoTimer.Start();
				return;
			}
			photoTimer.Stop();
			Dispatcher.Invoke(new Action(() => {
				ScreenShot();
				wbStreetView.InvokeScript("setControlsVisible", true);
				takingPhoto = false;
				KinectController.Disabled = false;
			}));
		}

		bool takingPhoto = false;
		private void KinectController_TakePhoto(object sender, EventArgs e) {
			if (takingPhoto) {
				return;
			}
			takingPhoto = true;
			KinectController.Disabled = true;
			secondsLeft = ExpSeconds;
			tbCountdown.Visibility = Visibility.Visible;
			tbCountdown.Text = secondsLeft.ToString();
			wbStreetView.InvokeScript("setControlsVisible", false);
			photoTimer.Start();
		}

		private void KinectController_GoForward(object sender, EventArgs e) {
			wbStreetView.InvokeScript("moveForward");
		}

		private void FrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {
			var reference = e.FrameReference.AcquireFrame();

			using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
			using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
			using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
			using (var bodyFrame = reference.BodyFrameReference.AcquireFrame()) {
				if (colorFrame != null && depthFrame != null && bodyIndexFrame != null) {
					if (picWidth == 0 || picHeight == 0) {
						picWidth = depthFrame.FrameDescription.Width;
						picHeight = depthFrame.FrameDescription.Height;
						bmp = new WriteableBitmap(picWidth, picHeight, 96, 96, PixelFormats.Bgra32, null);
						imgForeground.Source = bmp;
						imgForeground.Height = bmp.Height;
						imgForeground.Width = bmp.Width;
						System.Windows.Controls.Canvas.SetLeft(imgForeground, cForeground.ActualWidth / 2 - bmp.Width / 2);
						System.Windows.Controls.Canvas.SetTop(imgForeground, cForeground.ActualHeight / 2 - bmp.Height / 2);
					}
					var bytes = bgRemover.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);
					bmp.Lock();
					Marshal.Copy(bytes, 0, bmp.BackBuffer, bytes.Length);
					bmp.AddDirtyRect(new Int32Rect(0, 0, picWidth, picHeight));
					bmp.Unlock();
				}
				if (bodyFrame != null) {
					KinectController.ProcessBodyFrame(bodyFrame);
				}
			}
		}

		private void ScreenShot() {
			var bitmap = new System.Drawing.Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
										 System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
			System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
			graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
			var fileName = System.IO.Path.Combine(System.IO.Path.GetFullPath(path), DateTime.Now.ToString(@"yyyyMMdd_HHmmss.pn\g"));
			bitmap.Save(fileName, ImageFormat.Png);
			imgPreview.Source = new BitmapImage(new Uri(fileName));
			imgPreview.Visibility = Visibility.Visible;
			photoPreviewTimer.Start();
		}
	}
}
