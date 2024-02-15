// See https://aka.ms/new-console-template for more information
using csv2kml;
using MathNet.Numerics.Providers.LinearAlgebra;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Diagnostics;
using System.Drawing;

public static partial class DataExtensions
{

    private class AltGain
    {
        public DateTime Time { get; set; }
        public double Gain { get; set; }

        public AltGain(DateTime time, double gain)
        {
            Time = time;
            Gain = gain;
        }
    }
    public static void CalculateFlightPhase(this IEnumerable<Data> data)
    {
        var amountInSeconds = 10;
        var buffer = new List<AltGain>();
        var index = 0;
        var lastAltitude = data.First().Altitude;
        foreach (var d in data)
        {
            if (d.MotorActive)
            {
                buffer.Clear();
                d.FlightPhase = FlightPhase.MotorClimb;
                //Console.WriteLine($"#{i} phase:{d.FlightPhase}");
            }
            else
            {
                while (buffer.Count()>1 && d.Time.Subtract(buffer.First().Time).TotalSeconds > amountInSeconds) buffer.RemoveAt(0);
                buffer.Add(new AltGain(d.Time,d.Altitude-lastAltitude));
                var weigth = buffer.Last().Time.Subtract(buffer.First().Time).TotalSeconds / amountInSeconds;
                var acc = 0D;
                if (buffer.Count() > 1) {
                    acc = buffer.Average(b => b.Gain) * weigth;
                }

                if (acc > 0)
                    d.FlightPhase = FlightPhase.Climb;
                else if (acc > -0.8)
                    d.FlightPhase = FlightPhase.Glide;
                else
                    d.FlightPhase = FlightPhase.Sink;
                //Console.WriteLine($"#{i} phase:{d.FlightPhase} acc:{acc} ");
            }
            lastAltitude = d.Altitude;
            index++;
        }
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
    public static Data[] GetDataByTime(this IEnumerable<Data> data, DateTime from , DateTime to)
    {
        var res=data.Where(d => d.Time >= from && d.Time<to).ToList();
        return res.ToArray();
    }

    public static IEnumerable<Data> GetDataAroundTime(this IEnumerable<Data> data,DateTime when,int aroundInSecond)
    {
        return data.Where(d => d.MotorActive==false && Math.Abs(d.Time.Subtract(when).TotalSeconds) <= aroundInSecond);
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

    public static Vector ToVector(this Data d)
    {
        return new Vector(d.Latitude,d.Longitude,d.Altitude);
    }
   
    public static LookAt CreateLookAt(this IEnumerable<Data> data,bool follow,  
            TourConfig tourConfig, LookAtCameraConfig cameraConfig)
    {
        var bb = new BoundingBox
        {
            West = data.Min(d => d.Longitude),
            East = data.Max(d => d.Longitude),
            North = data.Min(d => d.Latitude),
            South = data.Max(d => d.Latitude),
        };

        SharpKml.Base.Vector GetReference(PointReference reference)
        {
            SharpKml.Base.Vector res = null;
            switch (reference)
            {
                case PointReference.CurrentPoint:
                    res = data.Last().ToVector();
                    break;
                case PointReference.PreviousPoint:
                    var index = Math.Max(0,data.Count() - 2);
                    res = data.ElementAt(index).ToVector();
                    break;
                case PointReference.LastVisiblePoint:
                    res = data.First().ToVector();
                    break;
                case PointReference.BoundingBoxCenter:
                    res = new SharpKml.Base.Vector(
                                    bb.Center.Latitude,
                                    bb.Center.Longitude,
                                    (data.Min(d => d.Altitude) + data.Max(d => d.Altitude)) / 2);
                    break;
                case PointReference.PilotPosition:
                    var pilotPosition = data.First();
                    res = new SharpKml.Base.Vector(
                                    pilotPosition.Latitude,
                                    pilotPosition.Longitude,
                                    (data.Min(d => d.Altitude) + data.Max(d => d.Altitude)) / 2);
                    break;
                default:
                    throw new Exception($"unhandled lookAtReference: {cameraConfig.LookAt}");
                    break;
            }
            return res;
        }

        Vector lookTo = GetReference(cameraConfig.LookAt);
        Vector alignTo = GetReference(cameraConfig.AlignTo);

        var d = lookTo.Distance(alignTo);

        var res = new LookAt();
        res.AltitudeMode = tourConfig.AltitudeMode;
        res.Latitude = lookTo.Latitude;
        res.Longitude = lookTo.Longitude;
        res.Altitude = Math.Max(tourConfig.AltitudeOffset, lookTo.Altitude.Value + tourConfig.AltitudeOffset);
        res.Range = Math.Max(cameraConfig.MinimumRangeInMeters, d*1.6);

        lookTo.CalculateTiltPan(alignTo, out var calculatedPan, out var calculatedTilt,out var distance,out var groundDistance);
        if (cameraConfig.AlignTo == PointReference.PilotPosition) calculatedPan += 180;
        if (cameraConfig.Tilt.HasValue)
        {
            res.Tilt = cameraConfig.Tilt;
        }
        else{
            var tiltValue =160-calculatedTilt;
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
            var panValue = 180-calculatedPan + cameraConfig.PanOffset;
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