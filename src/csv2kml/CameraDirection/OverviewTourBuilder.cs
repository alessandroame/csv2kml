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
                From = 0,
                To = _ctx.Data.Count() - 1
            };
            var wholeBB = new BoundingBoxEx(_ctx.Data);
            var timeFactor = 300;
            var generator = new DirectionsGenerator(_ctx);
            //Reveal all data
            //var flyTos = generator.BuildTrackingShot(
            //        wholeSegment, timeFactor / 2,
            //        0, 360,
            //        0, 80,
            //        wholeBB.GroundDiagonalSize / 3 * 2, wholeBB.GroundDiagonalSize * 1.4,
            //        TimeSpanRange.SegmentBeginToEnd,
            //        LookAtReference.EntireBoundingBoxCenter
            //    );

            var duration = _ctx.Data.Last().Time.Subtract(_ctx.Data.First().Time).TotalSeconds;

            
            var flyTos = generator.BuildTrackingShot(
                wholeSegment, 5/duration,
                0, 360,//heading
                0, 80,//tilt
                3, 2,//range
                TimeSpanRange.EntireBeginToEnd,
                LookAtReference.EntireBoundingBoxCenter
            );
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);

            //Rewind to pilot
            flyTos = generator.BuildTrackingShot(
                    wholeSegment, timeFactor,
                    360, 360 + 120,
                    80, 20,
                    2, 1,
                    TimeSpanRange.SegmentReverseBeginToCurrent,
                    LookAtReference.PilotPosition
                );
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);

            var heading = 0;
            timeFactor = 20;
            foreach (var segment in reducedSegments)
            { 
                if (segment.ThermalType == ThermalType.None)
                {
                    flyTos = generator.BuildTrackingShot(
                                   segment, timeFactor,
                                   heading, heading-30,//heading
                                   80, 60,//tilt
                                   1, 3,//range
                                   TimeSpanRange.SegmentBeginToCurrent,
                                   LookAtReference.SegmentBoundingBoxCenter
                               );
                    foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                }
                else
                {
                    flyTos = generator.BuildTrackingShot(
                                   segment, timeFactor,
                                   heading, heading + 90, //heading
                                   60, 80,//tilt
                                   3, 5,//range
                                   TimeSpanRange.SegmentBeginToCurrent,
                                   LookAtReference.CurrentPoint
                               );
                    foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                    heading += 180;
                }
                while (heading > 360) heading -= 360;
                while (heading < 0) heading += 360;
                //Console.WriteLine($"Current Heading {heading}°");
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
