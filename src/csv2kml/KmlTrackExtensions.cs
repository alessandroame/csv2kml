using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml
{
    public static class KmlTrackExtensions
    {
        public static void BuildStyles(this Container container, string name, int subdivisions)
        {
            //Used to hide children
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
                var lineColor = $"33{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                var extrudeColor = $"55{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                Console.WriteLine($"{styleId} -> {lineColor}");
                container.AddStyle(new Style
                {
                    Id = $"extruded{styleId}",
                    Line = new LineStyle
                    {
                        Color = Color32.Parse(lineColor),
                        Width = 3
                    },
                    Icon = new IconStyle
                    {
                        Scale = 0
                    },
                    Polygon = new PolygonStyle
                    {
                        Color = Color32.Parse(extrudeColor)
                    }
                });
                lineColor = $"44{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                container.AddStyle(new Style
                {
                    Id = $"ground{styleId}",
                    Line = new LineStyle
                    {
                        Color = Color32.Parse(lineColor),
                        Width = 3
                    },
                    Icon = new IconStyle
                    {
                        Scale = 0
                    },
                    Polygon = new PolygonStyle
                    {
                        Color = Color32.Parse(extrudeColor)
                    }
                });
                lineColor = $"FF{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                container.AddStyle(new Style
                {
                    Id = styleId,
                    Line = new LineStyle
                    {
                        Color = Color32.Parse(lineColor),
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
                Description = new Description { Text = $"#{trackIndex++} -> {style}" }
            }; 
            placemark.Time = new SharpKml.Dom.TimeSpan
            {
                Begin = data.First().Time,
                End = data.Last().Time,
            };
            foreach (var d in data)
            {
                track.AddWhen(d.Time);
                track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude+altitudeOffset));
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
                StyleUrl = new Uri($"#extruded{style}", UriKind.Relative),
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
                lineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude+ altitudeOffset));
                groundLineString.Coordinates.Add(new Vector(d.Latitude, d.Longitude, d.Altitude + altitudeOffset));
            }
            return placemark;
        }

        public static void GenerateColoredTrack(this Container container, Data[] data, string name, int subdivision, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset,string styleRadix="")
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
                var nextItem = data[i + 1];
                //var delta = nextItem.Altitude - item.Altitude;
                var delta = item.VSpeed;
                coords.Add(item);
                var nv = delta.Normalize(3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv * subdivision / 2);

                if (oldNormalizedValue != normalizedValue)
                {
                    var p = CreatePlacemarkWithTrack(coords, $"{styleRadix}vspeed{oldNormalizedValue}", altitudeMode, altitudeOffset);

                    p.Viewpoint = coords.First().CreateLookAt(coords.Last(), true, altitudeMode, altitudeOffset);

                    folder.AddFeature(p);
                    coords = new List<Data> { item };
                    oldNormalizedValue = normalizedValue;
                }
            }
            coords.Add(data[data.Length - 1]);
            var lastPlacemark = CreatePlacemarkWithTrack(coords, $"{styleRadix}{name}{oldNormalizedValue}", altitudeMode, altitudeOffset);
            lastPlacemark.Viewpoint = coords.First().CreateLookAt(coords.Last(), true, altitudeMode, altitudeOffset);

            folder.AddFeature(lastPlacemark);
            Console.WriteLine($"point count: {data.Length}");
        }

        public static void GenerateLineString(this Container container, Data[] data, string name, int subdivision, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset)
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
                var nextItem = data[i + 1];
                //var delta = nextItem.Altitude - item.Altitude;
                var delta = item.VSpeed;
                coords.Add(item);
                var nv = delta.Normalize(3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv * subdivision / 2);

                if (oldNormalizedValue != normalizedValue)
                {
                    var p = CreatePlacemarkWithLineString(coords, $"vspeed{oldNormalizedValue}", altitudeMode, altitudeOffset);
                    folder.AddFeature(p);
                    coords = new List<Data> { item };
                    oldNormalizedValue = normalizedValue;
                }
            }
            coords.Add(data[data.Length - 1]);
            var lastPlacemark = CreatePlacemarkWithTrack(coords, $"{name}{oldNormalizedValue}", altitudeMode, altitudeOffset);
            folder.AddFeature(lastPlacemark);
            Console.WriteLine($"point count: {data.Length}");
        }

        public static FlyTo CreateFlyTo(AbstractView view)
        {
            //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
            var res = new FlyTo();
            res.Mode = FlyToMode.Bounce;
            res.Duration = 2;
            res.View = view;
            return res;
        }



        public static void GenerateCameraPath(this Container container, Data[] data,string cameraName, SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset, int frameBeforeStep = 10)
        {
            var tourplaylist = new Playlist();
            for (int i = frameBeforeStep; i < data.Length - 1; i += frameBeforeStep)
            {
                var from = data[i - frameBeforeStep];
                var to = data[i - frameBeforeStep + 1];
                var flyto = from.CreateCamera(to, altitudeMode,altitudeOffset);

                var camera = from.CreateCamera(to, altitudeMode, altitudeOffset);
                var flyTo = from.CreateFlyTo(to, camera, FlyToMode.Smooth);
                tourplaylist.AddTourPrimitive(flyTo);
            }
            var tour = new Tour { Name = cameraName };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }

        public static void GenerateLookPath(this Container container, 
            Data[] data,string cameraName,
            SharpKml.Dom.AltitudeMode altitudeMode, int altitudeOffset,
            int frameBeforeStep = 10, bool follow = false)
        {

            var tourplaylist = new Playlist();

            for (int i = 0; i < data.Length- frameBeforeStep; i += frameBeforeStep)
            {
                var from= data[i];
                var to = data[i + frameBeforeStep];
                var lookAt = from.CreateLookAt(to,follow, altitudeMode, altitudeOffset);
                var flyTo = from.CreateFlyTo(to, lookAt,FlyToMode.Smooth);
                tourplaylist.AddTourPrimitive(flyTo);
            }
            var tour = new Tour { Name = cameraName };
            tour.Playlist = tourplaylist;
            container.AddFeature(tour);
        }
    }
}
