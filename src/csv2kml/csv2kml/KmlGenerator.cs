using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using SharpKml.Engine;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace csv2kml
{
    public  class KmlGenerator
    {
        private Data[] _data;
        private Folder _rootFolder;

        public KmlGenerator(Data[] data,string rootName)
        {
            _data = data;
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

        public void GenerateColoredTrack(string name)
        {
            var folder = new Folder
            {
                Name = name,
                StyleUrl = new Uri("#hiddenChildren", UriKind.Relative)
            };
            var styles=new List<string>();
            _rootFolder.AddFeature(folder);
            BuildStyles("vspeed",20);
            var coords = new CoordinateCollection();
            var oldNormalizedValue = 0;
            for (var i = 0; i < _data.Length - 1; i++)
            {
                var item = _data[i];

                var v = new Vector(item.Latitude, item.Longitude, item.Altitude);

                var nextItem = _data[i + 1];
                //var delta = nextItem.Altitude - item.Altitude;
                var delta = item.VSpeed;
                coords.Add(v);
                var nv = NormalizeValue(delta, 3);
                //Console.WriteLine($"{delta}->{nv}");
                var normalizedValue = (int)Math.Round(nv*20);
                
                if (oldNormalizedValue != normalizedValue)
                {
                    styles.Add($"vspeed{oldNormalizedValue}");
                    var p = CreatePlacemark(coords, $"vspeed{oldNormalizedValue}");
                    folder.AddFeature(p);
                    coords = new CoordinateCollection();
                    coords.Add(v);
                    oldNormalizedValue = normalizedValue;
                }
            }
            var lastData = _data[_data.Length - 1];
            var lastVector = new Vector(lastData.Latitude, lastData.Longitude, lastData.Altitude);

            coords.Add(lastVector);
            var lastPlacemark = CreatePlacemark(coords, $"{name}{oldNormalizedValue}");
            folder.AddFeature(lastPlacemark);
            Console.WriteLine(string.Join("\r\n", 
                styles.GroupBy(s=>s).Select(g=>$"{g.Key}->{g.Count()}").ToArray()));
        }

        Placemark CreatePlacemark(CoordinateCollection coords, string style)
        {
            return new Placemark
            {
                Name = "Track",
                Geometry = new LineString
                {
                    AltitudeMode = SharpKml.Dom.AltitudeMode.RelativeToGround,
                    Coordinates = coords
                },
                StyleUrl = new Uri($"#{style}", UriKind.Relative)
            };
        }

        int GetColor(double normalizedValue)
        {
            return (int)Math.Min(255, Math.Round(255 * normalizedValue*2));
        }
        int GetInvColor(double normalizedValue)
        {
            return (int)Math.Min(255, Math.Round(255*2-255 * normalizedValue*2));
        }
        private void BuildStyles(string name, int subdivisions)
        {

            _rootFolder.AddStyle(new Style
            {
                Id = "hiddenChildren",
                List = new ListStyle
                {
                    ItemType=ListItemType.CheckHideChildren
                }
            });
            //min = red  =
            //0   = cyan = 
            //max = blue =
            for (var i = 1; i <= subdivisions ; i++)
            {
                var styleId = $"{name}-{i}";
                var k = (double)i / subdivisions;
                var c1 = (GetColor(k)).ToString("X2");
                var c2 = (GetInvColor(k)).ToString("X2");

                _rootFolder.AddStyle(new Style
                {
                    Id = styleId,
                    Line = new LineStyle
                    {
                        Color = Color32.Parse($"FF{c1}{c2}00"),
                        Width = 4
                    }
                });
                Console.WriteLine($"{name}-{i}->FF{c1}{c2}00");
            }
            for (var i = 0; i <= subdivisions; i++)
            {
                var styleId = $"{name}{i}";
                var k = (double)i / subdivisions;
                var c1 = (GetColor(k)).ToString("X2");
                var c2 = (GetInvColor(k)).ToString("X2");

                _rootFolder.AddStyle(new Style
                {
                    Id = styleId,
                    Line = new LineStyle
                    {
                        Color = Color32.Parse($"FF00{c2}{c1}"),
                        Width = 4
                    }
                });
                Console.WriteLine($"{name}{i}->FF00{c2}{c1}");
            }
        }

        public bool SaveTo(string fn,out string errors)
        {
            var outStream = new FileStream(fn, FileMode.Create);
            var res = false;
            errors = string.Empty;
            try
            {
                var kml = new Kml()
                {
                    Feature = _rootFolder
                };
                KmlFile.Create(kml, false).Save(outStream);
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
