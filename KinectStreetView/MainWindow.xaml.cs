using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

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

		string path;
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

		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			frameReader.MultiSourceFrameArrived += FrameReader_MultiSourceFrameArrived;
			sensor.Open();
			pup.Height = wbStreetView.ActualHeight * 0.75;
			pup.Width = wbStreetView.ActualWidth;
			pup.VerticalOffset = 100;
			pup.IsOpen = true;
			KinectController.GoForward += KinectController_GoForward;
			KinectController.TakePhoto += KinectController_TakePhoto;
		}

		private void KinectController_TakePhoto(object sender, EventArgs e) {
			ScreenShot();
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
			var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
										 Screen.PrimaryScreen.Bounds.Height);
			Graphics graphics = Graphics.FromImage(bitmap);
			graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
			bitmap.Save(System.IO.Path.Combine(path, DateTime.Now.ToString(@"yyyyMMdd_HHmmss.pn\g")), ImageFormat.Png);
		}
	}
}
