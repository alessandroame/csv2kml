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
                    var flyTo= CreateFixedShot(data, duration,heading);
                    tourplaylist.AddTourPrimitive(flyTo);
                }
                else
                {//thermal 
                    var count = 10;
                    var flyTos = CreateCircularTracking(data, duration / count, headingOffset, ref heading, count);
                    foreach(var flyTo in flyTos) tourplaylist.AddTourPrimitive(flyTo);
                }
            }
            var tour = new Tour { Name = "Quick overview" };
            tour.Playlist = tourplaylist;
            return tour;
        }

        public FlyTo[] CreateCircularTracking(IEnumerable<Data> data, double duration, int rotationdirection,ref double heading, int stepCount = 10)
        {
            var res=new List<FlyTo>();
            var bb = new BoundingBoxEx(data); 
            var visibleTimeFrom = data.First().Time;
            var totalRotation = duration * 10;
            Console.WriteLine($"DURATION: {duration}  ROTATION:{totalRotation}");
            for (var i = 0; i < stepCount; i++)
            {
                heading += 120 / stepCount * Math.Sign(rotationdirection);
                /*while (heading < 0) heading += 360;
                while (heading > 360) heading -= 360;*/
                //Console.WriteLine($"heeading: {heading} phase #{segment.SegmentIndex} {segment.FlightPhase}  thermal #{segment.ThermalIndex}");
                var lookAtIndex = data.Count() / stepCount * i;
                var visibleTimeTo = data.ElementAt(lookAtIndex).Time;
                var lookAt = data.ElementAt(lookAtIndex).ToVector();
                var range = Math.Max(400, bb.GroundDiagonalSize * 2);

                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration / stepCount,
                    View = CameraHelper.CreateLookAt(lookAt, range, heading, 80,
                                                    visibleTimeFrom, visibleTimeTo, _ctx.AltitudeOffset)
                };
                res.Add(flyTo);
            }
            return res.ToArray();
        }

        public FlyTo CreateFixedShot(IEnumerable<Data> data,double duration,double heading)
        {
            var bb = new BoundingBoxEx(data);
            var visibleTimeFrom = data.First().Time;
            var visibleTimeTo = data.Last().Time;
            var lookAt = bb.Center;
            var range = Math.Max(250, bb.GroundDiagonalSize * 2);
            var res = new FlyTo
            {
                Mode = FlyToMode.Bounce,
                Duration = duration,
                View = CameraHelper.CreateLookAt(lookAt, range, heading, 70,
                visibleTimeFrom, visibleTimeTo, _ctx.AltitudeOffset)
            };
            return res;
        }
    }


}
