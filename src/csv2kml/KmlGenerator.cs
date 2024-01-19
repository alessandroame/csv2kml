using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace csv2kml
{
    public  class KmlGenerator
    {
        private Data[] _data;
        private Document _kml;
        private Folder _rootFolder;

        public KmlGenerator(Data[] data,string rootName)
        {
            _data = data;
            _kml = new Document();
            Tour tour = new Tour();
            tour.Name = "First Person View";
            //AddNamespace(kml, "gx", "http://www.google.com/kml/ext/2.2");
            Playlist tourplaylist = new Playlist();
            _rootFolder = new Folder
            {
                Name = rootName
            };
        }

        private double NormalizeValue(double value,double max)
        {
            var res = value/ max;
            if (res > 1) res = 1;
            else if (res<-1) res = -1;
            return res;
        }

        private Bitmap GenerateLegend(double k,int subdivisions)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            var w = 200;
            var h = 400;
            var bitmap = new Bitmap(w,h);
            Graphics graphics = Graphics.FromImage(bitmap);

            for (var i = 0; i <= subdivisions; i++)
            {
                var color = ValueGetColor((float)i / subdivisions);
                var value = i / subdivisions * k;

                var brush = new SolidBrush(color);
                graphics.FillRectangle(
                    brush,
                    new Rectangle {
                        X=0,
                        Y=h-h/subdivisions*i,
                        Width=w/2, 
                        Height=h/subdivisions
                    });
                bitmap.Save("legend.bmp");
#pragma warning restore CA1416 // Validate platform compatibility
            }
            return bitmap;
        }
        internal void GenerateCameraPath(string cameraName, int frameBeforeStep = 10)
        {
            //https://csharp.hotexamples.com/examples/SharpKml.Dom/Description/-/php-description-class-examples.html?utm_content=cmp-true
            var flyTo =new FlyTo();
            var lookAt = new LookAt();
        }
        public void GenerateColoredTrack(string name,int subdivision)
        {
            var folder = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            _rootFolder.AddFeature(folder);
            var min = _data.Min(d => d.VSpeed);
            var max = _data.Max(d => d.VSpeed);
            GenerateLegend(3, subdivision);
            BuildStyles("vspeed",subdivision);
            var coords = new List<Data>();
            var oldNormalizedValue = 0;
            for (var i = 0; i < _data.Length - 1; i++)
            {
                var item = _data[i];
                var v = new Vector(item.Latitude, item.Longitude, item.Altitude);
                var nextItem = _data[i + 1];
                //var delta = nextItem.Altitude - item.Altitude;
                var delta = item.VSpeed;
                coords.Add(item);
                var nv = NormalizeValue(delta, 3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv* subdivision/2);
                
                if (oldNormalizedValue != normalizedValue)
                {
                    var p = CreatePlacemark(coords, $"vspeed{oldNormalizedValue}");
                    folder.AddFeature(p);
                    coords = new List<Data>{item};
                    oldNormalizedValue = normalizedValue;
                }
            }
            coords.Add(_data[_data.Length - 1]);
            var lastPlacemark = CreatePlacemark(coords, $"{name}{oldNormalizedValue}");
            folder.AddFeature(lastPlacemark);
            Console.WriteLine($"point count: {_data.Length}");
        }
        int trackIndex = 0;
        Placemark CreatePlacemark(List<Data> data, string style)
        {
            //if (data.Count() <= 2) Debugger.Break();
            var track = new Track
            {
                AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround,
            };
            var placemark = new Placemark
            {
                Name = "",// Math.Round(data.Average(d => d.VSpeed), 2).ToString(),
                Geometry = track,
                StyleUrl = new Uri($"#{style}", UriKind.Relative),
                Description = new Description { Text =$"#{trackIndex++} -> {style}" }
            };
            foreach (var d in data) {
                track.AddWhen(d.Time);
                track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude));
            }
            return placemark;
            /*var coords = new CoordinateCollection(data.Select(d => new Vector(d.Latitude, d.Longitude, d.Altitude)));
            return new Placemark
            {
                Name = "Track",
                Geometry = new LineString
                {
                    AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround,
                    Coordinates = coords
                },
                StyleUrl = new Uri($"#{style}", UriKind.Relative)
            };*/
        }

        private Color ValueGetColor(float value)
        {
            float hue = (1-value) * 180;
            if (hue < 0) hue += 360;
            if (hue > 360) hue -= 360;
            float red, green, blue;

            if (hue < 60)
            {
                red = 1;
                green = hue / 60f;
                blue = 0;
            }
            else if (hue < 120)
            {
                red = 1 - (hue - 60) / 60f;
                green = 1;
                blue = 0;
            }
            else if (hue < 180)
            {
                red = 0;
                green = 1;
                blue = (hue - 120) / 60f;
            }
            else if (hue < 240)
            {
                red = 0;
                green = 1 - (hue - 180) / 60f;
                blue = 1;
            }
            else if (hue < 300)
            {
                red = (hue - 240) / 60f;
                green = 0;
                blue = 1;
            }
            else
            {
                red = 1;
                green = 0;
                blue = 1 - (hue - 300) / 60f;
            }

            return Color.FromArgb(255, (int)(red * 255), (int)(green * 255), (int)(blue * 255));
        }

        private void BuildStyles(string name,int subdivisions)
        {
            _rootFolder.AddStyle(new Style
            {
                Id = "hiddenChildren",
                List = new ListStyle
                {
                    ItemType=ListItemType.CheckHideChildren
                }
            });
            for (var i = 0; i <= subdivisions; i++)
            {
                var styleId = $"{name}{i-subdivisions/2}";
                var k = (float)i / subdivisions;
                var color = ValueGetColor(k);
                var c = $"FF{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                Console.WriteLine($"{styleId} -> {c}");
                _rootFolder.AddStyle(new Style
                {
                    Id = styleId,
                    Line = new LineStyle
                    {
                        Color = Color32.Parse(c),
                        Width= 3
                    },
                    Icon = new IconStyle
                    {
                        Scale = 0
                    }
                }) ;
            }
        }

        public bool SaveTo(string fn,out string errors)
        {
            var res = false;
            errors = string.Empty;
            try
            {
                _kml.AddFeature(_rootFolder);
                var serializer = new Serializer();
                serializer.Serialize(_kml);
                var outStream = new FileStream(fn, FileMode.Create);
                var sw = new StreamWriter(outStream);
                sw.Write(serializer.Xml);
                sw.Close();
                res = true;
            }
            catch (Exception ex)
            {
                errors= ex.ToString();
            }
            return res;
        }
    }
}
