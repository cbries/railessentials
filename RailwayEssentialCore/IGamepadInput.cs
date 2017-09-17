namespace RailwayEssentialCore
{
    public interface IGamepadInput
    {
        bool IsLeft { get; set; }
        bool IsRight { get; set; }

        bool IncSpeed { get; set; }
        bool DecSpeed { get; set; }
        bool StopSpeed { get; set; }
        bool MaxSpeed { get; set; }

        bool F0 { get; set; }
        bool F1 { get; set; }
        bool F2 { get; set; }
        bool F3 { get; set; }
        bool F4 { get; set; }
        bool F5 { get; set; }
    }
}
