// See https://aka.ms/new-console-template for more information
using SharpKml.Dom;
using SharpKml.Dom.GX;

public class Data
{
    public Data(DateTime time, double lat, double lon, double alt, double gpsSpeed, double vSpeed)
    {
        Time = time;
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        GPSSpeed = gpsSpeed;
        VSpeed = vSpeed;
    }

    public DateTime Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double VSpeed { get; set; }
    public double GPSSpeed { get; set; }

}

public static class DataExtensions
{
    private static double toRadian(this double num)
    {
        return num * Math.PI / 180;
    }

    private static double toDegree(this double num)
    {
        return num * 180 / Math.PI;
    }

    public static double Distance(this Data from, Data to)
    {
        const double EarthRadius = 6371; // Radius of the Earth in kilometers

        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude) * Math.Cos(from.Longitude);
        double y1 = EarthRadius * Math.Cos(from.Latitude) * Math.Sin(from.Longitude);
        double z1 = EarthRadius * Math.Sin(from.Latitude);

        double x2 = EarthRadius * Math.Cos(to.Latitude) * Math.Cos(to.Longitude);
        double y2 = EarthRadius * Math.Cos(to.Latitude) * Math.Sin(to.Longitude);
        double z2 = EarthRadius * Math.Sin(to.Latitude);

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;
        // Calculate great circle distance
        double d = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        return d;
    }

    public static void CalculateTiltPan(this Data from, Data to,out double pan,out double tilt)
    {
        const double EarthRadius = 6371; // Radius of the Earth in kilometers

        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude) * Math.Cos(from.Longitude);
        double y1 = EarthRadius * Math.Cos(from.Latitude) * Math.Sin(from.Longitude);
        double z1 = EarthRadius * Math.Sin(from.Latitude);

        double x2 = EarthRadius * Math.Cos(to.Latitude) * Math.Cos(to.Longitude);
        double y2 = EarthRadius * Math.Cos(to.Latitude) * Math.Sin(to.Longitude);
        double z2 = EarthRadius * Math.Sin(to.Latitude);

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;
        // Calculate great circle distance
        double d = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy,2) + Math.Pow(dz, 2));

        // Calculate pan
        pan = Math.Atan2(dy,dx);

        // Calculate tilt
        tilt = Math.Acos(dz/d);
    }

    public static FlyTo CreateFlyTo(this Data from, Data to, AbstractView view, FlyToMode flyMode=FlyToMode.Smooth)
    {
        //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
        var res = new FlyTo();
        res.Mode = flyMode;
        res.Duration = to.Time.Subtract(from.Time).TotalSeconds;
        res.View = view;
        return res;
    }

    public static Camera CreateCamera(this Data from, Data to, SharpKml.Dom.AltitudeMode altitudeMode,int altitudeOffset)
    {
        //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
        Camera res = new Camera();
        res.AltitudeMode = altitudeMode;
        res.Latitude = from.Latitude;
        res.Longitude = from.Longitude;
        res.Altitude = from.Altitude+ altitudeOffset;
        from.CalculateTiltPan(to, out var pan, out var tilt);
        res.Heading = pan.toDegree();
        res.Roll = 0;
        res.Tilt = tilt.toDegree();
        return res;
    }

    public static LookAt CreateLookAt(this Data from,Data to,bool follow, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset)
    {
        var res = new LookAt();
        res.AltitudeMode = altitudeMode;
        res.Latitude = (from.Latitude+to.Latitude)/2;
        res.Longitude = (from.Longitude+to.Longitude)/2;
        res.Altitude = to.Altitude+ altitudeOffset;
        res.Range = Math.Max(120, from.Distance(to));
        res.Tilt = 80;
        if (follow)
        {
            double xDiff = to.Latitude - from.Latitude;
            double yDiff = to.Longitude - from.Longitude;
            var p = Math.Atan2(yDiff, xDiff).toDegree() +20;
            res.Heading = p;
        }
        res.GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
        {
            Begin = from.Time.AddSeconds(-30),
            End = from.Time.AddSeconds(1),
        };
        /*lookat.Heading = pan++*10;
        if (pan >= 36) pan = 0;*/
        return res;
    }

}