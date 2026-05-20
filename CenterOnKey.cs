using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;

namespace CenterOnKey
{
	[PluginName("CenterOnKey")]
	public sealed class CenterOnKeyOutputMode : AbsoluteOutputMode
	{
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

		[Property("Recenter threshold (0 = any pressure)")]
		[DefaultPropertyValue(0U)]
		public uint PressureThreshold { get; set; }

        public int KeyCode { get; set; } = 0x59;

		[Resolved]
		public override IAbsolutePointer Pointer { get; set; }

		public override void Consume(IDeviceReport report)
		{
			this.TryRecenterInputArea(report);
			base.Consume(report);
		}

		private void TryRecenterInputArea(IDeviceReport report)
		{
			ITabletReport tabletReport = report as ITabletReport;
			if (tabletReport != null)
			{
				TabletReference tablet = this.Tablet;
				bool flag;
				if (tablet == null)
				{
					flag = (null != null);
				}
				else
				{
					TabletConfiguration properties = tablet.Properties;
					if (properties == null)
					{
						flag = (null != null);
					}
					else
					{
						TabletSpecifications specifications = properties.Specifications;
						flag = (((specifications != null) ? specifications.Digitizer : null) != null);
					}
				}
				if (flag && base.Input != null)
				{
					bool flag2 = (GetAsyncKeyState(this.KeyCode) & 0x8000) != 0;
					if (flag2 && !this._wasInContact)
					{
						DigitizerSpecifications digitizer = this.Tablet.Properties.Specifications.Digitizer;
						float num = digitizer.Width / digitizer.MaxX;
						float num2 = digitizer.Height / digitizer.MaxY;
						Vector2 vector = new Vector2(tabletReport.Position.X * num, tabletReport.Position.Y * num2);
						base.Input = new Area(base.Input.Width, base.Input.Height, vector, base.Input.Rotation);
						Area input = base.Input;
						string offsetMessage = string.Format(
							"Input area offset: X={0}, Y={1}",
							input.Position.X,
							input.Position.Y);
						Console.WriteLine(offsetMessage);
						Log.Write("CenterOnKey", offsetMessage);
					}
					this._wasInContact = flag2;
					return;
				}
			}
		}

		protected override void OnOutput(IDeviceReport report)
		{
			if (this.Pointer == null)
			{
				return;
			}
			IProximityReport proximityReport = report as IProximityReport;
			if (proximityReport != null)
			{
				IHoverDistanceHandler hoverDistanceHandler = this.Pointer as IHoverDistanceHandler;
				if (hoverDistanceHandler != null)
				{
					hoverDistanceHandler.SetHoverDistance(proximityReport.HoverDistance);
				}
			}
			IEraserReport eraserReport = report as IEraserReport;
			if (eraserReport != null)
			{
				IEraserHandler eraserHandler = this.Pointer as IEraserHandler;
				if (eraserHandler != null)
				{
					eraserHandler.SetEraser(eraserReport.Eraser);
				}
			}
			ITiltReport tiltReport = report as ITiltReport;
			if (tiltReport != null)
			{
				ITiltHandler tiltHandler = this.Pointer as ITiltHandler;
				if (tiltHandler != null)
				{
					tiltHandler.SetTilt(tiltReport.Tilt);
				}
			}
			ITabletReport tabletReport = report as ITabletReport;
			if (tabletReport != null)
			{
				IPressureHandler pressureHandler = this.Pointer as IPressureHandler;
				if (pressureHandler != null)
				{
					TabletReference tablet = this.Tablet;
					if (((tablet != null) ? tablet.Properties.Specifications.Pen : null) != null)
					{
						pressureHandler.SetPressure(tabletReport.Pressure / this.Tablet.Properties.Specifications.Pen.MaxPressure);
					}
				}
			}
			IAbsolutePositionReport absolutePositionReport = report as IAbsolutePositionReport;
			if (absolutePositionReport != null)
			{
				this.Pointer.SetPosition(absolutePositionReport.Position);
			}
			ISynchronousPointer synchronousPointer = this.Pointer as ISynchronousPointer;
			if (synchronousPointer != null)
			{
				if (report is OutOfRangeReport)
				{
					synchronousPointer.Reset();
				}
				synchronousPointer.Flush();
			}
		}

		private bool _wasInContact;
	}
}
