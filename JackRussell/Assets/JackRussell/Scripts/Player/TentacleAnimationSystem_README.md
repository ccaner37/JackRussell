# Tentacle Idle Animation System

## Overview
This system provides a robust, future-proof solution for tentacle idle animations with clean separation of concerns. The tentacle follows a target transform that gently sways while idle, creating a hover-like effect.

## Components

### TentacleController.cs
- **Purpose**: Manages tentacle idle animations and future aiming functionality
- **Features**:
  - DOTween-based smooth idle sway animation
  - Position-based aiming (future use)
  - Configurable animation parameters
  - Clean start/stop interface
  - Gizmo visualization for debugging

### Integration Points

#### Player.cs
- Added `_tentacleController` serialized field
- Added `TentacleController` accessor property
- Calls `UpdateController()` in Player's Update method

#### IdleState.cs  
- Starts tentacle idle animation on Enter
- Stops tentacle idle animation on Exit

## Setup Instructions

1. **Attach TentacleController**:
   - Add `TentacleController` component to your Player object
   - Assign your tentacle target transform to `_tentacleTarget`

2. **Configure Animation Parameters** (in Inspector):
   - `_idleAmplitude`: How much the tentacle sways (default: 0.1)
   - `_idleFrequency`: How fast it sways (default: 1.5)
   - `_idleAxis`: Sway direction vector (default: 0, 0.1, 0.05)

3. **Assign in Player**:
   - In Player inspector, drag your TentacleController component to the `_tentacleController` field

## Usage

### Current Functionality
- **Automatic**: Idle animation starts when player enters IdleState
- **Automatic**: Animation stops when player exits IdleState
- **Manual Control**: Use `_player.TentacleController.StartIdleAnimation()` / `StopIdleAnimation()`

### Future Aiming (Planned)
```csharp
// Start aiming at a position
_player.TentacleController.StartAiming(targetPosition);

// Update aim position during combat
_player.TentacleController.UpdateAimPosition(newTargetPosition);

// Stop aiming and return to idle
_player.TentacleController.StopAiming(true);
```

## Features

### Robust Design
- Null safety checks throughout
- Proper cleanup of DOTween animations
- Graceful handling of missing references
- Editor validation for reasonable parameter values

### Future-Proof
- Position-based aiming (not rotation)
- Configurable animation parameters
- Separate controller for tentacle logic
- Easy integration with state machine

### Performance
- Lightweight DOTween animations
- Update only when needed
- Efficient vector math

### Debugging
- Gizmo visualization shows sway area
- Real-time aim position visualization
- Clear public properties for inspection

## Customization

### Animation Behavior
Modify `CreateIdleSwayTween()` in TentacleController for different animation patterns:
- Use different easing functions
- Change loop types
- Add multi-axis motion
- Create compound animations

### Aiming System
The aiming system is designed for position-based control:
- Set target world positions
- Smooth movement toward targets
- Future integration with combat systems

## Benefits

1. **Separation of Concerns**: Tentacle logic isolated from player/state logic
2. **Robust**: Comprehensive error handling and cleanup
3. **Future-Ready**: Built for expansion into combat/aiming systems
4. **Simple**: Easy to understand and modify
5. **Performant**: Efficient animation system