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
    private static double toRadian(double num)
    {
        return num * (Math.PI / 180);
    }

    private static double toDegree(double num)
    {
        return num * (180 / Math.PI);
    }

    public static double CalculateHeading(this Data from, Data to)
    {
        var fromLat = toRadian(from.Latitude);
        var fromLon = toRadian(from.Longitude);
        var toLat = toRadian(to.Latitude);
        var toLon = toRadian(to.Longitude);

        var dLon = toLon - fromLon;
        var x = Math.Tan(toLat / 2 + Math.PI / 4);
        var y = Math.Tan(fromLat / 2 + Math.PI / 4);
        var dPhi = Math.Log(x / y);
        if (Math.Abs(dLon) > Math.PI)
        {
            if (dLon > 0.0)
            {
                dLon = -(2 * Math.PI - dLon);
            }
            else
            {
                dLon = (2 * Math.PI + dLon);
            }
        }

        return (toDegree(Math.Atan2(dLon, dPhi)) + 360) % 360;
    }

    public static double CalculateTilt(this Data from, Data to)
    {
        // Calculate the slope between the two positions
        double slope = to.Altitude - from.Altitude;

        // Normalize the slope to be between -1 and 1
        if (slope < -1)
        {
            slope = -1;
        }
        else if (slope > 1)
        {
            slope = 1;
        }


        return tilt;
    }
  
    public static FlyTo CreateCamera(this Data from, Data to)
    {
        //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
        var res = new FlyTo();
        res.Mode = FlyToMode.Smooth;
        res.Duration = 5;
        Camera cam = new SharpKml.Dom.Camera();
        cam.AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround;
        cam.Latitude = from.Latitude;
        cam.Longitude = from.Longitude;
        cam.Altitude = from.Altitude;
        cam.Heading = from.CalculateHeading(to);
        cam.Roll = 0;
        cam.Tilt = to.CalculateTilt(from);
        res.View = cam;
        return res;
    }

    public static FlyTo CreateLookAt(this Data data)
    {
        var res = new FlyTo();
        res.Mode = FlyToMode.Smooth;
        res.Duration = 2;
        var lookat = new LookAt();
        lookat.AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround;
        lookat.Latitude = data.Latitude;
        lookat.Longitude = data.Longitude;
        lookat.Altitude = data.Altitude;
        lookat.Range = 180;
        lookat.Tilt = 80;
        res.View = lookat;
        return res;
    }

}