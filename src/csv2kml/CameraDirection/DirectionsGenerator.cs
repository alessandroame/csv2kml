using csv2kml.CameraDirection.Interpolators;
using SharpKml.Base;
using SharpKml.Dom.GX;

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
            for (var i = 0; i < stepCount; i++)
            {
                var index = segmentData.Count() / stepCount * i;
                var lastData = i == 0 ? segmentData.First() : segmentData.ElementAt(segmentData.Count() / stepCount * (i - 1));
                var currentData = segmentData.ElementAt(index);
                var time = currentData.Time;

                var duration = time.Subtract(lastData.Time).TotalMilliseconds / timeFactor / 1000;
                if (duration == 0) duration = 2;

                var lookAt = lookAtInterpolator.Calculate(time);
                var heading = headingInterpolator.Calculate(time);
                while (heading > 360) heading -= 360;
                while (heading < 0) heading += 360;


                var tilt = tiltInterpolator.Calculate(time);
                var range = rangeInterpolator.Calculate(time);
                SharpKml.Dom.TimeSpan timeSpan = timeSpanInterpolator.Calculate(time);

                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration,
                    View = CameraHelper.CreateLookAt(lookAt, range, heading, tilt, timeSpan.Begin, timeSpan.End, altitudeOffset)
                };
                res.Add(flyTo);
                Console.WriteLine($"#{index}\td:{Math.Round(duration, 2)}\th:{Math.Round(heading, 0)}\tt:{Math.Round(tilt, 0)}\tr:{Math.Round(range, 0)}\t{time}");
            }
            Console.WriteLine("---------------------------");
            return res.ToArray();
        }

        public FlyTo[] CreateTrackingShot(Segment segment, double timeFactor, 
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
                new LinearInterpolator(fromRange, toRange),
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
               new LinearInterpolator(100, 500),
               new TimeSpanInterpolator(TimeSpanRange.SegmentBeginToCurrent),
               altitudeOffset,
               stepCount);
        }
    }
}
