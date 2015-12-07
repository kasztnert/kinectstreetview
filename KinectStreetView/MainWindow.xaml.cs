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

namespace KinectStreetView {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			wbStreetView.Source = new Uri(System.IO.Path.GetFullPath("sv.html"));
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
	}
}
