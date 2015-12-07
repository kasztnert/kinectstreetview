using System;
using Microsoft.Kinect;

namespace KinectBackgroundRemoval {
	/// <summary>
	/// Provides extension methods for removing the background of a Kinect frame.
	/// </summary>
	public class BackgroundRemovalTool {
		#region Constants

		/// <summary>
		/// The DPI.
		/// </summary>
		readonly double DPI = 96.0;

		/// <summary>
		/// Bytes per pixel.
		/// </summary>
		readonly int BYTES_PER_PIXEL = 4;

		#endregion

		#region Members

		/// <summary>
		/// The depth values.
		/// </summary>
		ushort[] _depthData = null;

		/// <summary>
		/// The body index values.
		/// </summary>
		byte[] _bodyData = null;

		/// <summary>
		/// The RGB pixel values.
		/// </summary>
		byte[] _colorData = null;

		/// <summary>
		/// The RGB pixel values used for the background removal (green-screen) effect.
		/// </summary>
		byte[] _displayPixels = null;

		/// <summary>
		/// The color points used for the background removal (green-screen) effect.
		/// </summary>
		ColorSpacePoint[] _colorPoints = null;

		/// <summary>
		/// The coordinate mapper for the background removal (green-screen) effect.
		/// </summary>
		CoordinateMapper _coordinateMapper = null;

		int colorWidth;
		int colorHeight;

		int depthWidth;
		int depthHeight;

		int bodyIndexWidth;
		int bodyIndexHeight;

		/// <summary>
		/// Distance of head from camera in mms.
		/// </summary>
		ushort headDepth;
		/// <summary>
		/// Cutting distance in mms.
		/// </summary>
		ushort cutDepth;
		/// <summary>
		/// Difference between head distance and cutting distance in mms.
		/// </summary>
		readonly ushort depthCutDifference = 300;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a new instance of BackgroundRemovalTool.
		/// </summary>
		/// <param name="mapper">The coordinate mapper used for the background removal.</param>
		public BackgroundRemovalTool(CoordinateMapper mapper) {
			_coordinateMapper = mapper;
		}

		#endregion

		#region Methods
		bool fileSaved = false;

		/// <summary>
		/// Converts a depth frame to the corresponding System.Windows.Media.Imaging.BitmapSource and removes the background (green-screen effect).
		/// </summary>
		/// <param name="depthFrame">The specified depth frame.</param>
		/// <param name="colorFrame">The specified color frame.</param>
		/// <param name="bodyIndexFrame">The specified body index frame.</param>
		/// <returns>The corresponding System.Windows.Media.Imaging.BitmapSource representation of image.</returns>
		public byte[] GreenScreen(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame, byte[] bgImageData = null) {

			if (_displayPixels == null) {
				colorWidth = colorFrame.FrameDescription.Width;
				colorHeight = colorFrame.FrameDescription.Height;

				depthWidth = depthFrame.FrameDescription.Width;
				depthHeight = depthFrame.FrameDescription.Height;

				bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
				bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;
				_depthData = new ushort[depthWidth * depthHeight];
				_bodyData = new byte[depthWidth * depthHeight];
				_colorData = new byte[colorWidth * colorHeight * BYTES_PER_PIXEL];
				_displayPixels = new byte[depthWidth * depthHeight * BYTES_PER_PIXEL];
				_colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
			}

			//skeletonFrame.GetAndRefreshBodyData(bodies);
			//if (bodies[0].IsTracked) {
			//	headDepth = (ushort)(bodies[0].Joints[JointType.Head].Position.Z * 1000);
			//	cutDepth = (ushort)(headDepth + depthCutDifference);
			//}

			if (((depthWidth * depthHeight) == _depthData.Length)
				&& ((colorWidth * colorHeight * BYTES_PER_PIXEL) == _colorData.Length)
				&& ((bodyIndexWidth * bodyIndexHeight) == _bodyData.Length)) {

				depthFrame.CopyFrameDataToArray(_depthData);

				if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra) {
					colorFrame.CopyRawFrameDataToArray(_colorData);
				} else {
					colorFrame.CopyConvertedFrameDataToArray(_colorData, ColorImageFormat.Bgra);
				}

				bodyIndexFrame.CopyFrameDataToArray(_bodyData);

				_coordinateMapper.MapDepthFrameToColorSpace(_depthData, _colorPoints);

				Array.Clear(_displayPixels, 0, _displayPixels.Length);
				ushort depmax = ushort.MinValue, depmin = ushort.MaxValue;
				if (bgImageData != null) {
					Array.Copy(bgImageData, _displayPixels, bgImageData.Length);
				}
				for (int y = 0; y < depthHeight; ++y) {
					for (int x = 0; x < depthWidth; ++x) {
						int depthIndex = (y * depthWidth) + x;
						var dp = _depthData[depthIndex];
						if (dp > depmax) {
							depmax = dp;
						}
						if (dp < depmin && dp > 0) {
							depmin = dp;
						}

						byte player = _bodyData[depthIndex];

						if (player != 0xff /*&& _depthData[depthIndex] < cutDepth*/) {
							ColorSpacePoint colorPoint = _colorPoints[depthIndex];

							int colorX = (int)Math.Floor(colorPoint.X + 0.5);
							int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

							if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight)) {
								int colorIndex = ((colorY * colorWidth) + colorX) * BYTES_PER_PIXEL;
								int displayIndex = depthIndex * BYTES_PER_PIXEL;

								_displayPixels[displayIndex + 0] = _colorData[colorIndex];
								_displayPixels[displayIndex + 1] = _colorData[colorIndex + 1];
								_displayPixels[displayIndex + 2] = _colorData[colorIndex + 2];
								_displayPixels[displayIndex + 3] = 0xff;
							}
						}
					}
				}
			}
			if (!fileSaved) {
				//ImageFromRawBgraArray(_colorData, colorWidth, colorHeight).Save(@"D:\tomi\tmp\delme\col.png");
				//ImageFromRawBgraArray(_displayPixels, depthWidth, depthHeight).Save(@"D:\tomi\tmp\delme\dep.png");
			}
			//return _colorData;

			return _displayPixels;
		}

		#endregion

	}
}
