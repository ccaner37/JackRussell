using UnityEngine;
using VitalRouter;

namespace JackRussell
{
    public struct PressureCollectParticleCommand : ICommand
    {
        public Vector3 EnemyWorldPosition { get; }

        public PressureCollectParticleCommand(Vector3 enemyWorldPosition)
        {
            EnemyWorldPosition = enemyWorldPosition;
        }
    }
}