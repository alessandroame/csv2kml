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
            var res= (_to - _from) * percentage;
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
        SegmentCurrentToEnd
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




    public static class DirectionsGenerator
    {
        public static FlyTo[] Build(
            IEnumerable<Data> data, 
            Segment segment,
            double duration, 
            LookAtFunction lookAtFunc, 
            DataFunction<double> headingFunc, 
            DataFunction<double> tiltFunc, 
            DataFunction<double> rangeFunc, 
            TimeSpanFunction timeSpanFunc,
            double altitudeOffset, 
            int stepCount = 10)
        {
            var segmentData = data.ExtractSegment(segment);
            var res = new List<FlyTo>();
            for (var i = 0; i < stepCount; i++)
            {
                var index = segmentData.Count()/ stepCount*i;
                var currentData = segmentData.ElementAt(index);
                var time = currentData.Time;
                var lookAt = lookAtFunc.Calculate(time);
                var heading = headingFunc.Calculate(time);
                var tilt = tiltFunc.Calculate(time);
                var range = rangeFunc.Calculate(time);
                SharpKml.Dom.TimeSpan timeSpan = timeSpanFunc.Calculate(time);

                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration / stepCount,
                    View = CameraHelper.CreateLookAt(lookAt, range, heading, tilt, timeSpan.Begin, timeSpan.End, altitudeOffset)
                };
                res.Add(flyTo);
                Console.WriteLine($" #{index} h:{heading} t:{tilt} r:{range} {time}");
            }
            return res.ToArray();
        }




        public static FlyTo[] CreateCircularTracking(IEnumerable<Data> data, Segment segment, double duration, int rotationdirection,
                                              double fromHeading, double toHeading, double altitudeOffset, int stepCount = 10)
        {
            return CreateSpiralTracking(data, segment , duration, rotationdirection, fromHeading, toHeading, altitudeOffset, 20, 80, stepCount);
        }
        public static FlyTo[] CreateSpiralTracking(IEnumerable<Data> data,Segment segment, double duration, int rotationdirection,
                                              double fromHeading, double toHeading,
                                              double altitudeOffset, double fromTilt, int toTilt, int stepCount = 10)
        {
            return Build(
                data,
                segment,
                duration,
                new LookAtFunction(
                    data,
                    segment,
                    LookAtReference.SegmentBoundingBoxCenter),
                new FromToFunction(data, segment, fromHeading, toHeading),
                new FromToFunction(data, segment, fromTilt, toTilt),
                new FromToFunction(data, segment, 600, 1000),
                new TimeSpanFunction(data, segment, TimeSpanRange.SegmentBeginToEnd),
                altitudeOffset,
                stepCount);
        }

        public static FlyTo[] CreateCircularTracking(IEnumerable<Data> data, double duration, int rotationdirection,
                                      double fromHeading, double toHeading, double altitudeOffset, int stepCount = 10)
        {
            return CreateSpiralTracking(data, duration, rotationdirection, fromHeading, toHeading, altitudeOffset, 80, 80, stepCount);
        }
        public static FlyTo[] CreateSpiralTracking(IEnumerable<Data> data, double duration, int rotationdirection,
                                              double fromHeading, double toHeading,
                                              double altitudeOffset, double fromTilt, int toTilt, int stepCount = 10)
        {
            var res = new List<FlyTo>();
            var bb = new BoundingBoxEx(data);
            var visibleTimeFrom = data.First().Time;
            var dTilt = (toTilt - fromTilt) / stepCount;
            var tilt = fromTilt;
            var dHeading = (toHeading - fromHeading) / stepCount;
            var heading = fromHeading;
            for (var i = 0; i < stepCount; i++)
            {
                //heading += 120 / stepCount * Math.Sign(rotationdirection);
                while (heading < 0) heading += 360;
                while (heading > 360) heading -= 360;
                //Console.WriteLine($"heeading: {heading} phase #{segment.SegmentIndex} {segment.FlightPhase}  thermal #{segment.ThermalIndex}");
                var lookAtIndex = data.Count() / stepCount * i;
                var visibleTimeTo = data.ElementAt(lookAtIndex).Time;
                //var lookAt = data.ElementAt(lookAtIndex).ToVector();
                var lookAt = bb.Center;
                var range = Math.Max(400, bb.GroundDiagonalSize * 1.5);

                var flyTo = new FlyTo
                {
                    Mode = FlyToMode.Smooth,
                    Duration = duration / stepCount,
                    View = CameraHelper.CreateLookAt(lookAt, range, heading, tilt,
                                                    visibleTimeFrom, visibleTimeTo, altitudeOffset)
                };
                tilt += dTilt;
                heading += dHeading;
                res.Add(flyTo);
                Console.WriteLine($" DURATION: {flyTo.Duration}  ROTATION:{heading}");
            }
            return res.ToArray();
        }

        public static FlyTo CreateFixedShot(IEnumerable<Data> data, double duration, double heading, double altitudeOffset)
        {
            var bb = new BoundingBoxEx(data);
            var visibleTimeFrom = data.First().Time;
            var visibleTimeTo = data.Last().Time;
            var lookAt = bb.Center;
            var range = Math.Max(250, bb.GroundDiagonalSize * 1.5);
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
