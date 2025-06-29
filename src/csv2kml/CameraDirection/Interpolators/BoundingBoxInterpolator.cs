namespace csv2kml.CameraDirection.Interpolators
{
    public class BoundingBoxInterpolator : BaseInterpolator<double>
    {
        private Func<BoundingBoxEx, BoundingBoxEx, double,double> _evalFunc;

        public BoundingBoxInterpolator(Func<BoundingBoxEx, BoundingBoxEx, double,double> evalFunc) {
            _evalFunc = evalFunc;
        }

        public override double Eval(DateTime time)
        {
            var percentage = SegmentPercentage(time);
            var res = _evalFunc(_bb,_segmentBB,percentage);
            return res;
        }
    }
}
