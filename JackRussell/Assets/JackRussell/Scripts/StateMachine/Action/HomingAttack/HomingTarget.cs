using UnityEngine;
using JackRussell;

namespace JackRussell.States.Action
{
    /// <summary>
    /// Interface for objects that can be targeted by the player's homing attack.
    /// Implement on enemies, springs or any object that should react to a homing hit.
    /// </summary>
    public interface IHomingTarget
    {
        /// <summary>
        /// The transform to use as the homing target position.
        /// </summary>
        Transform TargetTransform { get; }

        /// <summary>
        /// Whether this target is currently valid (active) for homing.
        /// If false the homing logic should skip this target.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Called when the player successfully hits this target with a homing attack.
        /// Implementations should handle effects / destruction / damage here.
        /// </summary>
        /// <param name="player">Player that hit the target.</param>
        void OnHomingHit(Player player);
        
        /// <summary>
        /// Called when the hit stop animation ends (after homing attack impact).
        /// </summary>
        /// <param name="player">Player that hit the target.</param>
        void OnHitStopEnd(Player player);
    }
}