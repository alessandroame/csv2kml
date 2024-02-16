using SharpKml.Base;
using SharpKml.Dom;

namespace csv2kml
{
    public static partial class KmlExtensions
    {
        public class CameraHelper
        {
            public static Camera CreateCamera(Vector cameraPosition, Vector lookAtPosition, 
                DateTime fromTime, DateTime toTime, double altitudeOffset, out double heading)
            {
                if (!cameraPosition.Altitude.HasValue) throw new ArgumentNullException(nameof(cameraPosition.Altitude));
                cameraPosition.CalculateTiltPan(lookAtPosition, out heading, out var tilt, out var distance, out var groundDistance);
                var res = new Camera
                {
                    Latitude = cameraPosition.Latitude,
                    Longitude = cameraPosition.Longitude,
                    Altitude = Math.Max(20, cameraPosition.Altitude.Value) + altitudeOffset,
                    AltitudeMode = AltitudeMode.Absolute,
                    Heading = heading,
                    Tilt = tilt,
                    GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
                    {
                        Begin = fromTime,
                        End = toTime
                    }
                };
                return res;
            }
            public static LookAt CreateLookAt(Vector lookAtPosition, double range, double heading, double tilt,
                DateTime fromTime, DateTime toTime, double altitudeOffset)
            {
                if (!lookAtPosition.Altitude.HasValue) throw new ArgumentNullException(nameof(lookAtPosition.Altitude));
                var res = new LookAt
                {
                    Latitude = lookAtPosition.Latitude,
                    Longitude = lookAtPosition.Longitude,
                    Altitude = Math.Max(20, lookAtPosition.Altitude.Value) + altitudeOffset,
                    AltitudeMode = AltitudeMode.Absolute,
                    Heading = heading,
                    Tilt = tilt,
                    Range = range,
                    GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
                    {
                        Begin = fromTime,
                        End = toTime
                    }
                };
                return res;
            }
        }
    }
}
