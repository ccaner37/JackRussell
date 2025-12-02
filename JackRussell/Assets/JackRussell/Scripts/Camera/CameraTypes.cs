using UnityEngine;

namespace JackRussell.CameraController
{
    /// <summary>
    /// Defines different types of cameras in the system for easy identification and management.
    /// This enum is future-proof and allows for easy expansion of camera types.
    /// </summary>
    public enum CameraType
    {
        Main = 0,           // Primary game camera
        Punch = 1,          // Close-up punch/parry camera
        Aim = 2,            // Aiming camera
        Grounded = 3,       // Ground-level camera
        Airborne = 4,       // Air movement camera
        Rail = 5,           // Rail grinding camera
        Action = 6,         // General action camera
        Custom = 7          // Reserved for custom implementations
    }
}