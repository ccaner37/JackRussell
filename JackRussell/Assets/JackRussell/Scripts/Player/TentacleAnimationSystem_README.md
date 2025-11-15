# Tentacle Idle Animation System - Camera Responsive Version

## Overview
This system provides a robust, future-proof solution for tentacle idle animations with clean separation of concerns. The tentacle follows a target transform that gently sways while idle, creating a hover-like effect that responds to camera direction. **Both idle animation and camera responsiveness work together harmoniously without conflicts.**

## Features
- ✅ **Camera-Responsive Idle Animation**: Tentacle moves based on camera direction
- ✅ **DOTween-based smooth animations**
- ✅ **Position-based aiming system** (future use)
- ✅ **No Animation Conflicts**: Idle sway + camera responsiveness work together
- ✅ **Proper Left/Right Detection**: Tentacle can appear on both shoulders
- ✅ **Configurable animation parameters**
- ✅ **Clean separation of concerns**
- ✅ **Comprehensive debugging tools**

## Components

### TentacleController.cs
- **Purpose**: Manages tentacle idle animations and future aiming functionality
- **Key Features**:
  - ✅ **Harmonious camera-responsive idle sway animation**
  - ✅ **Proper left/right shoulder detection using cross-product mathematics**
  - DOTween-based smooth animations
  - Position-based aiming (ready for enemy targeting)
  - Configurable animation parameters
  - Clean start/stop interface
  - Gizmo visualization for debugging

### Integration Points

#### Player.cs
- Added `_tentacleController` serialized field
- Added `TentacleController` accessor property
- Added `_cameraController` and accessor for camera access
- Calls `UpdateController()` in Player's Update method

#### IdleState.cs  
- Starts tentacle idle animation on Enter
- Stops tentacle idle animation on Exit

## Setup Instructions

1. **Attach TentacleController**:
   - Add `TentacleController` component to your Player object
   - Assign your tentacle target transform to `_tentacleTarget`
   - Assign the Player component to `_player` field

2. **Configure Animation Parameters** (in Inspector):
   - `_idleAmplitude`: How much the tentacle sways (default: 0.1)
   - `_idleFrequency`: How fast it sways (default: 1.5)
   - `_idleAxis`: Sway direction vector (default: 0, 0.1, 0.05)

3. **Camera-Responsive Settings**:
   - `_isCameraResponsive`: Enable/disable camera responsiveness (default: true)
   - `_cameraResponsiveStrength`: How much camera affects animation (default: 0.7)
   - `_cameraLerpSpeed`: How smoothly it follows camera direction (default: 5)

4. **Assign in Player**:
   - In Player inspector, drag your TentacleController component to the `_tentacleController` field
   - Assign CinemachineCameraController to `_cameraController` field

## Camera-Responsive Behavior

### How It Works
The tentacle uses cross-product mathematics to determine camera position relative to the player:

```csharp
// Calculate left/right position using cross product
float rightLeftValue = Vector3.Cross(playerForward, horizontalPlayerToCamera.normalized).y;

// Positive = camera RIGHT of player → tentacle moves RIGHT (positive X)
// Negative = camera LEFT of player → tentacle moves LEFT (negative X)
```

### Correct Left/Right Movement
- **Camera on player's LEFT side** → tentacle appears on left shoulder (negative X)
- **Camera on player's RIGHT side** → tentacle appears on right shoulder (positive X)
- **Camera in FRONT** → tentacle centered (near zero X)
- **Camera BEHIND** → tentacle centered (near zero X)

### Distance-Based Responsiveness
- **Close camera** (1-2 units) → subtle movement (25-50% strength)
- **Medium camera** (3-4 units) → moderate movement (50-80% strength)
- **Far camera** (5+ units) → maximum movement (100% strength)
- **Threshold**: Only moves when camera is meaningfully positioned (>0.1f threshold)

### Mathematical Precision
```csharp
// Lateral movement calculation
float lateralStrength = Mathf.Clamp01(Mathf.Abs(distance) / 5f);
float horizontalOffset = rightLeftValue * strength * range * lateralStrength * 0.15f;

// Vertical movement based on camera height
float heightStrength = Mathf.Clamp01(Mathf.Abs(heightDifference) / 3f);
float verticalOffset = Mathf.Sign(upDownValue) * strength * range * heightStrength * 0.08f;
```

## Anti-Conflict Design

### Problem Solved
❌ **Old Issue**: Camera responsiveness would override/idle animation, causing jarring movements
❌ **Old Issue**: Left/right detection failed, tentacle never moved to negative X
✅ **Solution**: Position tracking system that combines both animations harmoniously
✅ **Solution**: Cross-product mathematics for proper directional detection

### How It Works
1. **Internal Position Tracking**: DOTween controls internal `_currentIdlePosition` (not transform directly)
2. **Combined Application**: `ApplyCombinedPosition()` adds camera offset to idle position
3. **Smooth Transitions**: Both systems use consistent lerp speeds for smooth movement
4. **No Conflicts**: Camera responsiveness enhances, doesn't replace, the idle animation

### Code Structure
```csharp
// DOTween animates internal position
DOTween.To(() => _currentIdlePosition, 
           x => _currentIdlePosition = x, 
           endPos, duration)
    .OnUpdate(() => {
        // Apply combined position
        ApplyCombinedPosition();
    });

// Combined position application
Vector3 targetPosition = _currentIdlePosition + _cameraBasedOffset;
_tentacleTarget.localPosition = Vector3.Lerp(lastAppliedPosition, targetPosition, lerpSpeed);
```

## Usage

### Current Functionality
- **Automatic**: Idle animation starts when player enters IdleState
- **Automatic**: Animation stops when player exits IdleState
- **Manual Control**: Use `_player.TentacleController.StartIdleAnimation()` / `StopIdleAnimation()`

### Camera Responsiveness Control
```csharp
// Configure camera responsiveness
_player.TentacleController.ConfigureCameraResponsiveness(
    enable: true,           // Enable/disable
    strength: 0.8f,         // How much camera affects it
    lerpSpeed: 6f,          // How smooth the transition
    range: 1.2f             // Movement range
);
```

### Future Aiming (Planned)
```csharp
// Start aiming at a position
_player.TentacleController.StartAiming(targetPosition);

// Update aim position during combat
_player.TentacleController.UpdateAimPosition(newTargetPosition);

// Stop aiming and return to idle
_player.TentacleController.StopAiming(true);
```

## Advanced Configuration

### Animation Behavior
Modify `CreateIdleSwayTween()` for different patterns:
- Change easing functions: `Ease.InOutSine`, `Ease.InOutQuad`, etc.
- Adjust loop types: `LoopType.Yoyo`, `LoopType.Restart`, `LoopType.Incremental`
- Create compound animations with `Sequence`

### Camera Responsiveness
The system uses cross-product mathematics for precise directional detection:
- Cross product for left/right detection
- Dot product for forward/backward detection
- Height difference for up/down movement
- Distance scaling for responsive intensity

## Debugging Features

### Enhanced Gizmo Visualization
- **Cyan**: Idle sway area (base range)
- **Blue**: Current idle position (from DOTween)
- **Red**: Current aim position (when aiming)
- **Green**: Camera-based offset
- **White**: Player forward direction
- **Yellow**: Player-to-camera vector
- **Magenta**: Right-side camera indicator
- **Cyan**: Left-side camera indicator
- **Gray→Yellow**: Distance-based strength visualization

### Runtime Inspection
Public properties for debugging:
- `IsIdleAnimating`
- `IsAiming`
- `IsCameraResponsive`
- `CurrentAimPosition`
- `TentacleTarget`

## Benefits

### Robust Design
- ✅ **No Animation Conflicts**: Both systems work harmoniously
- ✅ **Proper Left/Right Detection**: Tentacle can appear on both shoulders
- ✅ **Distance-Based Responsiveness**: More intuitive behavior
- Null safety checks throughout
- Proper cleanup of DOTween animations
- Graceful handling of missing references
- Editor validation for reasonable parameter values

### Camera-Responsive
- Makes the tentacle feel alive and interactive
- Responds naturally to player camera movement
- Cross-product mathematics for precise directional detection
- Smooth interpolation prevents jarring transitions
- **Works seamlessly with idle animation**

### Future-Proof
- Position-based aiming (not rotation)
- Configurable animation parameters
- Separate controller for tentacle logic
- Easy integration with state machine

### Performance
- Lightweight DOTween animations
- Efficient vector math
- Update only when needed

## Troubleshooting

### No Left Movement
1. Check that `_player` reference is assigned
2. Verify camera can move to player's left side
3. Ensure `_cameraResponsiveStrength` > 0
4. Monitor cross-product value in Gizmos (should be negative for left)

### No Camera Response
1. Check `_isCameraResponsive` is enabled
2. Verify `_player` reference is assigned
3. Ensure Camera.main exists in scene
4. Check `_cameraResponsiveStrength` > 0

### Jarring Movement
1. Increase `_cameraLerpSpeed` for smoother transitions
2. Reduce `_cameraResponsiveStrength` for subtler effect
3. Ensure consistent lerp speeds between systems

### Tentacle Not Moving
1. Verify `_tentacleTarget` is assigned
2. Check IK rig is properly configured
3. Ensure IdleState is active and tentacle animation is started
4. Monitor `_currentIdlePosition` and `_cameraBasedOffset` in debug

### Animation Conflicts
1. Check that `ApplyCombinedPosition()` is being called
2. Verify `_lastAppliedPosition` is tracking correctly
3. Ensure both systems use compatible lerp speeds
4. Monitor Gizmos to see both idle and camera components

### Debug the Left/Right Calculation
Use the enhanced Gizmos to visualize:
- **White arrow**: Player forward direction
- **Yellow line**: Player-to-camera vector
- **Magenta/Cyan arrows**: Left/right detection indicators
- **Distance circle**: Camera distance visualization

This enhanced system ensures your tentacle feels alive and responsive with proper left/right movement while maintaining smooth, natural idle animation behavior!