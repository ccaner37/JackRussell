using UnityEngine;

namespace JackRussell.Enemies
{
    /// <summary>
    /// Interface for enemies that can be parried by the player.
    /// Provides parry window functionality and instant kill mechanics.
    /// </summary>
    public interface IParryable
    {
        /// <summary>
        /// Whether this enemy is currently in a parry window.
        /// </summary>
        bool IsInParryWindow { get; }
        
        /// <summary>
        /// The transform where player should teleport to for successful parry.
        /// </summary>
        Transform ParryTargetTransform { get; }
        
        /// <summary>
        /// Called when player successfully parries this enemy.
        /// Should handle instant death and visual effects.
        /// </summary>
        /// <param name="player">Player that performed the parry</param>
        void OnParried(Player player);
        
        /// <summary>
        /// Called when parry window opens (enemy becomes vulnerable).
        /// </summary>
        void OnParryWindowOpen();
        
        /// <summary>
        /// Called when parry window closes (enemy no longer vulnerable).
        /// </summary>
        void OnParryWindowClose();
    }
}