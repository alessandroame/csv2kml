namespace csv2kml.CameraDirection.Interpolators
{
    public class LinearInterpolator : BaseInterpolator<double>
    {
        private double _from;
        private double _to;

        public LinearInterpolator(double from, double to)
        {
            _from = from;
            _to = to;
        }
        public override double Calculate(DateTime time)
        {
            var percentage = SegmentPercentage(time);
            var res = _from + (_to - _from) * percentage;
            return res;
        }
    }
}
