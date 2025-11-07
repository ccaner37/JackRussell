using System;

namespace JackRussell.States
{
    /// <summary>
    /// Interface for action states that want to block certain locomotion types.
    /// </summary>
    public interface IBlocksLocomotion
    {
        /// <summary>
        /// Gets the locomotion types that this action state blocks.
        /// When blocking is active, transitions to these locomotion types are prevented.
        /// </summary>
        LocomotionType BlocksLocomotion { get; }
        
        /// <summary>
        /// Whether locomotion blocking is currently active for this state.
        /// </summary>
        bool IsBlockingLocomotion { get; }
    }
}