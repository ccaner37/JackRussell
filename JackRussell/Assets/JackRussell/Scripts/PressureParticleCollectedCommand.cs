using VitalRouter;

namespace JackRussell
{
    public struct PressureParticleCollectedCommand : ICommand
    {
        public float PressureAmount { get; }

        public PressureParticleCollectedCommand(float pressureAmount)
        {
            PressureAmount = pressureAmount;
        }
    }
}