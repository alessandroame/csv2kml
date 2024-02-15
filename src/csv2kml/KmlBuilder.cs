using Csv;
using Csv2KML;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Xml.Linq;

namespace csv2kml
{
    public partial class KmlBuilder
    {
        private TourConfig _tourConfig;
        private Data[] _data;
        private CsvConfig _csvConfig;
        int _subdivision = 120;
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
            _data.CalculateFlightPhase();
            _rootFolder = new Folder
            {
                Name = $"{Path.GetFileNameWithoutExtension(csvFilename)}",
                Open = true
            };
            _rootFolder.AddFeature(BuildTrack());
            _rootFolder.AddFeature(BuildSegments());
            return this;
        }
        private void AddSegmentStyles(Folder container)
        {
            var colors = new Dictionary<FlightPhase, string>() {
                { FlightPhase.MotorClimb,"FF000000" },
                { FlightPhase.Climb,"FF0000FF" },
                { FlightPhase.Glide,"FF00FF00" },
                { FlightPhase.Sink,"FFFFFF00" },
                };
            foreach(var kv in colors)
            container.AddStyle(new Style
            {
                Id = $"segment{kv.Key}",
                Icon = new IconStyle
                {
                    Scale = 0
                },
                Line = new LineStyle
                {
                    Color = Color32.Parse(kv.Value),
                    Width = 2
                },
            });
            
        }
        public Folder BuildSegments()
        {
            var res = new Folder
            {
                Name = "Analysis",
                Open = true
            };
            var segmentsFolder= new Folder
            {
                Name = "Segments",
                Open = false
            };
            res.AddFeature(segmentsFolder);
            AddSegmentStyles(segmentsFolder);
            var segments = new List<Segment>();
            var index =1;
            var minimumSegmentLengthInSeconds = 20;
            var lastPhase = _data[0].FlightPhase;
            while (index < _data.Length)
            {
                var segment = new Segment
                {
                    From = index-1,
                    Type = _data[index].FlightPhase
                };

                while (index < _data.Length)
                {
                    if (_data[index].FlightPhase != lastPhase)
                    {
                        if (segment.Type != FlightPhase.MotorClimb 
                            && _data[index].Time.Subtract(_data[segment.From].Time).TotalSeconds < minimumSegmentLengthInSeconds)
                        {
                            //handling segment too short
                            segment.Type = _data[Math.Min(_data.Length - 1, index)].FlightPhase;
                            lastPhase = segment.Type;
                        }
                        else
                        {
                            break;
                        }
                    }
                    index++;
                } 

                segment.To = Math.Min(_data.Length - 1, index - 1);
                if (segments.Count()>1 && segments.Last().Type == segment.Type)
                {
                    segments.Last().To=segment.To;
                    Console.WriteLine($"Joint to last segment {segment.To}");
                }
                else
                {
                    segments.Add(segment);
                    Console.WriteLine($"{segment.Type} {segment.From}->{segment.To}");
                }
                //Console.WriteLine($"{_data[segment.From].Altitude}->{_data[segment.To].Altitude} {segment.Type}");
                if (index >= _data.Length) break;
                lastPhase = _data[index].FlightPhase;
            }
            index = 0;
            foreach (var segment in segments)
            {
                var track = new Track
                {
                    AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute,
                };
                var from = _data[segment.From];
                var to = _data[segment.To];
                var placemark = new Placemark
                {
                    Name = $"#{index} {segment.Type} {to.Altitude-from.Altitude}mt",
                    Geometry = track,
                    StyleUrl = new Uri($"#segment{segment.Type}", UriKind.Relative),
                    Description = new Description
                    {
                        Text = $"#{segment.Type} from {from.Altitude}mt to {to.Altitude}mt in {to.Time.Subtract(from.Time)}"
                    }
                };
                placemark.Time = new SharpKml.Dom.TimeSpan
                {
                    Begin = from.Time,
                    End = to.Time,
                };
                foreach (var d in new List<Data> { from, to })
                {
                    track.AddWhen(d.Time);
                    track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude + _tourConfig.AltitudeOffset));
                }
                segmentsFolder.AddFeature(placemark);
                index++;
            }
            foreach (var cameraSettings in _tourConfig.LookAtCameraSettings)
            {
                res.GenerateTrackTour(_data.ToList(), segments, _tourConfig, cameraSettings);
            }
            return res;
        }

        public Folder BuildTrack()
        {
            var trackFolder = new Folder
            {
                Name = "Track",
                Open = true
            };
            AddTrackStyles(trackFolder);
            trackFolder.GenerateColoredTrack(_data, "3D track", _subdivision, _tourConfig.AltitudeMode, _tourConfig.AltitudeOffset, "Value");
            trackFolder.GenerateColoredTrack(_data, "Ground track", _subdivision, SharpKml.Dom.AltitudeMode.ClampToGround, _tourConfig.AltitudeOffset, "groundValue");
            trackFolder.GenerateLineString(_data, "extruded track", _subdivision, _tourConfig.AltitudeMode, _tourConfig.AltitudeOffset, "extrudedValue");
            foreach (var cameraSettings in _tourConfig.LookAtCameraSettings)
            {
                trackFolder.GenerateLookBackPath(_data, _tourConfig, cameraSettings, true);
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

        private void AddTrackStyle(Folder container,string name,string lineColor,string polygonColor,int width)
        {
            container.AddStyle(new Style
            {
                Id = name,
                Line = new LineStyle
                {
                    Color = Color32.Parse(lineColor),
                    Width = width
                },
                Icon = new IconStyle
                {
                    Scale = 0
                },
                Polygon = new PolygonStyle
                {
                    Color = Color32.Parse(polygonColor),

                }
            });
        }
        private void AddTrackStyles(Folder container)
        {
            container.AddStyle(new Style
            {
                Id = "hiddenChildren",
                List = new ListStyle
                {
                    ItemType = ListItemType.CheckHideChildren
                }
            });
            var trackWidth = 4;
            AddTrackStyle(container, $"extrudedValueMotor", $"33000000", $"00000000", trackWidth);
            AddTrackStyle(container, $"groundValueMotor", $"88000000", $"55000000",trackWidth);
            AddTrackStyle(container, $"ValueMotor", $"FF000000", $"55000000", trackWidth);

            for (var i = 0; i <= _subdivision; i++)
            {
                var value = i - _subdivision / 2;
                var styleId = $"Value{value}";
                var normalizedValue = (double)value / _subdivision * 2;
                var logValue = normalizedValue.ApplyExpo(_csvConfig.ColorScaleExpo);
                var color = logValue.ToColor();
                //Console.WriteLine($"{i}-> {value} -> {normalizedValue} -> {logValue} -> {color}");

                var polygonColor = $"00{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                var lineColor = $"22{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, $"extruded{styleId}", lineColor, polygonColor, trackWidth/2);
                
                lineColor = $"88{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, $"ground{styleId}", lineColor, polygonColor, trackWidth);
                
                lineColor = $"FF{color.B.ToString("X2")}{color.G.ToString("X2")}{color.R.ToString("X2")}";
                AddTrackStyle(container, styleId, lineColor, polygonColor, trackWidth);
                //Console.WriteLine($"{styleId} -> {lineColor}");
            }
        }
        private Data[] LoadFromCsv(string csvFilename)
        {
            var res = new List<Data>();
            var fs = new FileStream(csvFilename, FileMode.Open);

            var lastTime = DateTime.MinValue;
            double lastLat = 0;
            double lastLon = 0;
            double? lastAlt=null;


            Dictionary<string, int> fieldByName =null;
            string getLineValue(ICsvLine line,string fieldTitle)
            {
                return line[fieldByName[fieldTitle.Trim()]];
            }
            foreach (var line in CsvReader.ReadFromStream(fs, new CsvOptions { HeaderMode = HeaderMode.HeaderPresent }))
            {
                if (fieldByName== null)
                {
                    fieldByName = new Dictionary<string, int>();
                    var headers = line.Headers;
                    for (var i = 0; i < headers.Length; i++)
                    {
                        try
                        {
                            fieldByName.Add(headers[i].Trim(), i);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
                try
                {
                    if (!getLineValue(line,_csvConfig.FieldsByTitle.Latitude).TryParseDouble(out var lat)) continue;
                    if (!getLineValue(line,_csvConfig.FieldsByTitle.Longitude).TryParseDouble(out var lon)) continue;
                    if (!getLineValue(line,_csvConfig.FieldsByTitle.Altitude).TryParseDouble(out var alt)) continue;
                    if (!getLineValue(line, _csvConfig.FieldsByTitle.VerticalSpeed).TryParseDouble(out var verticalSpeed)) continue;
                    if (!getLineValue(line, _csvConfig.FieldsByTitle.Motor).TryParseDouble(out var motor)) continue;
                    if (!getLineValue(line, _csvConfig.FieldsByTitle.Speed).TryParseDouble(out var speed)) continue;
                    var timestamp = DateTime.Parse(getLineValue(line,_csvConfig.FieldsByTitle.Timestamp));
                    //if (timestamp.Subtract(lastTime).TotalSeconds<1) continue;
                    if (lastTime == timestamp || lastLat == lat && lastLon == lon) continue;
                    //Console.WriteLine($"{timestamp} {motor}");
                    //import
                    //var data = new Data(timestamp, lat, lon, alt, lastAlt, speed, motor == 1);
                    var data = new Data(timestamp, lat, lon, alt, verticalSpeed, speed, motor == 1);

                    res.Add(data);
                    lastTime = timestamp;
                    lastLat = lat;
                    lastLon = lon;
                    lastAlt = alt;
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