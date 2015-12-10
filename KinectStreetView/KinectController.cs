using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Kinect;

namespace KinectStreetView {
	class KinectController {
		static Body[] bodies = null;
		static int ScreenWidth = Screen.PrimaryScreen.Bounds.Width;
		static int ScreenHeight = Screen.PrimaryScreen.Bounds.Height;
		static HandState lastHandState = HandState.Unknown;
		static DateTime lastHandStateChange = DateTime.Now;
		static bool mouseDownSent = false;
		static bool goForwardSent = false;
		static bool takePhotoSent = false;

		public static event EventHandler GoForward;
		public static event EventHandler TakePhoto;
		/// <summary>
		/// The max duration of a mouse click
		/// </summary>
		const int MouseClickInterval = 500;
		public static void ProcessBodyFrame(BodyFrame frame) {
			if (frame.BodyCount == 0) {
				return;
			}
			bodies = new Body[frame.BodyCount];
			frame.GetAndRefreshBodyData(bodies);
			// only the person first engaged can control the app
			var body = bodies.FirstOrDefault(b => b.IsTracked);
			if (body == null) {
				return;
			}
			var rhpos = body.Joints[JointType.HandRight].Position;
			var lhpos = body.Joints[JointType.HandLeft].Position;
			var splinebasepos = body.Joints[JointType.SpineBase].Position;
			var headpos = body.Joints[JointType.Head].Position;
			var vscreenHeight = headpos.Y - splinebasepos.Y;
			var vscreenWidth = vscreenHeight;
			// right hand raised, mouse control
			if (rhpos.Z < splinebasepos.Z - 0.3 && lhpos.Z > splinebasepos.Z - 0.3) {
				var cursX = rhpos.X;
				if (cursX < splinebasepos.X - vscreenWidth / 2) {
					cursX = splinebasepos.X - (float)vscreenWidth / 2;
				} else if (cursX > splinebasepos.X + vscreenWidth / 2) {
					cursX = splinebasepos.X + (float)vscreenWidth / 2;
				}
				cursX += (float)vscreenWidth / 2 - splinebasepos.X;
				int x = (int)(ScreenWidth * (cursX / vscreenWidth));

				var cursY = rhpos.Y;
				if (cursY < splinebasepos.Y) {
					cursY = splinebasepos.Y;
				} else if (cursY > headpos.Y) {
					cursY = headpos.Y;
				}
				cursY -= splinebasepos.Y;
				int y = ScreenHeight - (int)(ScreenHeight * (cursY / vscreenHeight));
				MouseControl.SetCursorPos(x, y);

				if (lastHandState == HandState.Closed && body.HandRightState == HandState.Closed
					&& (DateTime.Now - lastHandStateChange).Milliseconds > MouseClickInterval && !mouseDownSent) {

					mouseDownSent = true;
					MouseControl.MouseLeftDown();
				} else if (body.HandRightState != lastHandState) {
					switch (body.HandRightState) {
					case HandState.Open:
						if (lastHandState == HandState.Closed) {
							if ((DateTime.Now - lastHandStateChange).Milliseconds < MouseClickInterval) {
								MouseControl.DoMouseClick();
							} else {
								MouseControl.MouseLeftUp();
							}
						}
						mouseDownSent = false;
						break;
					case HandState.Closed:
						break;
					case HandState.Lasso:
					case HandState.Unknown:
					case HandState.NotTracked:
						if (mouseDownSent) {
							MouseControl.MouseLeftUp();
							mouseDownSent = false;
						}
						break;
					default:
						break;
					}
					lastHandState = body.HandRightState;
					lastHandStateChange = DateTime.Now;
				}
			} else {
				lastHandState = HandState.Unknown;
			}
			var lAnkle = body.Joints[JointType.AnkleLeft];
			var rAnkle = body.Joints[JointType.AnkleRight];

            if (lAnkle.TrackingState == TrackingState.Tracked
				&& rAnkle.TrackingState == TrackingState.Tracked) {

				if (Math.Abs(lAnkle.Position.Z - rAnkle.Position.Z) < vscreenHeight / 2) {
					if (!goForwardSent) {
						if (GoForward != null) {
							GoForward(null, new EventArgs());
						}
						goForwardSent = true;
					}
				} else {
					goForwardSent = false;
				}
			}

			if (body.Joints[JointType.ElbowRight].Position.Y >= headpos.Y) {
				if (!takePhotoSent && TakePhoto != null) {
					TakePhoto(null, new EventArgs());
				}
				takePhotoSent = true;
			} else {
				takePhotoSent = false;
			}
		}
	}
}
