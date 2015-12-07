using System;
using System.Collections.Generic;
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
using Microsoft.Kinect;

namespace KinectStreetView {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		KinectSensor sensor = null;
		MultiSourceFrameReader frameReader = null;
		KinectBackgroundRemoval.BackgroundRemovalTool bgRemover = null;
		public MainWindow() {
			InitializeComponent();
			wbStreetView.Source = new Uri(System.IO.Path.GetFullPath("sv.html"));
			imgForeground.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("cili.png")));
			sensor = KinectSensor.GetDefault();
			frameReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
			bgRemover = new KinectBackgroundRemoval.BackgroundRemovalTool(sensor.CoordinateMapper);
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
			wbStreetView.Visibility = wbStreetView.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			frameReader.MultiSourceFrameArrived += FrameReader_MultiSourceFrameArrived;
		}

		private void FrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {
			var reference = e.FrameReference.AcquireFrame();

			using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
			using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
			using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame()) {
				if (colorFrame != null && depthFrame != null && bodyIndexFrame != null) {
					var bytes = bgRemover.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);
				}
			}
		}
	}
}
