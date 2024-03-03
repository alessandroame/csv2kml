using SharpKml.Dom.GX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataExtensions;

namespace csv2kml.CameraDirection
{
    public static class DirectionsManager
    {

        public static FlyTo[] CreateCircularTracking(IEnumerable<Data> data, double duration, int rotationdirection,
                                              ref double heading, double altitudeOffset, int stepCount = 10)
        {
            var res = new List<FlyTo>();
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
                                                    visibleTimeFrom, visibleTimeTo, altitudeOffset)
                };
                res.Add(flyTo);
            }
            return res.ToArray();
        }

        public static FlyTo CreateFixedShot(IEnumerable<Data> data, double duration, double heading, double altitudeOffset)
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
                visibleTimeFrom, visibleTimeTo, altitudeOffset)
            };
            return res;
        }
    }
}
