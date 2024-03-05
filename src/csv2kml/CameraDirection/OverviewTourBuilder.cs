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
            var heading = 120D;
            var headingOffset = 90;
            var tourplaylist = new Playlist();
            //build camera directions
            var flyTos = DirectionsManager.CreateCircularTracking(_ctx.Data, 2, -1, ref heading, _ctx.AltitudeOffset);
            foreach (var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
            tourplaylist.AddTourPrimitive(DirectionsManager.CreateFixedShot(_ctx.Data, 4, heading, _ctx.AltitudeOffset));
            tourplaylist.AddTourPrimitive(DirectionsManager.CreateFixedShot(_ctx.Data.Take(2), 0.1, heading, _ctx.AltitudeOffset));
            foreach (var segment in reducedSegments)
            {
                var data = _ctx.Data.Skip(segment.From).Take(segment.To - segment.From);
                var duration = data.Last().Time.Subtract(data.First().Time).TotalSeconds;
                duration = duration / (segment.ThermalType == ThermalType.None ? 80 : 40);
                duration = Math.Min(15, Math.Max(5, duration));

                var bb = new BoundingBoxEx(data);
                data.First().ToVector().CalculateTiltPan(data.Last().ToVector(),
                    out var segmentHeading, out var segmentTilt, out var segmentDistance, out var segmentGroundDistance);

                headingOffset *= -1;
                if (segment.ThermalType == ThermalType.None)
                {
                    var flyTo= DirectionsManager.CreateFixedShot(data, duration,heading, _ctx.AltitudeOffset);
                    tourplaylist.AddTourPrimitive(flyTo);
                }
                else
                {//thermal 
                    Console.WriteLine($"#{segment.ThermalIndex} {segment.ThermalType}");
                    var count = 10;
                    flyTos = DirectionsManager.CreateCircularTracking(data, duration, headingOffset, ref heading,_ctx.AltitudeOffset, count);
                    foreach(var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                }
            }
            var tour = new Tour { Name = "Quick overview" };
            tour.Playlist = tourplaylist;
            return tour;
        }

    
    }


}
