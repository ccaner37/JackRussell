using System;

namespace JackRussell.States
{
    /// <summary>
    /// Types of locomotion that can be blocked by action states.
    /// </summary>
    [Flags]
    public enum LocomotionType
    {
        None = 0,
        Move = 1 << 0,
        Sprint = 1 << 1,
        Jump = 1 << 2,
        FastFall = 1 << 3,
        Dash = 1 << 4,
        Grind = 1 << 5,
        Crouch = 1 << 6,
        Fall = 1 << 7,
        Land = 1 << 8,
        PathFollow = 1 << 9,
        DashPanel = 1 << 10,

        // Combinations for convenience
        All = Move | Sprint | Jump | FastFall | Dash | Grind | Crouch | Fall | Land | PathFollow | DashPanel,
        Movement = Move | Sprint | Jump | FastFall | Dash,
        AirControl = Jump | FastFall | Dash | Fall
    }
}