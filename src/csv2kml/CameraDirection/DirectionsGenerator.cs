using Microsoft.Win32.SafeHandles;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Dom.GX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataExtensions;

namespace csv2kml.CameraDirection
{
    public abstract class BaseFunction<T>
    {
        public abstract T Calculate(DateTime time);
    }
    public abstract class DataFunction<T> : BaseFunction<T>
    {
        protected IEnumerable<Data> _data;
        protected BoundingBoxEx _bb;
        protected IEnumerable<Data> _segmentData;
        protected BoundingBoxEx _segmentBB;

        public DataFunction(IEnumerable<Data> data,Segment segment)
        {
            _data = data;
            _bb = new BoundingBoxEx(data);
            _segmentData = data.ExtractSegment(segment);
            _segmentBB = new BoundingBoxEx(_segmentData);
        }

        protected double SegmentPercentage(DateTime time)
        {
            var dT = _segmentData.Last().Time.Subtract(_segmentData.First().Time).TotalMilliseconds;
            var cT = time.Subtract(_segmentData.First().Time).TotalMilliseconds;
            var res = cT / dT;
            return res;
        }
    }
    public class FromToFunction : DataFunction<double>
    {
        private double _from;
        private double _to;

        public FromToFunction(IEnumerable<Data> data,Segment segment,double from, double to):base(data,segment)
        {
            _from = from;
            _to = to;
        }
        public override double Calculate(DateTime time)
        {
            var percentage = SegmentPercentage(time);
            var res= _from+(_to - _from) * percentage;
            return res;
        }
    }
    public class LookAtFunction : DataFunction<Vector>
    {
        private LookAtReference _reference;

        public LookAtFunction(IEnumerable<Data> data,Segment segment, LookAtReference reference) : base(data,segment) {
            _reference = reference;
        }
    
        public override Vector Calculate(DateTime time)
        {
            Vector res = res = _bb.Center; 
            switch (_reference)
            {
                case LookAtReference.CurrentPoint:
                    res=_data.GetDataByTime(time).ToVector();
                    break;
                case LookAtReference.EntireCurrentBoundingBoxCenter:
                    var ebb = new BoundingBoxEx(_data.GetDataByTime(_data.ElementAt(0).Time, time));
                    res = ebb.Center;
                    break;
                case LookAtReference.EntireBoundingBoxCenter:
                    res = _bb.Center;
                    break;
                case LookAtReference.SegmentCurrentBoundingBoxCenter:
                    var sbb = new BoundingBoxEx(_segmentData.GetDataByTime(_segmentData.ElementAt(0).Time, time));
                    res = sbb.Center;
                    break;
                case LookAtReference.SegmentBoundingBoxCenter:
                    res = _segmentBB.Center;
                    break;
                case LookAtReference.PilotPosition:
                    res = _data.ElementAt(0).ToVector();
                    break;
            }
            return res;
        }
    }
    public class TimeSpanFunction : DataFunction<SharpKml.Dom.TimeSpan>
    {
        private TimeSpanRange _timeRange;

        public TimeSpanFunction(IEnumerable<Data> data, Segment segment, TimeSpanRange timeRange) : base(data, segment)
        {
            _timeRange = timeRange;
        }

        public override SharpKml.Dom.TimeSpan Calculate(DateTime time)
        {
            var res = new SharpKml.Dom.TimeSpan();
            switch (_timeRange)
            {
                case TimeSpanRange.EntireBeginToEnd:
                    res.Begin = _data.First().Time;
                    res.End = _data.Last().Time;
                    break;
                case TimeSpanRange.EntireBeginToCurrent:
                    res.Begin = _data.First().Time;
                    res.End = time;
                    break;
                case TimeSpanRange.EntireCurrentToEnd:
                    res.Begin = time;
                    res.End = _data.Last().Time;
                    break;
                case TimeSpanRange.SegmentBeginToEnd:
                    res.Begin = _segmentData.First().Time;
                    res.End = _segmentData.Last().Time;
                    break;
                case TimeSpanRange.SegmentBeginToCurrent:
                    res.Begin = _segmentData.First().Time;
                    res.End = time;
                    break;
                case TimeSpanRange.SegmentCurrentToEnd:
                    res.Begin = time;
                    res.End = _segmentData.Last().Time;
                    break;
                case TimeSpanRange.SegmentReverseBeginToCurrent:
                    res.Begin = _segmentData.First().Time;
                    res.End = _segmentData.Last().Time.AddSeconds(_segmentData.First().Time.Subtract(time).TotalSeconds);
                    break;
            }
            return res;
        }
    }

    public enum TimeSpanRange
    {
        EntireBeginToEnd,
        EntireBeginToCurrent,
        EntireCurrentToEnd,
        SegmentBeginToEnd,
        SegmentBeginToCurrent,
        SegmentCurrentToEnd,
        SegmentReverseBeginToCurrent
    }

    public enum LookAtReference
    {
        CurrentPoint,
        EntireBoundingBoxCenter,
        EntireCurrentBoundingBoxCenter,
        SegmentBoundingBoxCenter,
        SegmentCurrentBoundingBoxCenter,
        PilotPosition
    }




    public class DirectionsGenerator
    {
        private IEnumerable<Data> _data;
        private double _altitudeOffset;

        public DirectionsGenerator(Context ctx)
        {
            _data = ctx.Data;
            _altitudeOffset = ctx.AltitudeOffset;
        }
        public FlyTo[] Build(Segment segment,
                                double timeFactor, 
                                LookAtFunction lookAtFunc, 
                                DataFunction<double> headingFunc, 
                                DataFunction<double> tiltFunc, 
                                DataFunction<double> rangeFunc, 
                                TimeSpanFunction timeSpanFunc,
                                double altitudeOffset, 
                                int stepCount = 10)
        {
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

                var lookAt = lookAtFunc.Calculate(time);
                var heading = headingFunc.Calculate(time);
                while (heading > 360) heading -= 360;
                while (heading < 0) heading += 360;


                var tilt = tiltFunc.Calculate(time);
                var range = rangeFunc.Calculate(time);
                SharpKml.Dom.TimeSpan timeSpan = timeSpanFunc.Calculate(time);

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
                new LookAtFunction(_data, segment, reference),
                new FromToFunction(_data, segment, fromHeading, toHeading),
                new FromToFunction(_data, segment, fromTilt, toTilt),
                new FromToFunction(_data, segment, fromRange, toRange),
                new TimeSpanFunction(_data, segment, timeSpanRange),
                _altitudeOffset,
                stepCount);
        }

        public FlyTo[] CreateFixedShot(Segment segment, double timeFactor, double heading, double altitudeOffset, LookAtReference reference, int stepCount = 10)
        {
            return Build(
               segment,
               timeFactor,
               new LookAtFunction(_data,segment, reference),
               new FromToFunction(_data, segment, heading, heading),
               new FromToFunction(_data, segment, 70, 70),
               new FromToFunction(_data, segment, 100, 500),
               new TimeSpanFunction(_data, segment, TimeSpanRange.SegmentBeginToCurrent),
               altitudeOffset,
               stepCount);
        }
    }
}
