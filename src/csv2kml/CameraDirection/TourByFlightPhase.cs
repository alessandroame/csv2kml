using SharpKml.Dom.GX;
using System.Diagnostics;
using static DataExtensions;

namespace csv2kml.CameraDirection
{
    public class TourByFlightPhase : TourBuilder
    {
        private LookAtCameraConfig _cameraConfig;

        public TourByFlightPhase(Context context, LookAtCameraConfig cameraConfig) : base(context)
        {
            _cameraConfig = cameraConfig;
        }

        public override Tour Build()
        {
            var tourplaylist = new Playlist();
            var data = _ctx.Data;
            var currentTime = data.First().Time.AddSeconds(_cameraConfig.UpdatePositionIntervalInSeconds);
            var lastTime = data.Last().Time;
            var oldHeading = 0D;
            var previousTime = data.First().Time;
            var totSeconds = 0d;
            int tourIndex = 0;
            while (true)
            {
                var visibleData = new Data[0];
                while (visibleData.Length == 0)
                {
                    currentTime = currentTime.AddSeconds(_cameraConfig.UpdatePositionIntervalInSeconds);
                    if (currentTime > lastTime) break;
                    var from = currentTime.AddSeconds(-_cameraConfig.VisibleHistorySeconds);
                    visibleData = data.GetDataByTime(from, currentTime);
                }
                if (currentTime > lastTime) break;
                var currentData = visibleData.Last();

                var i = data.ToList().FindIndex(d => d.Time == currentData.Time);

                var segment = _ctx.Segments.FirstOrDefault(s => s.From <= i && s.To > i);
                if (segment == null) { break; }
                var segmentData = data.Skip(segment.From).Take(segment.To - segment.From);

                var duration = currentTime.Subtract(previousTime).TotalMilliseconds / 1000;
                totSeconds += duration;

                previousTime = currentTime;
                var segmentBB = new BoundingBoxEx(segmentData);
                segmentData.First().ToVector().CalculateTiltPan(segmentData.Last().ToVector(),
                    out var segmentHeading, out var segmentTilt, out var segmentDistance, out var segmentGroundDistance);

                var visibleTimeFrom = visibleData.First().Time;
                if (segmentData.First().Time < visibleTimeFrom) visibleTimeFrom = segmentData.First().Time;

                if (segment.FlightPhase == FlightPhase.Climb || segment.FlightPhase == FlightPhase.MotorClimb)
                {
                    //var maxDegreePerSeconds = (double)180/ cameraConfig.UpdatePositionIntervalInSeconds;
                    var segmentPercentage = (double)(i - segment.From) / (segment.To - segment.From);
                    var segmentDurationInSeconds = data[segment.To].Time.Subtract(data[segment.From].Time).TotalSeconds;
                    var heading = 0d;
                    heading = 360 * segmentPercentage * segmentDurationInSeconds.Normalize(180);
                    var distance = segmentGroundDistance * 2 * segmentPercentage;

                    var cameraPos = currentData.ToVector().MoveTo(Math.Max(80, distance), segmentHeading + heading);
                    cameraPos.Altitude = currentData.Altitude+20 ;
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
                        if (oldHeading - heading > _cameraConfig.MaxDeltaHeadingDegrees)
                            heading = oldHeading - _cameraConfig.MaxDeltaHeadingDegrees;
                        if (heading - oldHeading > _cameraConfig.MaxDeltaHeadingDegrees)
                            heading = oldHeading + _cameraConfig.MaxDeltaHeadingDegrees;
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
                currentTime = currentTime.AddSeconds(_cameraConfig.UpdatePositionIntervalInSeconds);
            }
            var tour = new Tour { Name = "Tour by flight phase" };
            tour.Playlist = tourplaylist;
            return tour;
        }
    }



}
