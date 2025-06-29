using csv2kml.CameraDirection.Interpolators;
using SharpKml.Base;
using SharpKml.Dom.GX;
using System;

namespace csv2kml.CameraDirection
{

    public class DirectionsGenerator
    {
        private Context _ctx;
        private IEnumerable<Data> _data;
        private double _altitudeOffset;

        public DirectionsGenerator(Context ctx)
        {
            _ctx = ctx;
            _data = ctx.Data;
            _altitudeOffset = ctx.AltitudeOffset;
        }
        public FlyTo[] Build(Segment segment,
                                double timeFactor,
                                IInterpolator<Vector> lookAtInterpolator,
                                IInterpolator<double> headingInterpolator,
                                IInterpolator<double> tiltInterpolator,
                                IInterpolator<double> rangeInterpolator,
                                IInterpolator<SharpKml.Dom.TimeSpan> timeSpanInterpolator,
                                double altitudeOffset,
                                int stepCount = 10)
        {
            lookAtInterpolator.Init(_ctx, segment);
            headingInterpolator.Init(_ctx, segment);
            tiltInterpolator.Init(_ctx, segment);
            rangeInterpolator.Init(_ctx, segment);
            timeSpanInterpolator.Init(_ctx, segment);

            var segmentData = _data.ExtractSegment(segment);
            var res = new List<FlyTo>();
            var deltaSegment = segmentData.Count() / (double)stepCount;
            for (var i = 1; i <= stepCount; i++)
            {
                var index = (int)Math.Min(deltaSegment * i, segmentData.Count() - 1);
                var lastIndex = segmentData.Count() / stepCount * (i - 1);
                var lastData = segmentData.ElementAt(lastIndex);
                var currentData = segmentData.ElementAt(index);
                var time = currentData.Time;

                var duration = time.Subtract(lastData.Time).TotalMilliseconds / timeFactor / 1000;

                var lookAt = lookAtInterpolator.Eval(time);
                var heading = headingInterpolator.Eval(time);
                while (heading > 360) heading -= 360;
                while (heading < 0) heading += 360;


                var tilt = tiltInterpolator.Eval(time);
                var range = rangeInterpolator.Eval(time);
                SharpKml.Dom.TimeSpan timeSpan = timeSpanInterpolator.Eval(time);

                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration,
                    View = CameraHelper.CreateLookAt(lookAt, range, heading, tilt, timeSpan.Begin, timeSpan.End, altitudeOffset)
                };
                res.Add(flyTo);
                Console.WriteLine($"#{index}\tduration:{Math.Round(duration, 2)}\theading:{Math.Round(heading, 0)}\ttilt:{Math.Round(tilt, 0)}\trange:{Math.Round(range, 0)}\t{time}\tdata #{currentData.Index}");
            }
            Console.WriteLine(". . . . . . . . . . . . . . . . . ");
            return res.ToArray();
        }

        public FlyTo[] BuildTrackingShotByTimeFactor(Segment segment, double timeFactor,
                                          double fromHeading, double toHeading,
                                          double fromTilt, double toTilt,
                                          double fromRange, double toRange,
                                          TimeSpanRange timeSpanRange,
                                          LookAtReference reference,
                                          int stepCount = 10)
        {
            return Build(
            segment,
            timeFactor,
            new LookAtInterpolator(reference),
            new LinearInterpolator(fromHeading, toHeading),
            new LinearInterpolator(fromTilt, toTilt),
            new BoundingBoxInterpolator((dataBB, segmentBB, percentage) =>
            {
                var res = segmentBB.DiagonalSize * (fromRange + (toRange - fromRange) * percentage);
                return res;
            }),
            new TimeSpanInterpolator(timeSpanRange),
            _altitudeOffset,
            stepCount);
        }

        public FlyTo[] CreateFixedShot(Segment segment, double timeFactor, double heading, double altitudeOffset, LookAtReference reference, int stepCount = 10)
        {
            return Build(
               segment,
               timeFactor,
               new LookAtInterpolator(reference),
               new LinearInterpolator(heading, heading),
               new LinearInterpolator(70, 70),
                new BoundingBoxInterpolator((dataBB, segmentBB, percentage) =>
                {
                    var res = segmentBB.DiagonalSize * 2 * percentage;
                    return res;
                }),
               new TimeSpanInterpolator(TimeSpanRange.SegmentBeginToCurrent),
               altitudeOffset,
               stepCount);
        }
    }
}
