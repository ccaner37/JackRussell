using VitalRouter;

namespace JackRussell
{
    public struct StarCollectedUpdateCommand : ICommand
    {
        public int CollectedCount { get; }

        public StarCollectedUpdateCommand(int collectedCount)
        {
            CollectedCount = collectedCount;
        }
    }
}