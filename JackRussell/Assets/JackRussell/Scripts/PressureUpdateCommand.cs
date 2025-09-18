using VitalRouter;

namespace JackRussell
{
    public struct PressureUpdateCommand : ICommand
    {
        public float Pressure { get; }

        public PressureUpdateCommand(float pressure)
        {
            Pressure = pressure;
        }
    }
}