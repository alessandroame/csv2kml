using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static csv2kml.KmlBuilder;
using static DataExtensions;

namespace csv2kml
{
    public static class KmlExtensions
    {
        static int trackIndex = 0;
        public static Placemark CreatePlacemarkWithTrack(this List<Data> data, string style, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset)
        {
            var track = new Track
            {
                AltitudeMode = altitudeMode,
            };
            //var multiTrack = new MultipleTrack();
            //multiTrack.AddTrack(track);
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = track,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                Description = new Description
                {
                    Text = $"#{trackIndex++} -> {style} " +
                    //$"\r\nMotor: {string.Join(" -> ", data.Select(d => d.MotorActive))}" +
                    $"\r\nPhase: {string.Join(" -> ", data.Select(d => d.FlightPhase))}" +
                    $"\r\nAlt: {string.Join(" -> ", data.Select(d => Math.Round(d.Altitude,2)))}" +
                    $"\r\nVSpeed: {string.Join(" -> ", data.Select(d => Math.Round(d.VerticalSpeed,2)))}"
                }
            };
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            foreach (var d in data)
            {
                track.AddWhen(d.Time);
                track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude + altitudeOffset));
            }
            return placemark;
        }

        public static Placemark CreatePlacemarkWithLineString(this List<Data> data, string style, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset)
        {
            var lineString = new LineString
            {
                AltitudeMode = altitudeMode,
                Extrude = true,
                //Tessellate = true,
                Coordinates = new CoordinateCollection(),
            };
            var groundLineString = new LineString
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.ClampToGround,
                //Tessellate = true,
                Coordinates = new CoordinateCollection(),
            };
            var multipleGeometry = new MultipleGeometry();
            multipleGeometry.AddGeometry(lineString);
            multipleGeometry.AddGeometry(groundLineString);
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = multipleGeometry,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                //Description = new Description { Text = $"#{trackIndex++} -> {style} motor:{data.Any(d=>d.MotorActive)}" }
            };
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            //placemark.Viewpoint = data.First().CreateLookAt(data.Last(), true, altitudeMode, altitudeOffset);
            foreach (var d in data)
            {
                lineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude + altitudeOffset));
                groundLineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude + altitudeOffset));
            }
            return placemark;
        }
        public static void GenerateColoredTrack(this Container container, Data[] data, string name, int subdivision, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset, string styleRadix)
        {
            container.GenerateTrack(data, name, subdivision, altitudeMode, altitudeOffset, styleRadix, CreatePlacemarkWithTrack);
        }
        public static void GenerateLineString(this Container container, Data[] data, string name, int subdivision, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset, string styleRadix)
        { 
            container.GenerateTrack(data, name, subdivision, altitudeMode, altitudeOffset, styleRadix, CreatePlacemarkWithLineString);

        }
        public static void GenerateTrack(this Container container, Data[] data, string name, int subdivision, 
            SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset, string styleRadix,
            Func<List<Data>,string, SharpKml.Dom.AltitudeMode, int,Placemark> placemarkGenerator)
        {
            var folder = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            container.AddFeature(folder);
            var coords = new List<Data>();
            var oldStyleIndex = 0;
            var oldMotorActive = data[0].MotorActive;
            var styleId = "";
            for (var i = 0; i < data.Length; i++)
            {
                var item = data[i];

                //TODO Normalize ( by config value )
                var nv = item.VerticalSpeed.Normalize(5);
                var styleIndex = (int)Math.Round(nv * subdivision / 2);

                if (oldMotorActive && !item.MotorActive)
                {
                    styleId = "Motor";
                    var p = placemarkGenerator(coords, $"{styleRadix}{styleId}", altitudeMode, altitudeOffset);
                    folder.AddFeature(p);
                    coords = new List<Data> { coords.Last() };
                    oldStyleIndex = styleIndex;
                    oldMotorActive = data[i].MotorActive;
                }
                else if (!oldMotorActive && oldStyleIndex != styleIndex)
                {
                    styleId = oldStyleIndex.ToString();
                    var p = placemarkGenerator(coords, $"{styleRadix}{styleId}", altitudeMode, altitudeOffset);
                    folder.AddFeature(p);
                    //if (coords.Count() > 3) Debugger.Break();
                    coords = new List<Data> { coords.Last() };
                    oldStyleIndex = styleIndex;
                    oldMotorActive = data[i].MotorActive;
                }
                coords.Add(item);
            }
            var lastPlacemark = placemarkGenerator(coords, $"{styleRadix}{oldStyleIndex}", altitudeMode, altitudeOffset);

            folder.AddFeature(lastPlacemark);
            //Console.WriteLine($"point count: {data.Length}");
        }


        //public static void GenerateCameraPath(this Container container, Data[] data,string cameraName, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset, int frameBeforeStep = 10)
        //{
        //    var tourplaylist = new Playlist();
        //    for (int i = frameBeforeStep; i < data.Length - 1; i += frameBeforeStep)
        //    {
        //        var from = data[i - frameBeforeStep];
        //        var to = data[i - frameBeforeStep + 1];
        //        var flyto = from.CreateCamera(to, altitudeMode,altitudeOffset);

        //        var camera = from.CreateCamera(to, altitudeMode, altitudeOffset);
        //        var flyTo = from.CreateFlyTo(to, camera, FlyToMode.Smooth);
        //        tourplaylist.AddTourPrimitive(flyTo);
        //    }
        //    var tour = new Tour { Name = cameraName };
        //    tour.Playlist = tourplaylist;
        //    container.AddFeature(tour);
        //}


        public static void GenerateTrackTour(this Container container,
            List<Data> data, IEnumerable<Segment> segments, TourConfig tourConfig, LookAtCameraConfig cameraConfig)
        {
            var tourplaylist = new Playlist();
            var currentTime = data.First().Time.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
            var oldHeading = 0D;
            while (true)
            {
                var visibleData = data.GetDataByTime(currentTime.AddSeconds(-cameraConfig.VisibleHistorySeconds), currentTime);
                if (visibleData.Count() == 0) break;


                var currentData = visibleData.Last();
                var duration = data.GetDurationInSeconds(currentData.Time, currentData.Time.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds));



                var i = data.ToList().FindIndex(d=>d.Time==currentData.Time);

                var segment = segments.FirstOrDefault(s => s.From <= i && s.To > i);
                if (segment == null) { break; }
                var segmentData = data.Skip(segment.From).Take(segment.To - segment.From);
                var segmentBB = new BoundingBoxEx(segmentData);
                segmentData.First().ToVector().CalculateTiltPan(segmentData.Last().ToVector(),
                    out var segmentHeading, out var segmentTilt, out var segmentDistance, out var segmentGroungDistance);

                if (segment.Type == FlightPhase.Climb || segment.Type == FlightPhase.MotorClimb)
                {
                    var cameraPos = segmentBB.Center.MoveTo(Math.Max(80,segmentGroungDistance*2), segmentHeading+120);
                    //cameraPos.Altitude = currentData.Altitude+75;
                    var lookAt = currentData.ToVector();
                    var visibleTimeFrom = visibleData.First().Time;
                    if (segmentData.First().Time < visibleTimeFrom) visibleTimeFrom = segmentData.First().Time;
                    var flyTo = new FlyTo
                    {
                        Mode = FlyToMode.Smooth,
                        Duration = duration,
                        View = CreateCamera(cameraPos, lookAt, visibleTimeFrom, currentData.Time, tourConfig,out var heading)
                    };
                    tourplaylist.AddTourPrimitive(flyTo);
                    oldHeading = heading;
                }
                else
                {
                    var visibleDataBB = new BoundingBoxEx(visibleData);
                    currentData.ToVector().CalculateTiltPan(visibleDataBB.Center,
                        out var visibleDataHeading, out var visibleDataTilt, out var visibleDataDistance, out var visibleDataGroungDistance);

                    var heading =visibleDataHeading+30;

                    if (oldHeading != 0)
                    {
                        if (oldHeading - heading > cameraConfig.MaxDeltaHeadingDegrees)
                            heading = oldHeading - cameraConfig.MaxDeltaHeadingDegrees;
                        if (heading - oldHeading > cameraConfig.MaxDeltaHeadingDegrees)
                            heading = oldHeading + cameraConfig.MaxDeltaHeadingDegrees;
                    }


                    var lookAt = currentData.ToVector();
                    var flyTo = new FlyTo
                    {
                        Mode = FlyToMode.Smooth,
                        Duration = duration,
                        View = CreateLookAt(lookAt, Math.Max(80, visibleDataBB.DiagonalSize*1.5), heading, 60, visibleData.First().Time, currentData.Time, tourConfig)
                    };
                    tourplaylist.AddTourPrimitive(flyTo);
                    oldHeading = heading;
                }
                currentTime = currentTime.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
            }
            var tour = new Tour { Name = "Track Tour by flight phase"};
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }

        private static Camera CreateCamera(Vector cameraPosition, Vector lookAtPosition,DateTime fromTime, DateTime toTime,TourConfig tourConfig ,out double heading)
        {
            cameraPosition.CalculateTiltPan(lookAtPosition, out heading, out var tilt, out var distance, out var groundDistance);
            var res= new Camera
            {
                Latitude = cameraPosition.Latitude,
                Longitude = cameraPosition.Longitude,
                Altitude = Math.Max(20, cameraPosition.Altitude.Value) +tourConfig.AltitudeOffset,
                AltitudeMode = tourConfig.AltitudeMode,
                Heading = heading,
                Tilt = tilt,
                GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
                {
                    Begin = fromTime,
                    End = toTime
                }
            };
            return res;
        }
        public static LookAt CreateLookAt(Vector lookAtPosition, double range,double heading, double tilt, DateTime fromTime, DateTime toTime, TourConfig tourConfig)
        {
            var res = new LookAt
            {
                Latitude = lookAtPosition.Latitude,
                Longitude = lookAtPosition.Longitude,
                Altitude = Math.Max(20,lookAtPosition.Altitude.Value) + tourConfig.AltitudeOffset,
                AltitudeMode = tourConfig.AltitudeMode,
                Heading = heading,
                Tilt = tilt,
                Range = range,
                GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
                {
                    Begin = fromTime,
                    End = toTime
                }
            };
            return res;
        }

        //public static void GenerateSegmentsTour__(this Container container, Data[] data, List<Segment> segments)
        //{
        //    var tourplaylist = new Playlist();
        //    foreach (var segment in segments)
        //    {
        //        var dataToShow = data.Skip(segment.From).Take(segment.To - segment.From);
        //        var bb = new BoundingBox
        //        {
        //            West = dataToShow.Min(d => d.Longitude),
        //            East = dataToShow.Max(d => d.Longitude),
        //            North = dataToShow.Min(d => d.Latitude),
        //            South = dataToShow.Max(d => d.Latitude),
        //        };
        //        var duration = dataToShow.Last().Time.Subtract(dataToShow.First().Time).TotalMilliseconds/1000;
        //        var dataCenter= new Vector(
        //                            bb.Center.Latitude,
        //                            bb.Center.Longitude,
        //                            (dataToShow.Min(d => d.Altitude) + dataToShow.Max(d => d.Altitude)) / 2);
                
        //        dataToShow.First().ToVector().CalculateTiltPan(dataToShow.Last().ToVector(), out var pan, out var tilt, out var distance, out var groundDistance);

        //        if (segment.Type == FlightPhase.Climb)
        //        {
        //            pan = pan ;
        //            distance += 60;
        //        }
        //        else
        //        {
        //            pan = 90;
        //            distance = 50;
        //        }

        //        var cameraPosition = dataCenter.MoveTo(distance + 100,pan);//pan+90);
        //        cameraPosition.CalculateTiltPan(dataCenter, out pan, out tilt, out distance, out groundDistance);
        //        Console.WriteLine($"pan{pan}");

        //        if (segment.Type == FlightPhase.Climb)
        //        {
        //            tilt = Math.Max(70, tilt);
        //        }
        //        else
        //        {
        //            tilt = 45;
        //            dataCenter.Altitude += 150;
        //        }

        //        var view = new Camera
        //        {
        //            Latitude = cameraPosition.Latitude,
        //            Longitude = cameraPosition.Longitude,
        //            Altitude = dataCenter.Altitude + 356,
        //            AltitudeMode=SharpKml.Dom.AltitudeMode.Absolute,
        //            Heading = 180-pan,
        //            Tilt = tilt,
        //            GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
        //            {
        //                Begin = dataToShow.First().Time.AddSeconds(-120),
        //                End = dataToShow.Last().Time,
        //            }
        //        };
        //        //var view = new LookAt
        //        //{
        //        //    Latitude = dataCenter.Latitude,
        //        //    Longitude = dataCenter.Longitude,
        //        //    Altitude = dataCenter.Altitude+ 356,
        //        //    AltitudeMode=SharpKml.Dom.AltitudeMode.Absolute,
        //        //    Heading = 180,
        //        //    Tilt = 80,
        //        //    Range=300,
        //        //    GXTimePrimitive = new SharpKml.Dom.GX.TimeSpan
        //        //    {
        //        //        Begin = dataToShow.First().Time.AddSeconds(-60),
        //        //        End = dataToShow.Last().Time,
        //        //    }
        //        //};
        //        FlyToMode fm = FlyToMode.Smooth;
        //        if (segment.Type == FlightPhase.Climb) fm = FlyToMode.Bounce;
        //        var flyTo = new FlyTo
        //        {
        //            Mode = fm,
        //            Duration = duration,
        //            View = view
        //        };
        //        tourplaylist.AddTourPrimitive(flyTo);
        //    }
        //    var tour = new Tour { Name = "Tour analysis" };
        //    tour.Playlist = tourplaylist;
        //    container.AddFeature(tour);
        //}
        public static void GenerateLookBackPath(this Container container,
            Data[] data, TourConfig tourConfig, LookAtCameraConfig cameraConfig, bool follow = false)
        {
            var tourplaylist = new Playlist();

            var oldHeading = 0d;
            for (int i = 0; i < data.Length - cameraConfig.UpdatePositionIntervalInSeconds; i += cameraConfig.UpdatePositionIntervalInSeconds)
            {
                var dataToShow = new List<Data>();
                for (var n = i; n >= 0; n--)
                {
                    var diff = data[i].Time.Subtract(data[n].Time).TotalSeconds;
                    if (diff > cameraConfig.VisibleHistorySeconds) break;
                    dataToShow.Insert(0, data[n]);
                }

                var m = 1;
                while (m < data.Length 
                    && data[m].Time.Subtract(data[i].Time).TotalSeconds< cameraConfig.UpdatePositionIntervalInSeconds) 
                {
                   m++;
                }
                var duration = data[m].Time.Subtract(data[i].Time).TotalMilliseconds/1000;
                var lookAt = dataToShow.CreateLookAt(follow,tourConfig, cameraConfig);
                if (oldHeading != 0)
                {
                    if (oldHeading - lookAt.Heading > cameraConfig.MaxDeltaHeadingDegrees) 
                            lookAt.Heading = oldHeading - cameraConfig.MaxDeltaHeadingDegrees;
                    if (lookAt.Heading - oldHeading > cameraConfig.MaxDeltaHeadingDegrees) 
                            lookAt.Heading = oldHeading + cameraConfig.MaxDeltaHeadingDegrees;
                }
                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration,
                    View = lookAt
                };
                tourplaylist.AddTourPrimitive(flyTo);
                oldHeading = lookAt.Heading.HasValue ? lookAt.Heading.Value : 0;
            }
            var tour = new Tour { Name = cameraConfig.Name };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }
    }
}
