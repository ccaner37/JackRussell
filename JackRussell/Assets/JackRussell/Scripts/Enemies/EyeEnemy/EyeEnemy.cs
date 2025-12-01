using JackRussell.Enemies;
using UnityEngine;

namespace JackRussell
{
    public class EyeEnemy : Enemy
    {
        public override bool IsActive => IsEnemyActive;

        public override void OnDeath()
        {
            base.OnDeath();
        }
    }
}
