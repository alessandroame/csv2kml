// See https://aka.ms/new-console-template for more information
using SharpKml.Dom;
using SharpKml.Engine;
using System.Diagnostics;

public static class DataExtensions
{
    private static double ToRadian(this double num)
    {
        return num * Math.PI / 180;
    }

    private static double ToDegree(this double num)
    {
        return num * 180 / Math.PI;
    }

    public static double Distance(this Data from, Data to)
    {
        const double EarthRadius = 6371 * 1000; // Radius of the Earth in kilometers

        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude.ToRadian()) * Math.Cos(from.Longitude.ToRadian());
        double y1 = EarthRadius * Math.Cos(from.Latitude.ToRadian()) * Math.Sin(from.Longitude.ToRadian());
        double z1 = EarthRadius + from.Altitude;

        double x2 = EarthRadius * Math.Cos(to.Latitude.ToRadian()) * Math.Cos(to.Longitude.ToRadian());
        double y2 = EarthRadius * Math.Cos(to.Latitude.ToRadian()) * Math.Sin(to.Longitude.ToRadian());
        double z2 = EarthRadius + to.Altitude;

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;
        // Calculate great circle distance
        double d = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        return d;
    }

    public static void CalculateTiltPan(this SharpKml.Base.Vector from, SharpKml.Base.Vector to,out double pan,out double tilt, out double distance, out double groundDistance)
    {
        const double EarthRadius = 6371*1000; // Radius of the Earth in kilometers
        
        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude.ToRadian()) * Math.Cos(from.Longitude.ToRadian());
        double y1 = EarthRadius * Math.Cos(from.Latitude.ToRadian()) * Math.Sin(from.Longitude.ToRadian());
        double z1 = EarthRadius + from.Altitude.Value;

        double x2 = EarthRadius * Math.Cos(to.Latitude.ToRadian()) * Math.Cos(to.Longitude.ToRadian());
        double y2 = EarthRadius * Math.Cos(to.Latitude.ToRadian()) * Math.Sin(to.Longitude.ToRadian());
        double z2 = EarthRadius + to.Altitude.Value;

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;

        // Calculate pan
        pan = Math.Atan2(dy,dx).ToDegree();

        // Calculate great circle distance
        distance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        // Calculate tilt
        tilt = Math.Acos(dz / distance).ToDegree();
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

    public static double Distance(this SharpKml.Base.Vector from, SharpKml.Base.Vector to)
    {
        const double EarthRadius = 6371 * 1000; // Radius of the Earth in kilometers

        // Convert coordinates to Cartesian
        double x1 = EarthRadius * Math.Cos(from.Latitude.ToRadian()) * Math.Cos(from.Longitude.ToRadian());
        double y1 = EarthRadius * Math.Cos(from.Latitude.ToRadian()) * Math.Sin(from.Longitude.ToRadian());
        double z1 = EarthRadius + from.Altitude.Value;

        double x2 = EarthRadius * Math.Cos(to.Latitude.ToRadian()) * Math.Cos(to.Longitude.ToRadian());
        double y2 = EarthRadius * Math.Cos(to.Latitude.ToRadian()) * Math.Sin(to.Longitude.ToRadian());
        double z2 = EarthRadius + to.Altitude.Value;

        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;
        // Calculate great circle distance
        double d = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
        return d;
    }

    public static SharpKml.Base.Vector ToVector(this Data d)
    {
        return new SharpKml.Base.Vector(d.Latitude,d.Longitude,d.Altitude);
    }

    public static LookAt CreateLookAt(this IEnumerable<Data> data,bool follow, SharpKml.Dom.AltitudeMode altitudeMode, 
        int rangeOffset, SharpKml.Dom.AltitudeMode altitudeMode, 
            int altitudeOffset, int visibleHistorySeconds, int? tilt,int pan
        )
    {
        var bb = new BoundingBox
        {
            West = data.Min(d => d.Longitude),
            East = data.Max(d => d.Longitude),
            North = data.Min(d => d.Latitude),
            South = data.Max(d => d.Latitude),
        };
        SharpKml.Base.Vector lookTo;
        //if (lookAtBoundingboxCenter)
        //{
            lookTo = new SharpKml.Base.Vector(bb.Center.Latitude, bb.Center.Longitude, (data.Min(d => d.Altitude) + data.Max(d => d.Altitude)) / 2);
        //}
        //else
        //{
        //    lookTo = data[Math.min]
        //}
        var bbCenter=new SharpKml.Base.Vector(bb.Center.Latitude,bb.Center.Longitude,
            (data.Min(d => d.Altitude) + data.Max(d=>d.Altitude)));!!!!!
        
        var from = new SharpKml.Base.Vector(bb.North, bb.East, 0); 
        var to = new SharpKml.Base.Vector(bb.South, bb.West, data.Max(d=>d.Altitude));
        var d=from.Distance(to);

        var res = new LookAt();
        res.AltitudeMode = altitudeMode;
        res.Latitude = bb.Center.Latitude;
        res.Longitude = bb.Center.Longitude;
        res.Altitude = Math.Max(altitudeOffset, lookTo.Altitude.Value + altitudeOffset);
        res.Range = Math.Max(minDistance, d*1.6);

        data.Last().ToVector().CalculateTiltPan(lookTo, out var calculatedPan, out var calculatedTilt,out var distance,out var groundDistance);
        if (tilt.HasValue)
        {
            res.Tilt = tilt;
        }
        else{
            var value =160-calculatedTilt;
            res.Tilt = Math.Max(80, value);
            //Debug.WriteLine($"--------------------------------------------------");
            //Debug.WriteLine($"alt lookat {res.Altitude} last {data.Last().ToVector().Altitude.Value + altitudeOffset}");
            //Debug.WriteLine($"tilt {calculatedTilt} -> {value} -> {res.Tilt}");

            //Debug.WriteLine($"distance:{distance} groundDist:{groundDistance} alt:{from.Altitude - to.Altitude}");
            //Debug.WriteLine($"tilt calculated:{calculatedTilt} value:{value} out{res.Tilt}");
        }
        if (follow)
        {
            var panValue = 180-calculatedPan + pan;
            while (panValue > 360) panValue -= 360;
            res.Heading = panValue;
            //Debug.WriteLine($"--------------------------------------------------");
            //Debug.WriteLine($"pan calculated {calculatedPan} offset {pan} -> output {res.Heading}");

        }
        res.GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
        {
            Begin = data.Last().Time.AddSeconds(-visibleHistorySeconds),
            End = data.Last().Time,
        };
        return res;
    }

}