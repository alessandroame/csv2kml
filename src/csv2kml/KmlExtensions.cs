using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
            var multiTrack = new MultipleTrack();
            multiTrack.AddTrack(track);
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = multiTrack,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                Description = new Description
                {
                    Text = $"#{trackIndex++} -> {style} " +
                    $"H: {string.Join(" -> ", data.Select(d => Math.Round(d.Altitude)))}"
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
                Description = new Description { Text = $"#{trackIndex++} -> {style}" }
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
            var folder = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            container.AddFeature(folder);
            var coords = new List<Data>();
            var oldNormalizedValue = 0;
            for (var i = 0; i < data.Length - 1; i++)
            {
                var item = data[i];
                coords.Add(item);
                //var delta = 0d;
                //if (i > 0)
                //{
                //    var previousItem = data[i - 1];
                //    delta = item.Altitude - previousItem.Altitude;
                //}
                var nv = data[i + 1].ValueToColorize.Normalize(3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv * subdivision / 2);

                if (oldNormalizedValue != normalizedValue)
                {
                    var p = CreatePlacemarkWithTrack(coords, $"{styleRadix}{oldNormalizedValue}", altitudeMode, altitudeOffset);
                    folder.AddFeature(p);
                    coords = new List<Data> { item };
                    oldNormalizedValue = normalizedValue;
                }
            }
            coords.Add(data[data.Length - 1]);
            var lastPlacemark = CreatePlacemarkWithTrack(coords, $"{styleRadix}{name}{oldNormalizedValue}", altitudeMode, altitudeOffset);

            folder.AddFeature(lastPlacemark);
            //Console.WriteLine($"point count: {data.Length}");
        }

        public static void GenerateLineString(this Container container, Data[] data, string name, int subdivision, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset,string styleRadix)
        {
            var folder = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            container.AddFeature(folder);
            var coords = new List<Data>();
            var oldNormalizedValue = 0;
            for (var i = 0; i < data.Length - 1; i++)
            {
                var item = data[i];
                coords.Add(item);
                //var delta = 0d;
                //if (i > 0)
                //{
                //    var previousItem = data[i - 1];
                //    delta = item.Altitude - previousItem.Altitude;
                //}
                var nv = data[i + 1].ValueToColorize.Normalize(3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv * subdivision / 2);

                if (oldNormalizedValue != normalizedValue)
                {
                    var p = CreatePlacemarkWithLineString(coords, $"{styleRadix}{oldNormalizedValue}", altitudeMode, altitudeOffset);
                    folder.AddFeature(p);
                    coords = new List<Data> { item };
                    oldNormalizedValue = normalizedValue;
                }
            }
            coords.Add(data[data.Length - 1]);
            var lastPlacemark = CreatePlacemarkWithTrack(coords, $"{styleRadix}{oldNormalizedValue}", altitudeMode, altitudeOffset);
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

        public static FlyTo CreateFlyTo(double duration, AbstractView view, FlyToMode flyMode = FlyToMode.Smooth)
        {
            //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
            var res = new FlyTo();
            res.Mode = flyMode;
            res.Duration = duration;
            res.View = view;
            return res;
        }

        public static void GenerateLookBackPath(this Container container,
            Data[] data, string cameraName,
            SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset,
            int frameBeforeStep, bool lookAtBoundingboxCenter, 
            int visibleHistorySeconds,int lookbackSeconds,
            int rangeOffset, int? tilt, int pan, bool follow = false)
        {

            var tourplaylist = new Playlist();

            var maxDeltaHeading = 30;
            var oldHeading = 0d;
            for (int i = 0; i < data.Length - frameBeforeStep; i += frameBeforeStep)
            {
                var dataToShow = new List<Data>();
                for (var n = i; n >= 0; n--)
                {
                    var diff = data[i].Time.Subtract(data[n].Time).TotalSeconds;
                    if (diff > lookbackSeconds) break;
                    dataToShow.Insert(0, data[n]);
                }
                var lookAt = dataToShow.CreateLookAt(follow,rangeOffset,
                    altitudeMode,altitudeOffset,lookAtBoundingboxCenter,visibleHistorySeconds,
                    tilt, pan);

                if (oldHeading != 0)
                {
                    if (oldHeading - lookAt.Heading > maxDeltaHeading) lookAt.Heading = oldHeading - maxDeltaHeading;
                    if (lookAt.Heading - oldHeading > maxDeltaHeading) lookAt.Heading = oldHeading + maxDeltaHeading;
                }
                var duration = dataToShow.Last().Time.Subtract(dataToShow.First().Time).TotalSeconds;
                var flyTo = CreateFlyTo(duration, lookAt, FlyToMode.Smooth);
                tourplaylist.AddTourPrimitive(flyTo);

                oldHeading = lookAt.Heading.HasValue ? lookAt.Heading.Value : 0;
            }
            var tour = new Tour { Name = cameraName };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }
    }
}
