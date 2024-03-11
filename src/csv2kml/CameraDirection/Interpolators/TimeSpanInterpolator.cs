namespace csv2kml.CameraDirection.Interpolators
{
    public class TimeSpanInterpolator : BaseInterpolator<SharpKml.Dom.TimeSpan>
    {
        private TimeSpanRange _timeRange;

        public TimeSpanInterpolator(TimeSpanRange timeRange) 
        {
            _timeRange = timeRange;
        }

        public override SharpKml.Dom.TimeSpan Calculate(DateTime time)
        {
            var res = new SharpKml.Dom.TimeSpan();
            switch (_timeRange)
            {
                case TimeSpanRange.EntireBeginToEnd:
                    res.Begin = _context.Data.First().Time;
                    res.End = _context.Data.Last().Time;
                    break;
                case TimeSpanRange.EntireBeginToCurrent:
                    res.Begin = _context.Data.First().Time;
                    res.End = time;
                    break;
                case TimeSpanRange.EntireCurrentToEnd:
                    res.Begin = time;
                    res.End = _context.Data.Last().Time;
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
}
