// See https://aka.ms/new-console-template for more information
using csv2kml;
using csv2kml.CameraDirection;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;

public static partial class DataExtensions
{
    public static IEnumerable<Data> Clip(this IEnumerable<Data> data, int from, int to)
    {
        return data.Skip(from).Take(to - from);
    }
    public static double VerticalSpeed(this IEnumerable<Data> data)
    {
        var from = data.First();
        var to = data.Last();
        var dAlt = to.Altitude - from.Altitude;
        var dTime = to.Time.Subtract(from.Time).TotalSeconds;
        var dVspeed = dAlt / dTime;
        return dVspeed;
    }
    public static double GetDurationInSeconds(this IEnumerable<Data> data, int from, int to)
    {
        return data.ElementAt(to).Time.Subtract(data.ElementAt(from).Time).TotalMilliseconds / 1000;
    }

    public static double GetDurationInSeconds(this IEnumerable<Data> data, DateTime from, DateTime to)
    {
        var res = 1000D;
        var filterdData = data.GetDataByTime(from, to);
        if (filterdData.Count() > 1)
        {
            res = filterdData.Last().Time.Subtract(filterdData.First().Time).TotalMilliseconds;
        }
        return res / 1000;
    }
    public static Data GetDataByTime(this IEnumerable<Data> data, DateTime time)
    {
        var res = data.First(d => d.Time == time);
        return res;
    }

    public static IEnumerable<Data> ExtractSegment(this IEnumerable<Data> data, Segment segment)
    {
        return data.Skip(segment.From).Take(segment.To - segment.From);
    }
    public static Data[] GetDataByTime(this IEnumerable<Data> data, DateTime from, DateTime to)
    {
        var res = data.Where(d => d.Time >= from && d.Time <= to).ToList();
        return res.ToArray();
    }
    public static IEnumerable<Data> GetAroundBySeconds(this Data[] data, int index, int minIndex, int maxIndex, double beforeSeconds, int afterSeconds)
    {
        var res = new List<Data> { data[index] };
        for (var i = index - 1; i > 0 && i >= minIndex; i--)
        {
            if (data[index].Time.Subtract(data[i].Time).TotalSeconds > beforeSeconds) break;
            res.Insert(0, data[i]);
        }
        for (var i = index + 1; i < data.Length && i < maxIndex; i++)
        {
            if (data[i].Time.Subtract(data[index].Time).TotalSeconds > afterSeconds) break;
            res.Add(data[i]);
        }
        return res;
    }
    public static Vector Midpoint(this Data from, Data to,double weigth=0.5)
    {
        return new Vector(
            from.Latitude*weigth + to.Latitude*(1- weigth) ,
            from.Longitude * weigth + to.Longitude * (1 - weigth),
            from.Altitude * weigth + to.Altitude * (1 - weigth));
    }
    public static double Distance(this Data from, Data to)
    {
        const double EarthRadius = 6371 * 1000;

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

    public static Vector ToVector(this Data d)
    {
        return new Vector(d.Latitude, d.Longitude, d.Altitude);
    }

    public static IEnumerable<Data> GetDataAroundTime(this IEnumerable<Data> data, DateTime when, int aroundInSecond)
    {
        return data.Where(d => d.MotorActive == false && Math.Abs(d.Time.Subtract(when).TotalSeconds) <= aroundInSecond);
    }


    public static LookAt CreateLookAt(this IEnumerable<Data> data, bool follow,
            double altitudeOffset, LookAtCameraConfig cameraConfig)
    {
        var bb = new BoundingBox
        {
            West = data.Min(d => d.Longitude),
            East = data.Max(d => d.Longitude),
            North = data.Min(d => d.Latitude),
            South = data.Max(d => d.Latitude),
        };

        Vector GetReference(PointReference reference)
        {
            Vector? res = null;
            switch (reference)
            {
                case PointReference.CurrentPoint:
                    res = data.Last().ToVector();
                    break;
                case PointReference.PreviousPoint:
                    var index = Math.Max(0, data.Count() - 2);
                    res = data.ElementAt(index).ToVector();
                    break;
                case PointReference.LastVisiblePoint:
                    res = data.First().ToVector();
                    break;
                case PointReference.BoundingBoxCenter:
                    res = new Vector(
                                    bb.Center.Latitude,
                                    bb.Center.Longitude,
                                    (data.Min(d => d.Altitude) + data.Max(d => d.Altitude)) / 2);
                    break;
                case PointReference.PilotPosition:
                    var pilotPosition = data.First();
                    res = new Vector(
                                    pilotPosition.Latitude,
                                    pilotPosition.Longitude,
                                    (data.Min(d => d.Altitude) + data.Max(d => d.Altitude)) / 2);
                    break;
                default:
                    throw new Exception($"unhandled lookAtReference: {cameraConfig.LookAt}");
            }
            return res;
        }

        Vector lookTo = GetReference(cameraConfig.LookAt);
        if (!lookTo.Altitude.HasValue) throw new ArgumentNullException(nameof(lookTo.Altitude));

        Vector alignTo = GetReference(cameraConfig.AlignTo);

        var d = lookTo.Distance(alignTo);

        var res = new LookAt();
        res.AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute;
        res.Latitude = lookTo.Latitude;
        res.Longitude = lookTo.Longitude;
        res.Altitude = Math.Max(altitudeOffset, lookTo.Altitude.Value + altitudeOffset);
        res.Range = Math.Max(cameraConfig.MinimumRangeInMeters, d * 1.6);

        lookTo.CalculateTiltPan(alignTo, out var calculatedPan, out var calculatedTilt, out var distance, out var groundDistance);
        if (cameraConfig.AlignTo == PointReference.PilotPosition) calculatedPan += 180;
        if (cameraConfig.Tilt.HasValue)
        {
            res.Tilt = cameraConfig.Tilt;
        }
        else
        {
            var tiltValue = 160 - calculatedTilt;
            while (tiltValue > 360) tiltValue -= 360;
            res.Tilt = Math.Min(80, tiltValue);
            //Debug.WriteLine($"--------------------------------------------------");
            //Debug.WriteLine($"alt lookat {res.Altitude} last {data.Last().ToVector().Altitude.Value + altitudeOffset}");
            //Debug.WriteLine($"tilt {calculatedTilt} -> {tiltValue} -> {res.Tilt}");

            //Debug.WriteLine($"distance:{distance} groundDist:{groundDistance} alt:{from.Altitude - to.Altitude}");
            //Debug.WriteLine($"tilt calculated:{calculatedTilt} value:{value} out{res.Tilt}");
        }
        if (follow)
        {
            var panValue = 180 - calculatedPan + cameraConfig.PanOffset;
            while (panValue > 360) panValue -= 360;
            res.Heading = panValue;
            //Debug.WriteLine($"--------------------------------------------------");
            //Debug.WriteLine($"pan calculated {calculatedPan} offset {pan} -> output {res.Heading}");

        }
        res.GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
        {
            Begin = data.Last().Time.AddSeconds(-cameraConfig.VisibleHistorySeconds),
            End = data.Last().Time,
        };
        return res;
    }

}