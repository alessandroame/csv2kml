// See https://aka.ms/new-console-template for more information
using csv2kml;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Diagnostics;

public static class DataExtensions
{

    public static void CalculateFlightPhase(this Data[] data)
    {
        var amount = 10;
        var buffer = new List<double>();
        var lastAltitude = data.FirstOrDefault()?.Altitude ?? 0;
        var i = 0;
        foreach (var d in data)
        {
            if (d.MotorActive)
            {
                buffer.Clear();
                d.FlightPhase = FlightPhase.MotorClimb;
                Console.WriteLine($"#{i} phase:{d.FlightPhase}");
            }
            else
            {
                while (buffer.Count() > amount) buffer.RemoveAt(0);
                buffer.Add(d.Altitude - lastAltitude);
                var weigth = (double)buffer.Count() / amount;
                var acc = buffer.Count()<=1?0:(buffer.Average() * weigth);
                if (acc > 0.1)
                    d.FlightPhase = FlightPhase.Climbing;
                else if (acc > 0.6)
                    d.FlightPhase = FlightPhase.Gliding;
                else
                    d.FlightPhase = FlightPhase.Sinking;
                Console.WriteLine($"#{i} phase:{d.FlightPhase} acc:{acc} ");
            }
            lastAltitude = d.Altitude;
            i++;
        }
    }
    public static void CalculateFlightPhase1(this Data[] data)
    {
        var lookAroundInSeconds = 10;
        for (var i = 0; i < data.Count(); i++)
        {
            if (data[i].MotorActive||data[i - 1].MotorActive)
            { 
                data[i].FlightPhase = FlightPhase.MotorClimb;
                //Console.WriteLine($"#{i} motor speed={data[i].Speed}");
            }
            else
            {
                var dataSet = data.GetDataAroundTime(data[i].Time,lookAroundInSeconds);
                var elapsedSeconds = dataSet.Max(d => d.Time).Subtract(dataSet.Min(d => d.Time)).TotalSeconds;
                //var weight = elapsedSeconds / lookAroundInSeconds / 2;
                var deltaSpeed = dataSet.Max(d => d.Speed) - dataSet.Min(d => d.Speed);
                var avgVSpeed = dataSet.Average(d => d.VerticalSpeed);//*weight;
                //if (i > 90) Debugger.Break();
                if (avgVSpeed>0)
                    data[i].FlightPhase = FlightPhase.Climbing;
                else if (avgVSpeed > -0.8)
                    data[i].FlightPhase = FlightPhase.Gliding;
                else 
                    data[i].FlightPhase = FlightPhase.Sinking;
//                Console.WriteLine($"#{i} phase:{data[i].FlightPhase} speed={data[i].Speed} alt={data[i].Altitude} ds={deltaSpeed} avs={avgVSpeed}");
            }
        }
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

    private static void CalculateTiltPan(this SharpKml.Base.Vector from, SharpKml.Base.Vector to,out double pan,out double tilt, out double distance, out double groundDistance)
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

        SharpKml.Base.Vector lookTo = GetReference(cameraConfig.LookAt);
        SharpKml.Base.Vector alignTo = GetReference(cameraConfig.AlignTo);

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