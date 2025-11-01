using VitalRouter;

namespace JackRussell
{
    public struct DashChargesUpdateCommand : ICommand
    {
        public int CurrentCharges { get; }

        public DashChargesUpdateCommand(int currentCharges)
        {
            CurrentCharges = currentCharges;
        }
    }
}