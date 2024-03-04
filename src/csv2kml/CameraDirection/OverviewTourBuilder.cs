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
            var tourplaylist = new Playlist();
            var heading = 0D;
            var headingOffset = 90;
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
            //build camera directions
            foreach (var segment in reducedSegments)
            {
                var data = _ctx.Data.Skip(segment.From).Take(segment.To - segment.From);
                var duration = data.Last().Time.Subtract(data.First().Time).TotalSeconds;
                duration = duration / (segment.ThermalType == ThermalType.None ? 20 : 40);
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
                    var count = 10;
                    var flyTos = DirectionsManager.CreateCircularTracking(data, duration / count, headingOffset, ref heading,_ctx.AltitudeOffset, count);
                    foreach(var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                }
            }
            var tour = new Tour { Name = "Quick overview" };
            tour.Playlist = tourplaylist;
            return tour;
        }

    
    }


}
