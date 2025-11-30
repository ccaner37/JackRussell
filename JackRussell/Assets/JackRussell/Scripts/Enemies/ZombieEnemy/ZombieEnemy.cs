using JackRussell.Enemies;
using UnityEngine;

namespace JackRussell
{
    public class ZombieEnemy : Enemy
    {
        public override bool IsActive => IsEnemyActive;
    }
}
