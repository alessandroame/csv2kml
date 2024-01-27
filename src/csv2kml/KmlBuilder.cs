using Csv;
using Csv2KML;
using SharpKml.Base;
using SharpKml.Dom;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace csv2kml
{
    public class KmlBuilder
    {
        private TourConfig _tourConfig;
        private Data[] _data;
        private CsvConfig _csvConfig;
        int _subdivision = 20;
        Folder _rootFolder;
        public KmlBuilder UseCsvConfig(string configFilename)
        {
            _csvConfig = CsvConfig.FromFile(configFilename);
            return this;
        }

        public KmlBuilder UseTourConfig(string configFilename)
        {
            _tourConfig = TourConfig.FromFile(configFilename);
            return this;
        }
        public KmlBuilder Build(string csvFilename)
        {
            _data = LoadFromCsv(csvFilename);

            _rootFolder = new Folder
            {
                Name = $"{Path.GetFileNameWithoutExtension(csvFilename)}",
                Open = true
            };
            _rootFolder.AddFeature(BuildTrack());
            return this;
        }
        public Folder BuildTrack()
        {
            var trackFolder = new Folder
            {
                Name = "Track",
                Open = true
            };
            AddStyles(trackFolder);
            trackFolder.GenerateColoredTrack(_data, "3D track", _subdivision, _tourConfig.AltitudeMode, _tourConfig.AltitudeOffset,"Value");
            trackFolder.GenerateColoredTrack(_data, "Ground track", _subdivision, AltitudeMode.ClampToGround, _tourConfig.AltitudeOffset, "groundValue");
            trackFolder.GenerateLineString(_data, "extruded track", _subdivision, _tourConfig.AltitudeMode, _tourConfig.AltitudeOffset,"extrudedValue");
            foreach (var cameraSettings in _tourConfig.LookAtCameraSettings)
            {
                trackFolder.GenerateLookBackPath(_data, cameraSettings.Name, _tourConfig.AltitudeMode, _tourConfig.AltitudeOffset,
                        cameraSettings.UpdatePositionFrameInterval,
                        cameraSettings.LookAtBoundingBoxCenter,
                        cameraSettings.VisibleHistorySeconds,
                        cameraSettings.LookBackSeconds,
                        cameraSettings.RangeInMeters,
                        cameraSettings.Tilt,
                        cameraSettings.PanOffset, true);
            }
            return trackFolder;
        }

        public void Save(string fn)
        {
            try
            {
                var kml = new Kml();
                var document = new Document
                {
                    Open = true
                };
                document.AddFeature(_rootFolder);
                kml.Feature = document;
                //_kml.(document);
                var serializer = new Serializer();
                serializer.Serialize(kml);
                var outStream = new FileStream(fn, FileMode.Create);
                var sw = new StreamWriter(outStream);
                sw.Write(serializer.Xml);
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void AddStyles(Folder container)
        {
            container.AddStyle(new Style
            {
                Id = "hiddenChildren",
                List = new ListStyle
                {
                    ItemType = ListItemType.CheckHideChildren
                }
            });
            for (var i = 0; i <= _subdivision; i++)
            {
                var styleId = $"Value{i - _subdivision / 2}";
                var k = (float)i / _subdivision;
                var color = k.ToColor();
                var lineColor = $"33{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                var extrudeColor = $"55{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
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
                Console.WriteLine($"{styleId} -> {lineColor}");
            }
        }
        private Data[] LoadFromCsv(string csvFilename)
        {
            var res = new List<Data>();
            var fs = new FileStream(csvFilename, FileMode.Open);

            var lastTime = DateTime.MinValue;
            double lastLat = 0;
            double lastLon = 0;
            foreach (var line in CsvReader.ReadFromStream(fs, new CsvOptions { HeaderMode = HeaderMode.HeaderPresent }))
            {
                try
                {
                    if (!line[_csvConfig.LatitudeIndex].TryParseDouble(out var lat)) continue;
                    if (!line[_csvConfig.LongitudeIndex].TryParseDouble(out var lon)) continue;
                    if (!line[_csvConfig.AltitudeIndex].TryParseDouble(out var alt)) continue;
                    if (!line[_csvConfig.ValueToColorizeIndex].TryParseDouble(out var valueToColorize)) continue;
                    var timestamp = DateTime.Parse(line[_csvConfig.TimestampIndex]);
                    if (timestamp.Subtract(lastTime).TotalSeconds<1) continue;
                    //import
                    var data = new Data(timestamp, lat, lon, alt, valueToColorize);
                    res.Add(data);
                    lastTime = timestamp;
                    lastLat = lat;
                    lastLon = lon;
                    //if (res.Count > 100) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            //Interpolate(res);
            return res.ToArray();
        }

        //private void Interpolate(List<Data> data)
        //{
        //    InterpolatField(data, (d) => d.Time.Ticks,
        //        (data, delta) => { data.Time = data.Time.AddTicks((long)delta); });
        //    InterpolatField(data, (d) => d.Altitude,
        //        (data, delta) => { data.Altitude = data.Altitude + delta; });
        //    InterpolatField(data, (d) => d.Latitude,
        //        (data, delta) => { data.Latitude = data.Latitude + delta; });
        //    InterpolatField(data, (d) => d.Longitude,
        //           (data, delta) => { data.Longitude = data.Longitude + delta; });
        //}

        //private void InterpolatField(List<Data> data, Func<Data, double> valueGetter, Action<Data, double> valueSetter) 
        //{
        //    var segment = new List<Data>();
        //    var lastData = data[0];
        //    foreach (var d in data)
        //    {
        //        if (valueGetter(lastData)==(valueGetter(d)))
        //        {
        //            segment.Add(d);
        //        }
        //        else
        //        {
        //            if (segment.Count() > 1)
        //            {
        //                var v = segment.Count() + 1;
        //                var delta = (valueGetter(d)-valueGetter(lastData)) / v;
        //                var zz0 = segment.Select(s => $"{s.Time.Ticks}-{s.Altitude}").ToArray();
        //                for (var i = 1; i < segment.Count(); i++)
        //                {
        //                    var s = segment[i];
        //                    valueSetter(s,delta * i);
        //                }
        //                var zz1 = segment.Select(s => $"{s.Time.Ticks}-{s.Altitude}").ToArray();
        //            }
        //            lastData = d;
        //            segment = new List<Data>() { d };
        //        }
        //    }
        //}



    }
}