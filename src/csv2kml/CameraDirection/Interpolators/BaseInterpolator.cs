using static DataExtensions;

namespace csv2kml.CameraDirection.Interpolators
{
    public abstract class BaseInterpolator<T> : IInterpolator<T>
    {
        protected Context _context;
        protected BoundingBoxEx _bb;
        protected IEnumerable<Data> _segmentData;
        protected BoundingBoxEx _segmentBB;

        public void Init(Context context, Segment segment)
        {
            _context = context;
            _bb = new BoundingBoxEx(_context.Data);
            _segmentData = _context.Data.ExtractSegment(segment);
            _segmentBB = new BoundingBoxEx(_segmentData);
        }

        public abstract T Calculate(DateTime time);

        protected double SegmentPercentage(DateTime time)
        {
            var dT = _segmentData.Last().Time.Subtract(_segmentData.First().Time).TotalMilliseconds;
            var cT = time.Subtract(_segmentData.First().Time).TotalMilliseconds;
            var res = cT / dT;
            return res;
        }
    }
}
