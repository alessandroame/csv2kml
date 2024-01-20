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

    public static void CalculateTiltPan(this Data from, Data to,out double pan,out double tilt)
    {
        const double EarthRadius = 6371; // Radius of the Earth in kilometers
        const double CorrectionFactor = 0.0005; // Altitude correction factor (decreases tilt and increases pan with increasing altitude)

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
  
    public static FlyTo CreateCamera(this Data from, Data to)
    {
        //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
        var res = new FlyTo();
        res.Mode = FlyToMode.Bounce;
        res.Duration = 2;
        Camera cam = new Camera();
        cam.AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround;
        cam.Latitude = from.Latitude;
        cam.Longitude = from.Longitude;
        cam.Altitude = from.Altitude;
        from.CalculateTiltPan(to, out var pan, out var tilt);
        cam.Heading = pan.toDegree();
        cam.Roll = 0;
        cam.Tilt = tilt.toDegree();
        res.View = cam;
        return res;
    }

    static double pan = 0;
    public static FlyTo CreateLookAt(this Data from,Data to,bool follow)
    {
        var res = new FlyTo();
        res.Mode = FlyToMode.Smooth;
        res.Duration = to.Time.Subtract(from.Time).TotalSeconds;
        var lookat = new LookAt();
        lookat.AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround;
        lookat.Latitude = from.Latitude;
        lookat.Longitude = from.Longitude;
        lookat.Altitude = from.Altitude;
        lookat.Range = 120;
        lookat.Tilt = 80;
        if (follow)
        {
            double xDiff = to.Latitude - from.Latitude;
            double yDiff = to.Longitude - from.Longitude;
            var p = Math.Atan2(yDiff, xDiff).toDegree();
            lookat.Heading = p;
        }

        /*lookat.Heading = pan++*10;
        if (pan >= 36) pan = 0;*/
        res.View = lookat;
        return res;
    }

}