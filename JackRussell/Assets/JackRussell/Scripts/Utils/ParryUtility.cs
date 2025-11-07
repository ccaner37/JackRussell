using JackRussell.Enemies;
using UnityEngine;

namespace JackRussell
{
    /// <summary>
    /// Utility class for parry-related operations.
    /// Provides modular, reusable methods for finding and managing parryable enemies.
    /// </summary>
    public static class ParryUtility
    {
        public const float PARRY_RANGE = 50;

        /// <summary>
        /// Find the nearest parryable enemy within the specified range.
        /// Returns the closest enemy that is currently in a parry window.
        /// </summary>
        /// <param name="player">The player to search from</param>
        /// <param name="PARRY_RANGE">Search radius</param>
        /// <returns>Nearest parryable enemy in parry window, or null if none found</returns>
        public static IParryable FindNearestParryableEnemy(Player player)
        {
            if (player == null) return null;

            // Find all parryable enemies in range
            Collider[] cols = Physics.OverlapSphere(player.transform.position, PARRY_RANGE, player.HomingMask);
            if (cols == null || cols.Length == 0) return null;
            
            IParryable bestTarget = null;
            float bestDistance = float.MaxValue;
            
            foreach (var col in cols)
            {
                if (col == null) continue;
                
                // Try to get IParryable from collider or parent
                var parryable = col.GetComponent<IParryable>();
                if (parryable == null)
                {
                    parryable = col.GetComponentInParent<IParryable>();
                }
                
                if (parryable == null || !parryable.IsInParryWindow) continue;
                
                float distance = Vector3.Distance(player.transform.position, parryable.ParryTargetTransform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = parryable;
                }
            }
            
            return bestTarget;
        }

        /// <summary>
        /// Check if any parryable enemy is currently in parry window within the specified range.
        /// </summary>
        /// <param name="player">The player to search from</param>
        /// <param name="range">Search radius</param>
        /// <returns>True if any parryable enemy is in parry window, false otherwise</returns>
        public static bool HasParryableEnemyInRange(Player player, float range)
        {
            return FindNearestParryableEnemy(player) != null;
        }

        /// <summary>
        /// Get all parryable enemies in range, regardless of their parry window state.
        /// </summary>
        /// <param name="player">The player to search from</param>
        /// <param name="range">Search radius</param>
        /// <returns>Array of all parryable enemies in range</returns>
        public static IParryable[] GetAllParryableEnemiesInRange(Player player, float range)
        {
            if (player == null) return null;

            Collider[] cols = Physics.OverlapSphere(player.transform.position, range, player.HomingMask);
            if (cols == null || cols.Length == 0) return null;

            var parryableList = new System.Collections.Generic.List<IParryable>();
            
            foreach (var col in cols)
            {
                if (col == null) continue;
                
                var parryable = col.GetComponent<IParryable>();
                if (parryable == null)
                {
                    parryable = col.GetComponentInParent<IParryable>();
                }
                
                if (parryable != null)
                {
                    parryableList.Add(parryable);
                }
            }
            
            return parryableList.ToArray();
        }
    }
}