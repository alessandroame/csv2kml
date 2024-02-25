using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using System.ComponentModel;
using System.Diagnostics;
using static csv2kml.KmlExtensions;
using static DataExtensions;

namespace csv2kml
{
    public class SegmentBuilder
    {
        private Context _ctx;

        public SegmentBuilder(Context ctx)
        {
            _ctx = ctx;
            _ctx.Segments = BuildSegments();
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
                res.AddFeature(BuildTour(cameraSettings));
            }
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
                    Name = $"#{index} {segment.Type} {to.Altitude - from.Altitude}mt",
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
                Visibility =false,
                Open = false
            };
            AddStyles(res);
            var index = 0;
            foreach (var segment in _ctx.Segments.Where(s=> s.Type == FlightPhase.Climb ))
            {
                var segmentData = _ctx.Data.Skip(segment.From).Take(segment.To - segment.From);
                //short duration not considered a thermal 
                if (segmentData.Last().Time.Subtract(segmentData.First().Time).TotalSeconds < 15) continue;

                var from = _ctx.Data[segment.From];
                var to = _ctx.Data[segment.To];

                var vSpeed = segmentData.VerticalSpeed();
                var thermalType =     vSpeed.ToThermalType();
                var track = new Track
                {
                    AltitudeMode = SharpKml.Dom.AltitudeMode.Absolute,
                };
                var placemark = new Placemark
                {
                    Name = $"#{index} {thermalType} thermal gain {to.Altitude - from.Altitude}mt",
                    Geometry = track,
                    StyleUrl = new Uri($"#{thermalType}", UriKind.Relative),
                    Description = new Description
                    {
                        Text = $"\r\nfrom {from.Altitude}mt to {to.Altitude}mt" +
                                $"\r\nduration {to.Time.Subtract(from.Time)}" +
                                $"\r\navg vert. speed={Math.Round(vSpeed,2)}m/s",
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
        private Tour BuildTour(LookAtCameraConfig cameraConfig)
        {
            var tourplaylist = new Playlist();
            var data = _ctx.Data;
            var currentTime = data.First().Time.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
            var lastTime = data.Last().Time;
            var oldHeading = 0D;
            var previousTime= data.First().Time;
            var totSeconds = 0d;
            while (true)
            {
                var visibleData = new Data[0];
                while (visibleData.Length == 0)
                {
                    currentTime = currentTime.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
                    if (currentTime > lastTime) break;
                    visibleData = data.GetDataByTime(currentTime.AddSeconds(-cameraConfig.VisibleHistorySeconds), currentTime);
                }
                if (currentTime > lastTime) break;
                var currentData = visibleData.Last();

                var i = data.ToList().FindIndex(d => d.Time == currentData.Time);

                var segment = _ctx.Segments.FirstOrDefault(s => s.From <= i && s.To > i);
                if (segment == null) { break; }
                var segmentData = data.Skip(segment.From).Take(segment.To - segment.From);

                var duration = currentTime.Subtract(previousTime).TotalMilliseconds/1000;
                totSeconds += duration;
                
                previousTime = currentTime;
                var segmentBB = new BoundingBoxEx(segmentData);
                segmentData.First().ToVector().CalculateTiltPan(segmentData.Last().ToVector(),
                    out var segmentHeading, out var segmentTilt, out var segmentDistance, out var segmentGroundDistance);

                var visibleTimeFrom = visibleData.First().Time;
                if (segmentData.First().Time < visibleTimeFrom) visibleTimeFrom = segmentData.First().Time;

                if (segment.Type == FlightPhase.Climb || segment.Type == FlightPhase.MotorClimb)
                {
                    //var maxDegreePerSeconds = (double)180/ cameraConfig.UpdatePositionIntervalInSeconds;
                    var segmentPercentage = (double)(i - segment.From) / (segment.To - segment.From);
                    var segmentDurationInSeconds = data[segment.To].Time.Subtract(data[segment.From].Time).TotalSeconds;
                    var heading = 0d;
                    heading = 720 * segmentPercentage * segmentDurationInSeconds.Normalize(180);
                    var distance = segmentGroundDistance * 2 * segmentPercentage;

                    var cameraPos = currentData.ToVector().MoveTo(Math.Max(80, distance ), segmentHeading + heading);
                    cameraPos.Altitude = (segmentData.Min(d=>d.Altitude)+currentData.Altitude)/2+50;
                    var lookAt = currentData.ToVector();
                    var flyTo = new FlyTo
                    {
                        Id = (tourIndex++).ToString(),
                        Mode = FlyToMode.Smooth,
                        Duration = duration,
                        View = CameraHelper.CreateCamera(cameraPos, lookAt, visibleTimeFrom, currentData.Time, _ctx.AltitudeOffset, out heading)
                    };
                    tourplaylist.AddTourPrimitive(flyTo);
                    oldHeading = heading;
                }
                else
                {
                    var visibleDataBB = new BoundingBoxEx(visibleData);
                    currentData.ToVector().CalculateTiltPan(visibleDataBB.Center,
                        out var visibleDataHeading, out var visibleDataTilt, out var visibleDataDistance, out var visibleDataGroungDistance);

                    var heading = visibleDataHeading + 30;

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
                        Id = (tourIndex++).ToString(),
                        Mode = FlyToMode.Smooth,
                        Duration = duration,
                        View = CameraHelper.CreateLookAt(lookAt, Math.Max(140, visibleDataBB.DiagonalSize * 1.5), heading, 60,
                            visibleTimeFrom, currentData.Time, _ctx.AltitudeOffset)
                    };
                    tourplaylist.AddTourPrimitive(flyTo);

                    oldHeading = heading;
                    
                }
                currentTime = currentTime.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
            }
            var tour = new Tour { Name = "Tour by flight phase" };
            tour.Playlist = tourplaylist;
            return tour;
        }

        static int tourIndex = 0;

        private Segment[] BuildSegments()
        {
            var res = new List<Segment>();
            var index = 0;
            var minimumSegmentLengthInSeconds = 12;
            var data = _ctx.Data;
            while (index < data.Length)
            {
                var segment = new Segment
                {
                    From = index,
                    Type = data[index].FlightPhase
                };
                Console.WriteLine($"Start segment -> {segment.Type}");

                while (data[index].FlightPhase == segment.Type)
                {
                    index++;
                    if (index >= data.Length) break;
                    var nextData = data[index];

                    if (nextData.FlightPhase != segment.Type)
                    {
                        if (segment.Type == FlightPhase.MotorClimb) break;
                        var dTime = nextData.Time.Subtract(data[segment.From].Time).TotalSeconds;
                        if (dTime < minimumSegmentLengthInSeconds)
                        {
                            segment.Type = nextData.FlightPhase;
                            //Console.WriteLine($"too short: dt {dTime}s");
                        }
                        else{
                            //Console.WriteLine($"BREAK (dt {dTime}s)");
                            break;
                        }
                    }
                }
                if (index >= data.Length) break;
                segment.To = index;
                if (segment.Type == FlightPhase.MotorClimb)
                {
                    res.Add(segment);
                }
                else 
                { 
                    segment.Type = data.Skip(segment.From).Take(index - segment.From).VerticalSpeed().ToFlightPhase();
                    Console.WriteLine($"End segment -> {segment.Type}");
                    if (res.Count() > 1 && res.Last().Type == segment.Type)
                    {
                        var lastSegment = res.Last();
                        lastSegment.To = segment.To;
                        segment.Type = data.Skip(lastSegment.From).Take(lastSegment.To - lastSegment.From).VerticalSpeed().ToFlightPhase();
                        Console.WriteLine($"SEGMENT JOINT!!!!");
                    }
                    else
                    {
                        res.Add(segment);
                    }
                }
                Console.WriteLine($"{segment.Type} {segment.From}->{segment.To}");
                //Console.WriteLine($"{_data[segment.From].Altitude}->{_data[segment.To].Altitude} {segment.Type}");
            }
            return res.ToArray();
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