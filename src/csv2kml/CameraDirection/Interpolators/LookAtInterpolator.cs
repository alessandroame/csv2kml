using SharpKml.Base;
using static DataExtensions;

namespace csv2kml.CameraDirection.Interpolators
{
    public class LookAtInterpolator : BaseInterpolator<Vector>
    {
        private LookAtReference _reference;

        public LookAtInterpolator(LookAtReference reference)
        {
            _reference = reference;
        }

        public override Vector Eval(DateTime time)
        {
            Vector res = _bb.Center;
            switch (_reference)
            {
                case LookAtReference.CurrentPoint:
                    res = _context.Data.GetDataByTime(time).ToVector();
                    break;
                case LookAtReference.EntireCurrentBoundingBoxCenter:
                    var ebb = new BoundingBoxEx(_context.Data.GetDataByTime(_context.Data.ElementAt(0).Time, time));
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
                    res = _context.Data.ElementAt(0).ToVector();
                    break;
            }
            return res;
        }
    }
}
