namespace csv2kml.CameraDirection.Interpolators
{
    public interface IInterpolator<T>
    {
        public void Init(Context context, Segment segment);
        public abstract T Calculate(DateTime time);

    }
}
