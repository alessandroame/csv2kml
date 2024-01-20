using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml
{
    public static class KmlTrackExtensions
    {
        private static void BuildStyles(this Container container, string name, int subdivisions)
        {
            container.AddStyle(new Style
            {
                Id = "hiddenChildren",
                List = new ListStyle
                {
                    ItemType = ListItemType.CheckHideChildren
                }
            });
            for (var i = 0; i <= subdivisions; i++)
            {
                var styleId = $"{name}{i - subdivisions / 2}";
                var k = (float)i / subdivisions;
                var color = k.ToColor();
                var c = $"FF{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                Console.WriteLine($"{styleId} -> {c}");
                container.AddStyle(new Style
                {
                    Id = styleId,
                    Line = new LineStyle
                    {
                        Color = Color32.Parse(c),
                        Width = 3
                    },
                    Icon = new IconStyle
                    {
                        Scale = 0
                    }
                });
            }
        }

        static int trackIndex = 0;
        public static Placemark CreatePlacemark(this List<Data> data, string style)
        {
            var track = new Track
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround,
            };
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = track,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                Description = new Description { Text = $"#{trackIndex++} -> {style}" }
            };
            foreach (var d in data)
            {
                track.AddWhen(d.Time);
                track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude));
            }
            return placemark;
        }

        public static void GenerateColoredTrack(this Container container, Data[] data, string name, int subdivision)
        {
            var folder = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            container.AddFeature(folder);
            var min = data.Min(d => d.VSpeed);
            var max = data.Max(d => d.VSpeed);
            container.BuildStyles("vspeed", subdivision);
            var coords = new List<Data>();
            var oldNormalizedValue = 0;
            for (var i = 0; i < data.Length - 1; i++)
            {
                var item = data[i];
                var nextItem = data[i + 1];
                //var delta = nextItem.Altitude - item.Altitude;
                var delta = item.VSpeed;
                coords.Add(item);
                var nv = delta.Normalize(3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv * subdivision / 2);

                if (oldNormalizedValue != normalizedValue)
                {
                    var p = CreatePlacemark(coords, $"vspeed{oldNormalizedValue}");
                    folder.AddFeature(p);
                    coords = new List<Data> { item };
                    oldNormalizedValue = normalizedValue;
                }
            }
            coords.Add(data[data.Length - 1]);
            var lastPlacemark = CreatePlacemark(coords, $"{name}{oldNormalizedValue}");
            folder.AddFeature(lastPlacemark);
            Console.WriteLine($"point count: {data.Length}");
        }

        public static void GenerateCameraPath(this Container container, Data[] data,string cameraName, int frameBeforeStep = 10)
        {
            var tourplaylist = new Playlist();
            for (int i = frameBeforeStep; i < data.Length - 1; i += frameBeforeStep)
            {
                var from = data[i - frameBeforeStep];
                var to = data[i - frameBeforeStep + 1];
                var flyto = from.CreateCamera(to);
                tourplaylist.AddTourPrimitive(flyto);
            }
            var tour = new Tour { Name = cameraName };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);

            tourplaylist = new Playlist();
            for (int i = frameBeforeStep; i < data.Length - 1; i += frameBeforeStep)
            {
                var from = data[i - frameBeforeStep];
                var to = data[i - frameBeforeStep + 1];
                var flyto = to.CreateCamera(from);
                tourplaylist.AddTourPrimitive(flyto);
            }
            tour = new Tour { Name = cameraName + " -> inverse heading" };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }
        public static void GenerateLookPath(this Container container, 
            Data[] data,string cameraName, 
            int frameBeforeStep = 10, bool follow = false)
        {

            var tourplaylist = new Playlist();

            for (int i = 0; i < data.Length- frameBeforeStep; i += frameBeforeStep)
            {
                var flyto = data[i].CreateLookAt(data[i+ frameBeforeStep],follow);
                tourplaylist.AddTourPrimitive(flyto);
            }
            var tour = new Tour { Name = cameraName };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }
    }
}
