
namespace csv2kml
{
    public class Segment
    {
        public int SegmentIndex { get; set; }
        public FlightPhase FlightPhase { get; set; }
        public int ThermalIndex { get; set; }
        public ThermalType ThermalType { get; set; } = ThermalType.None;
        public int From { get; set; }
        public int To { get; set; }
        public override string ToString()
        {
            var res = $" {From}-{To}";
            if (ThermalType==ThermalType.None)
                res =$"{FlightPhase} {res}";
            else
                res = $"{ThermalType} thermal {res}";
            return res;
        }

        internal Segment Clone()
        {
            return new Segment
            {
                SegmentIndex = SegmentIndex,
                FlightPhase = FlightPhase,
                ThermalIndex = ThermalIndex,
                ThermalType = ThermalType,
                From = From,
                To = To
            };
        }
    }
}