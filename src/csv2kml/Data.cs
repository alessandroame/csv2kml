// See https://aka.ms/new-console-template for more information
using SharpKml.Dom;
using SharpKml.Dom.GX;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;

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
        const double EarthRadius = 6371 * 1000; // Radius of the Earth in kilometers

        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude.toRadian()) * Math.Cos(from.Longitude.toRadian());
        double y1 = EarthRadius * Math.Cos(from.Latitude.toRadian()) * Math.Sin(from.Longitude.toRadian());
        double z1 = EarthRadius + from.Altitude;

        double x2 = EarthRadius * Math.Cos(to.Latitude.toRadian()) * Math.Cos(to.Longitude.toRadian());
        double y2 = EarthRadius * Math.Cos(to.Latitude.toRadian()) * Math.Sin(to.Longitude.toRadian());
        double z2 = EarthRadius + to.Altitude;

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;
        // Calculate great circle distance
        double d = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        return d;
    }

    public static void CalculateTiltPan(this Data from, Data to,out double pan,out double tilt, out double distance, out double groundDistance)
    {
        const double EarthRadius = 6371*1000; // Radius of the Earth in kilometers
        
        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude.toRadian()) * Math.Cos(from.Longitude.toRadian());
        double y1 = EarthRadius * Math.Cos(from.Latitude.toRadian()) * Math.Sin(from.Longitude.toRadian());
        double z1 = EarthRadius + from.Altitude;

        double x2 = EarthRadius * Math.Cos(to.Latitude.toRadian()) * Math.Cos(to.Longitude.toRadian());
        double y2 = EarthRadius * Math.Cos(to.Latitude.toRadian()) * Math.Sin(to.Longitude.toRadian());
        double z2 = EarthRadius + to.Altitude;

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;

        // Calculate pan
        pan = Math.Atan2(dy,dx).toDegree();

        // Calculate great circle distance
        distance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        // Calculate tilt
        tilt = Math.Acos(dz / distance).toDegree();
        groundDistance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
    }

    //public static Camera CreateCamera(this Data from, Data to, SharpKml.Dom.AltitudeMode altitudeMode,int altitudeOffset)
    //{
    //    //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
    //    Camera res = new Camera();
    //    res.AltitudeMode = altitudeMode;
    //    res.Latitude = from.Latitude;
    //    res.Longitude = from.Longitude;
    //    res.Altitude = from.Altitude+ altitudeOffset;
    //    from.CalculateTiltPan(to, out var pan, out var tilt);
    //    res.Heading = pan.toDegree();
    //    res.Roll = 0;
    //    res.Tilt = tilt.toDegree();
    //    return res;
    //}

    public static LookAt CreateLookAt(this Data from,Data to,bool follow, SharpKml.Dom.AltitudeMode altitudeMode, 
            int altitudeOffset, int minDistance,int? tilt,int pan, int lookbackCount
        )
    {
        var res = new LookAt();
        res.AltitudeMode = altitudeMode;
        res.Latitude = from.Latitude;
        res.Longitude = from.Longitude;
        res.Altitude = from.Altitude + altitudeOffset;
        res.Range = minDistance;
        from.CalculateTiltPan(to,out var calculatedPan, out var calculatedTilt,out var distance,out var groundDistance);
        if (tilt.HasValue)
        {
            res.Tilt = tilt;
        }
        else{
            var value = 180-calculatedTilt;
            res.Tilt = Math.Min(70, value);
            //Debug.WriteLine($"--------------------------------------------------");
            //Debug.WriteLine($"alt from {from.Altitude} to {to.Altitude}");
            //Debug.WriteLine($"tilt {calculatedTilt} -> {value} -> {res.Tilt}");
            //Debug.WriteLine($"distance:{distance} groundDist:{groundDistance} alt:{from.Altitude - to.Altitude}");
            //Debug.WriteLine($"tilt calculated:{calculatedTilt} value:{value} out{res.Tilt}");
        }
        if (follow)
        {
            var panValue = 180-calculatedPan + pan;
            res.Heading = panValue;
            //Debug.WriteLine($"--------------------------------------------------");
            //Debug.WriteLine($"pan calculated {calculatedPan} offset {pan} -> output {res.Heading}");

        }
        res.GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
        {
            Begin = from.Time.AddSeconds(-lookbackCount),
            End = from.Time,
        };
        /*lookat.Heading = pan++*10;
        if (pan >= 36) pan = 0;*/
        return res;
    }

}