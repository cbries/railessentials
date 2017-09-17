using RailwayEssentialCore;

namespace RailwayEssentialMdi.Input
{
    public class GamepadInput : IGamepadInput
    {
        public bool IsLeft { get; set; }
        public bool IsRight { get; set; }

        public bool IncSpeed { get; set; }
        public bool DecSpeed { get; set; }
        public bool StopSpeed { get; set; }
        public bool MaxSpeed { get; set; }

        public bool F0 { get; set; }
        public bool F1 { get; set; }
        public bool F2 { get; set; }
        public bool F3 { get; set; }
        public bool F4 { get; set; }
        public bool F5 { get; set; }

        public override string ToString()
        {
            return $"{IsLeft}, {IsRight}, {IncSpeed}, {DecSpeed}, {StopSpeed}, {MaxSpeed}, {F0}, {F1}, {F2}, {F3}, {F4}, {F5}";
        }
    }
}
