// See https://aka.ms/new-console-template for more information
using csv2kml;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Diagnostics;
using System.Drawing;

public static class DataExtensions
{

    public class AltGain
    {
        public DateTime Time { get; set; }
        public double Gain { get; set; }

        public AltGain(DateTime time, double gain)
        {
            Time = time;
            Gain = gain;
        }
    }
    public static void CalculateFlightPhase(this Data[] data)
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

                if (acc > 0.1)
                    d.FlightPhase = FlightPhase.Climb;
                else if (acc > -0.6)
                    d.FlightPhase = FlightPhase.Glide;
                else
                    d.FlightPhase = FlightPhase.Sink;
                //Console.WriteLine($"#{i} phase:{d.FlightPhase} acc:{acc} ");
            }
            lastAltitude = d.Altitude;
            index++;
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
                    data[i].FlightPhase = FlightPhase.Climb;
                else if (avgVSpeed > -0.8)
                    data[i].FlightPhase = FlightPhase.Glide;
                else 
                    data[i].FlightPhase = FlightPhase.Sink;
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
    public static System.Drawing.Point Position(this Vector vector)
    {
        var res = new System.Drawing.Point();
        const double EarthRadius = 6371 * 1000; // Radius of the Earth in kilometers

        // Convert coordinates to Cartesian
        res.X = (int)(EarthRadius * Math.Cos(vector.Latitude.ToRadian()) * Math.Cos(vector.Longitude.ToRadian()));
        double y1 = (int)(EarthRadius * Math.Cos(vector.Latitude.ToRadian()) * Math.Sin(vector.Longitude.ToRadian()));
        return res;
    }


    public static Vector MoveTo(this Vector vector,double distance,double angle)
    {
        const double earthRadius = 6371000; // Radius of the Earth in meters

        var rad = angle.ToRadian();

        /** http://www.movable-type.co.uk/scripts/latlong.html
    φ is latitude, λ is longitude, 
    θ is the bearing (clockwise from north), 
    δ is the angular distance d/R; 
    d being the distance travelled, R the earth’s radius*
    **/

        var d = distance / earthRadius;
        var lat1 = vector.Latitude.ToRadian();
        var lon1 = vector.Longitude.ToRadian();

        var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(d) + Math.Cos(lat1) * Math.Sin(d) * Math.Cos(rad));

        var lon2 = lon1 + Math.Atan2(Math.Sin(rad) * Math.Sin(d) * Math.Cos(lat1), Math.Cos(d) - Math.Sin(lat1) * Math.Sin(lat2));

        lon2 = (lon2 + 3 * Math.PI) % (2 * Math.PI) - Math.PI; // normalise to -180..+180°

        var res=new Vector( lat2.ToDegree(), lon2.ToDegree(), vector.Altitude.Value);
        return res;
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