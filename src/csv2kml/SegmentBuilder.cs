using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using static csv2kml.KmlExtensions;
using static DataExtensions;

namespace csv2kml
{
    public class SegmentBuilder
    {
        private Context _ctx;

        public SegmentBuilder UseCtx(Context ctx)
        {
            _ctx = ctx;
            _ctx.Segments = BuildSegments();
            return this;
        }
        public Feature Build()
        {
            var res = new Folder
            {
                Name = "Analysis",
                Open = true
            };
            var segmentsFolder = BuildTrack();
            res.AddFeature(segmentsFolder);
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
                Open = false
            };
            AddSegmentStyles(res);
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
        private Tour BuildTour(LookAtCameraConfig cameraConfig)
        {
            var tourplaylist = new Playlist();
            var data = _ctx.Data;
            var currentTime = data.First().Time.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
            var oldHeading = 0D;

            while (true)
            {
                var visibleData = data.GetDataByTime(currentTime.AddSeconds(-cameraConfig.VisibleHistorySeconds), currentTime);
                if (visibleData.Count() == 0) break;

                var currentData = visibleData.Last();
                var duration = data.GetDurationInSeconds(currentData.Time, currentData.Time.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds));

                var i = data.ToList().FindIndex(d => d.Time == currentData.Time);

                var segment = _ctx.Segments.FirstOrDefault(s => s.From <= i && s.To > i);
                if (segment == null) { break; }
                var segmentData = data.Skip(segment.From).Take(segment.To - segment.From);
                var segmentBB = new BoundingBoxEx(segmentData);
                segmentData.First().ToVector().CalculateTiltPan(segmentData.Last().ToVector(),
                    out var segmentHeading, out var segmentTilt, out var segmentDistance, out var segmentGroungDistance);

                if (segment.Type == FlightPhase.Climb || segment.Type == FlightPhase.MotorClimb)
                {
                    //var maxDegreePerSeconds = (double)180/ cameraConfig.UpdatePositionIntervalInSeconds;
                    var segmentPercentage = (double)(i - segment.From) / (segment.To - segment.From);
                    var segmentDurationInSeconds = data[segment.To].Time.Subtract(data[segment.From].Time).TotalSeconds;
                    var heading = 0d;
                    heading = 720 * segmentPercentage * segmentDurationInSeconds.Normalize(180);
                    var cameraPos = segmentBB.Center.MoveTo(Math.Max(180, segmentGroungDistance * 2), segmentHeading + heading);
                    cameraPos.Altitude += 50;
                    var lookAt = currentData.ToVector();
                    var visibleTimeFrom = visibleData.First().Time;
                    if (segmentData.First().Time < visibleTimeFrom) visibleTimeFrom = segmentData.First().Time;
                    var flyTo = new FlyTo
                    {
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
                        Mode = FlyToMode.Smooth,
                        Duration = duration,
                        View = CameraHelper.CreateLookAt(lookAt, Math.Max(140, visibleDataBB.DiagonalSize * 1.5), heading, 60,
                            visibleData.First().Time, currentData.Time, _ctx.AltitudeOffset)
                    };
                    tourplaylist.AddTourPrimitive(flyTo);
                    oldHeading = heading;
                }
                currentTime = currentTime.AddSeconds(cameraConfig.UpdatePositionIntervalInSeconds);
            }
            var tour = new Tour { Name = "Track Tour by flight phase" };
            tour.Playlist = tourplaylist;
            return tour;
        }
        private Segment[] BuildSegments()
        {
            var res = new List<Segment>();
            var index = 1;
            var minimumSegmentLengthInSeconds = 20;
            var data = _ctx.Data;
            var lastPhase = data[0].FlightPhase;
            while (index < data.Length)
            {
                var segment = new Segment
                {
                    From = index - 1,
                    Type = data[index].FlightPhase
                };

                while (index < data.Length)
                {
                    if (data[index].FlightPhase != lastPhase)
                    {
                        if (segment.Type != FlightPhase.MotorClimb
                            && data[index].Time.Subtract(data[segment.From].Time).TotalSeconds < minimumSegmentLengthInSeconds)
                        {
                            //handling segment too short
                            segment.Type = data[Math.Min(data.Length - 1, index)].FlightPhase;
                            lastPhase = segment.Type;
                        }
                        else
                        {
                            break;
                        }
                    }
                    index++;
                }

                segment.To = Math.Min(data.Length - 1, index - 1);
                if (res.Count() > 1 && res.Last().Type == segment.Type)
                {
                    res.Last().To = segment.To;
                    Console.WriteLine($"Joint to last segment {segment.To}");
                }
                else
                {
                    res.Add(segment);
                    Console.WriteLine($"{segment.Type} {segment.From}->{segment.To}");
                }
                //Console.WriteLine($"{_data[segment.From].Altitude}->{_data[segment.To].Altitude} {segment.Type}");
                if (index >= data.Length) break;
                lastPhase = data[index].FlightPhase;
            }
            return res.ToArray();
        }
        private static void AddSegmentStyles(Folder container)
        {
            var colors = new Dictionary<FlightPhase, string>() {
                { FlightPhase.MotorClimb,"FF000000" },
                { FlightPhase.Climb,"FF0000FF" },
                { FlightPhase.Glide,"FF00FF00" },
                { FlightPhase.Sink,"FFFFFF00" },
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
        }
    }
}