using SharpKml.Dom;
using SharpKml.Dom.GX;
using static DataExtensions;

namespace csv2kml.CameraDirection
{
    public class OverviewTourBuilder : TourBuilder
    {
        public OverviewTourBuilder(Context context) : base(context)
        {
        }

        //todo add config

        public override Tour Build()
        {
            var reducedSegments = new List<Segment>();
            var index = 0;
            //Join non thermal segments
            foreach (var segment in _ctx.Segments)
            {
                if (reducedSegments.Count > 0
                    && reducedSegments.Last().ThermalType == ThermalType.None
                    && segment.ThermalType == ThermalType.None)
                {
                    var lastSegment = reducedSegments.Last();
                    lastSegment.To = segment.To;
                    var segmentData = _ctx.Data.Skip(lastSegment.From).Take(lastSegment.To - lastSegment.From);
                    lastSegment.FlightPhase = segmentData.VerticalSpeed().ToFlightPhase();
                }
                else
                {
                    var newSegment = segment.Clone();
                    newSegment.SegmentIndex = index++;
                    reducedSegments.Add(newSegment);
                }
            }


            var heading = 0D;
            var tilt = 0D;
            var tourplaylist = new Playlist();
            //build camera directions
            var wholeSegment = new Segment
            {
                From=0,
                To=_ctx.Data.Count()-1
            };
            var wholeBB = new BoundingBoxEx(_ctx.Data);
            var timeFactor = 400;
            //var flyTos = DirectionsGenerator.CreateSpiralTracking(_ctx.Data,wholeSegment, timeFactor, -1, heading, heading + 120, _ctx.AltitudeOffset, 80, 60);
            var flyTos = DirectionsGenerator.Build(
                _ctx.Data,
                wholeSegment,
                timeFactor,
                new LookAtFunction(_ctx.Data,wholeSegment,LookAtReference.SegmentBoundingBoxCenter),
                new FromToFunction(_ctx.Data, wholeSegment, 120, 120+360),
                new FromToFunction(_ctx.Data, wholeSegment, 0, 90),
                new FromToFunction(_ctx.Data, wholeSegment, wholeBB.GroundDiagonalSize, wholeBB.GroundDiagonalSize*2),
                new TimeSpanFunction(_ctx.Data, wholeSegment, TimeSpanRange.SegmentBeginToEnd),
                _ctx.AltitudeOffset,
                10); 
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
            heading = ((LookAt)flyTos.Last().View).Heading.Value;
            tilt = ((LookAt)flyTos.Last().View).Tilt.Value;

            flyTos = DirectionsGenerator.Build(
                _ctx.Data,
                wholeSegment,
                timeFactor,
                new LookAtFunction(_ctx.Data, wholeSegment, LookAtReference.PilotPosition),
                new FromToFunction(_ctx.Data, wholeSegment, heading, heading+90),
                new FromToFunction(_ctx.Data, wholeSegment, tilt, 80),
                new FromToFunction(_ctx.Data, wholeSegment, wholeBB.GroundDiagonalSize*2, 300),
                new TimeSpanFunction(_ctx.Data, wholeSegment, TimeSpanRange.SegmentReverseBeginToCurrent),
                _ctx.AltitudeOffset,
                10);
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);

           /* foreach (var segment in reducedSegments)
            {
                var segmentData = _ctx.Data.ExtractSegment(segment);
                var duration = segmentData.Last().Time.Subtract(segmentData.First().Time).TotalSeconds;
                duration = duration / (segment.ThermalType == ThermalType.None ? 80 : 40);
                duration = Math.Min(15, Math.Max(2, duration));

                var bb = new BoundingBoxEx(segmentData);
                segmentData.First().ToVector().CalculateTiltPan(segmentData.Last().ToVector(),
                    out var segmentHeading, out var segmentTilt, out var segmentDistance, out var segmentGroundDistance);

                headingOffset *= -1;
                if (segment.ThermalType == ThermalType.None)
                {
                    flyTos = DirectionsGenerator.CreateFixedShot(_ctx.Data,segment, timeFactor, heading, _ctx.AltitudeOffset);
                    foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                }
                else
                {//thermal 
                    var angle = 360 * 2;
                    Console.WriteLine($"#{segment.ThermalIndex} {segment.ThermalType}");
                    var count = 10;
                    flyTos = DirectionsGenerator.CreateCircularTracking(_ctx.Data, segment, duration, headingOffset,
                        heading, heading + angle, _ctx.AltitudeOffset, count);
                    foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                    heading += angle;
                }
            }

            flyTos = DirectionsGenerator.CreateSpiralTracking(_ctx.Data, wholeSegment, timeFactor, -1, heading, heading + 120, _ctx.AltitudeOffset, 50, 80);
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
            heading += 120;

            flyTos = DirectionsGenerator.CreateSpiralTracking(_ctx.Data, wholeSegment, timeFactor, -1, heading, heading + 360, _ctx.AltitudeOffset, 80, 80, 40);
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
            heading += 120;*/

            var tour = new Tour { Name = "Quick overview" };
            tour.Playlist = tourplaylist;
            return tour;
        }


    }


}
