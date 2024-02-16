using SharpKml.Base;
using SharpKml.Dom;

namespace csv2kml
{
    public static class VectorExtensions
    {
        const double EarthRadiusInMeters = 6371000;

        public static Vector MoveTo(this Vector vector, double distance, double angle)
        {
            var rad = angle.ToRadian();

            var d = distance / EarthRadiusInMeters;
            var lat1 = vector.Latitude.ToRadian();
            var lon1 = vector.Longitude.ToRadian();

            var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(d) + Math.Cos(lat1) * Math.Sin(d) * Math.Cos(rad));
            var lon2 = lon1 + Math.Atan2(Math.Sin(rad) * Math.Sin(d) * Math.Cos(lat1), Math.Cos(d) - Math.Sin(lat1) * Math.Sin(lat2));
            lon2 = (lon2 + 3 * Math.PI) % (2 * Math.PI) - Math.PI; // normalise to -180..+180°

            Vector res;
            if (vector.Altitude.HasValue) {
                res = new Vector(lat2.ToDegree(), lon2.ToDegree(), vector.Altitude.Value);
            }
            else
            {
                res = new Vector(lat2.ToDegree(), lon2.ToDegree());
            }
            return res;
        }

        public static void CalculateTiltPan(this SharpKml.Base.Vector from, SharpKml.Base.Vector to, out double pan, out double tilt, out double distance, out double groundDistance)
        {
            if (!from.Altitude.HasValue) throw new ArgumentNullException(nameof(from.Altitude));
            if (!to.Altitude.HasValue) throw new ArgumentNullException(nameof(to.Altitude));

            double x1 = EarthRadiusInMeters * Math.Cos(from.Latitude.ToRadian()) * Math.Cos(from.Longitude.ToRadian());
            double y1 = EarthRadiusInMeters * Math.Cos(from.Latitude.ToRadian()) * Math.Sin(from.Longitude.ToRadian());
            double z1 = EarthRadiusInMeters + from.Altitude.Value;

            double x2 = EarthRadiusInMeters * Math.Cos(to.Latitude.ToRadian()) * Math.Cos(to.Longitude.ToRadian());
            double y2 = EarthRadiusInMeters * Math.Cos(to.Latitude.ToRadian()) * Math.Sin(to.Longitude.ToRadian());
            double z2 = EarthRadiusInMeters + to.Altitude.Value;

            var dx = x2 - x1;
            var dy = y2 - y1;
            var dz = z2 - z1;

            pan = 180-Math.Atan2(dy, dx).ToDegree();

            distance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
            tilt = 180-Math.Acos(dz / distance).ToDegree();
            groundDistance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
        }

        public static double Distance(this SharpKml.Base.Vector from, SharpKml.Base.Vector to)
        {
            if (!from.Altitude.HasValue) throw new ArgumentNullException(nameof(from.Altitude));
            if (!to.Altitude.HasValue) throw new ArgumentNullException(nameof(to.Altitude));
            
            double x1 = EarthRadiusInMeters * Math.Cos(from.Latitude.ToRadian()) * Math.Cos(from.Longitude.ToRadian());
            double y1 = EarthRadiusInMeters * Math.Cos(from.Latitude.ToRadian()) * Math.Sin(from.Longitude.ToRadian());
            double z1 = EarthRadiusInMeters + from.Altitude.Value;
            
            double x2 = EarthRadiusInMeters * Math.Cos(to.Latitude.ToRadian()) * Math.Cos(to.Longitude.ToRadian());
            double y2 = EarthRadiusInMeters * Math.Cos(to.Latitude.ToRadian()) * Math.Sin(to.Longitude.ToRadian());
            double z2 = EarthRadiusInMeters + to.Altitude.Value;

            var dx = x2 - x1;
            var dy = y2 - y1;
            var dz = z2 - z1;
            double d = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
            return d;
        }

    }
}
