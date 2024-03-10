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

            var tourplaylist = new Playlist();
            //build camera directions
            var wholeSegment = new Segment
            {
                From=0,
                To=_ctx.Data.Count()-1
            };
            var wholeBB = new BoundingBoxEx(_ctx.Data);
            var timeFactor = 400;

            var generator = new DirectionsGenerator(_ctx);
            //Reveal all data
            //var flyTos = generator.CreateTrackingShot(
            //        wholeSegment, timeFactor / 2,
            //        0, 360,
            //        0, 80,
            //        wholeBB.GroundDiagonalSize / 3 * 2, wholeBB.GroundDiagonalSize * 1.4,
            //        TimeSpanRange.SegmentBeginToEnd,
            //        LookAtReference.EntireBoundingBoxCenter
            //    );

            //var flyTos = generator.CreateTrackingShot(
            //    wholeSegment, timeFactor / 2,
            //    0, 360,
            //    0, 80,
            //    wholeBB.GroundDiagonalSize / 3 * 2, wholeBB.GroundDiagonalSize * 1.4,
            //    TimeSpanRange.SegmentBeginToCurrent,
            //    LookAtReference.SegmentCurrentBoundingBoxCenter
            //);
            //foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);

            ////Rewind to pilot
            //flyTos = generator.CreateTrackingShot(
            //        wholeSegment, timeFactor,
            //        360, 360+120,
            //        80, 20,
            //        wholeBB.GroundDiagonalSize * 1.4, 300,
            //        TimeSpanRange.SegmentReverseBeginToCurrent,
            //        LookAtReference.PilotPosition
            //    );
            //foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);

            var heading = 360 + 120;
            timeFactor = 40;
            foreach (var segment in reducedSegments)
            {
                if (segment.ThermalType == ThermalType.None)
                {
                    var flyTos = generator.CreateTrackingShot(
                                   segment, timeFactor,
                                   heading, heading,
                                   60, 60,
                                   450, 450,
                                   TimeSpanRange.SegmentBeginToCurrent,
                                   LookAtReference.SegmentBoundingBoxCenter
                               );
                    foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                }
                else
                {
                    var flyTos = generator.CreateTrackingShot(
                                   segment, timeFactor,
                                   heading, heading + 180,
                                   80,80,
                                   450, 600,
                                   TimeSpanRange.SegmentBeginToCurrent,
                                   LookAtReference.CurrentPoint
                               );
                    foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                    heading += 180;
                }
                while (heading > 360) heading -= 360;
                while (heading < 0) heading += 360;
                Console.WriteLine($"°°°°°°°°°°°°°°°°°°°°° {heading}");
            }

            //flyTos = DirectionsGenerator.CreateSpiralTracking(_ctx.Data, wholeSegment, timeFactor, -1, heading, heading + 120, _ctx.AltitudeOffset, 50, 80);
            //foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
            //heading += 120;

            //flyTos = DirectionsGenerator.CreateSpiralTracking(_ctx.Data, wholeSegment, timeFactor, -1, heading, heading + 360, _ctx.AltitudeOffset, 80, 80, 40);
            //foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
            //heading += 120;

            var tour = new Tour { Name = "Quick overview" };
            tour.Playlist = tourplaylist;
            return tour;
        }


    }


}
