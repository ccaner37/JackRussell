using UnityEngine;
using VitalRouter;

namespace JackRussell
{
    public struct PressureCollectParticleCommand : ICommand
    {
        public Vector3 EnemyWorldPosition { get; }
        public float TotalPressure { get; }

        public PressureCollectParticleCommand(Vector3 enemyWorldPosition, float totalPressure)
        {
            EnemyWorldPosition = enemyWorldPosition;
            TotalPressure = totalPressure;
        }
    }
}