using csv2kml.CameraDirection;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;

namespace csv2kml
{
    public class SegmentBuilder
    {
        private Context _ctx;

        public SegmentBuilder(Context ctx)
        {
            _ctx = ctx;
            _ctx.Segments = CalculateSegments();
        }
        public Feature Build()
        {
            var res = new Folder
            {
                Name = "Analysis",
                Open = true,
            };
            var dataFolder = new Folder
            {
                Name = "Data",
                Open = true,
            };
            dataFolder.AddStyle(new Style
            {
                List = new ListStyle
                {
                    ItemType = ListItemType.RadioFolder
                }
            });
            res.AddFeature(dataFolder);
            var segmentsFolder = BuildTrack();
            dataFolder.AddFeature(segmentsFolder);
            var thermalsFolder = BuildThermalsTrack();
            dataFolder.AddFeature(thermalsFolder);

            foreach (var cameraSettings in _ctx.TourConfig.LookAtCameraSettings)
            {
                res.AddFeature(new TourByFlightPhase(_ctx, cameraSettings).Build());
            }
            res.AddFeature(new OverviewTourBuilder(_ctx).Build());
            return res;
        }
        private Feature BuildTrack()
        {
            var res = new Folder
            {
                Name = "Segments",
                Open = false,
                Visibility = false
            };
            AddStyles(res);
            var index = 0;
            foreach (var segment in _ctx.Segments)
            {
                var track = new Track
                {
                    AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute,
                };
                var from = _ctx.Data[segment.From];
                var to = _ctx.Data[segment.To];

                var placemark = new Placemark
                {
                    Name = $"#{index} {segment.FlightPhase} {to.Altitude - from.Altitude}mt",
                    Geometry = track,
                    StyleUrl = new Uri($"#segment{segment.FlightPhase}", UriKind.Relative),
                    Description = new Description
                    {
                        Text = $"#{segment.FlightPhase} from {from.Altitude}mt to {to.Altitude}mt in {to.Time.Subtract(from.Time)}"
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
                    track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude + _ctx.AltitudeOffset));
                }
                res.AddFeature(placemark);
                index++;
            }
            return res;
        }
        private Feature BuildThermalsTrack()
        {
            var res = new Folder
            {
                Name = "Thermals",
                Visibility = false,
                Open = false
            };
            AddStyles(res);
            var index = 0;
            foreach (var segment in _ctx.Segments.Where(s => s.FlightPhase == FlightPhase.Climb))
            {
                var segmentData = _ctx.Data.Skip(segment.From).Take(segment.To - segment.From);
                //short duration not considered a thermal 
                if (segmentData.Last().Time.Subtract(segmentData.First().Time).TotalSeconds < 15) continue;

                var from = _ctx.Data[segment.From];
                var to = _ctx.Data[segment.To];

                var vSpeed = segmentData.VerticalSpeed();
                var thermalType = vSpeed.ToThermalType();

                var thermalDescription = thermalType == ThermalType.Normal ? "Thermal" : $"{thermalType.ToString()} thermal";
                var track = new Track
                {
                    AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute,
                };
                var placemark = new Placemark
                {
                    Name = $"#{index} {thermalDescription} gain {to.Altitude - from.Altitude}mt",
                    Geometry = track,
                    StyleUrl = new Uri($"#{thermalType}", UriKind.Relative),
                    Description = new Description
                    {
                        Text = $"\r\nfrom {from.Altitude}mt to {to.Altitude}mt" +
                                $"\r\nduration {to.Time.Subtract(from.Time)}" +
                                $"\r\navg vert speed={Math.Round(vSpeed, 2)}m/s",
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
                    track.AddCoordinate(new Vector(d.Latitude, d.Longitude, d.Altitude + _ctx.AltitudeOffset));
                }
                res.AddFeature(placemark);
                index++;
            }
            return res;
        }

        static int tourIndex = 0;

        private Segment[] CalculateSegments()
        {
            var res = new List<Segment>();
            var index = 0;
            var data = _ctx.Data;
            while (index < data.Length - 1)
            {
                var segment = GetNextSegment(data, index);
                if (segment == null) break;
                index = segment.To;
                if (segment.To >= data.Length) segment.To = data.Length - 1;
                if (res.Count() > 0 && res.Last().FlightPhase == segment.FlightPhase)
                {
                    res.Last().To = segment.To;
                    Console.WriteLine($"{new string('.', segment.FlightPhase.ToString().Length)}.{segment.From}->{segment.To} JOIN");
                }
                else
                {
                    res.Add(segment);
                    Console.WriteLine($"{segment.FlightPhase} {segment.From}->{segment.To}");
                }
            }
            var sIndex = 0;
            var tIndex = 0;
            foreach (var segment in res)
            {
                segment.SegmentIndex = sIndex++;

                if (segment.FlightPhase != FlightPhase.Climb) continue;

                //thermals recognition
                var segmentData = _ctx.Data.Skip(segment.From).Take(segment.To - segment.From);
                //short duration not considered a thermal 
                if (segmentData.Last().Time.Subtract(segmentData.First().Time).TotalSeconds < 15) continue;
                //is a thermal
                segment.ThermalIndex = tIndex++;
                var vSpeed = segmentData.VerticalSpeed();
                segment.ThermalType = vSpeed.ToThermalType();
            }
            return res.ToArray();
        }

        private Segment GetNextSegment(Data[] data, int from)
        {
            var res = new Segment
            {
                From = from
            };
            var maxIndex = res.From;

            var index = from;
            if (data[from].MotorActive)
            {
                res.FlightPhase = FlightPhase.MotorClimb;
                var firstNonMotorData = data.Skip(from).FirstOrDefault(d => !d.MotorActive);
                if (firstNonMotorData == null)
                    res.To = data.Last().Index;
                else
                {
                    res.To = firstNonMotorData.Index;
                }
            }
            else
            {
                var minimumSegmentLengthInSeconds = 16;
                var startTime = data[from].Time;
                var firstMotorData = data.Skip(from).FirstOrDefault(d => d.MotorActive);
                if (firstMotorData == null)
                    maxIndex = data.Last().Index;
                else
                {
                    maxIndex = firstMotorData.Index;
                }
                res.To = maxIndex;
                //look ahead to calculate segment flight phase
                var buffer = data.GetAroundBySeconds(from, res.From, maxIndex, 0, minimumSegmentLengthInSeconds);
                res.FlightPhase = buffer.VerticalSpeed().ToFlightPhase();
                for (int i = buffer.Last().Index; i <= maxIndex; i++)
                {
                    buffer = data.GetAroundBySeconds(i, res.From, maxIndex, minimumSegmentLengthInSeconds, 0);
                    //check when phase changed
                    var currentPhase = buffer.VerticalSpeed().ToFlightPhase();
                    if (currentPhase != res.FlightPhase)
                    {
                        //if(currentPhase==FlightPhase.Climb) Debugger.Break();
                        break;
                    }
                    res.To = i + 1;
                }
                var segmentData = data.Clip(res.From, res.To);
                if (segmentData.Count() > 1)
                    res.FlightPhase = segmentData.VerticalSpeed().ToFlightPhase();
                else
                    res.FlightPhase = data.First().VerticalSpeed.ToFlightPhase();
            }
            return res;
        }
        private static void AddStyles(Folder container)
        {
            var colors = new Dictionary<FlightPhase, string>() {
                { FlightPhase.MotorClimb,"FF000000" },
                { FlightPhase.Climb,"FF0000FF" },
                { FlightPhase.Glide,"FF00FF00" },
                { FlightPhase.Sink,"FFFFFF00" }
                };
            foreach (var kv in colors)
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

            container.AddStyle(new Style
            {
                Id = $"{ThermalType.Weak}",
                Icon = new IconStyle
                {
                    Scale = 0
                },
                Line = new LineStyle
                {
                    Color = Color32.Parse("FF00FF00"),
                    Width = 6
                },
            });
            container.AddStyle(new Style
            {
                Id = $"{ThermalType.Normal}",
                Icon = new IconStyle
                {
                    Scale = 0
                },
                Line = new LineStyle
                {
                    Color = Color32.Parse("FF00FFFF"),
                    Width = 10
                },
            });
            container.AddStyle(new Style
            {
                Id = $"{ThermalType.Strong}",
                Icon = new IconStyle
                {
                    Scale = 0
                },
                Line = new LineStyle
                {
                    Color = Color32.Parse("FF0000FF"),
                    Width = 14
                },
            });

        }
    }
}