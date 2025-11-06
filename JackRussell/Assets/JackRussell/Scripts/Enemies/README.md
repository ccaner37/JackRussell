# Enemy Turret System Implementation

## Overview

This document provides a complete implementation of a turret enemy system for the JackRussell 3D Sonic-inspired game. The turret enemy detects players, tracks them, charges up attacks, and fires laser projectiles. The system is designed to be future-proof and easily extensible.

## Architecture

### Base Classes

1. **GameEntity** (`Assets/JackRussell/Scripts/Enemies/GameEntity.cs`)
   - Base class for all entities with health/damage mechanics
   - Provides shared functionality between Player and Enemy classes
   - Handles health management, damage, and death

2. **Enemy** (`Assets/JackRussell/Scripts/Enemies/Enemy.cs`)
   - Base class for all enemy entities
   - Inherits from GameEntity and implements HomingTarget
   - Provides common enemy behaviors and homing attack vulnerability

### Turret System Components

1. **TurretEnemy** (`Assets/JackRussell/Scripts/Enemies/TurretEnemy/TurretEnemy.cs`)
   - Main turret controller class
   - Manages state machine and all turret behaviors
   - Handles detection, targeting, and firing

2. **TurretStateBase** (`Assets/JackRussell/Scripts/Enemies/TurretEnemy/TurretStates/TurretStateBase.cs`)
   - Base class for all turret states
   - Provides common state functionality and helper methods

3. **Turret States** (in `Assets/JackRussell/Scripts/Enemies/TurretEnemy/TurretStates/`)
   - **TurretIdleState**: Turret is inactive, waiting for player detection
   - **TurretDetectingState**: Player detected, beginning targeting sequence
   - **TurretTargetingState**: Rotating to face player
   - **TurretPreparingState**: Charging up attack with glow effect
   - **TurretFiringState**: Firing laser projectile
   - **TurretCooldownState**: Recovery period after firing

4. **TurretProjectile** (`Assets/JackRussell/Scripts/Enemies/TurretEnemy/TurretProjectile.cs`)
   - Laser projectile that damages player
   - Travels in straight line with configurable speed and damage
   - Handles collision detection and impact effects

5. **TurretGlowEffect** (`Assets/JackRussell/Scripts/Enemies/TurretEnemy/TurretGlowEffect.cs`)
   - Manages material glow effect during charging phase
   - Uses DOTween for smooth animations
   - Manipulates "_Glow" material property

## State Machine Flow

```
Idle → Detecting → Targeting → Preparing → Firing → Cooldown → (back to Targeting or Idle)
```

- **Idle**: Turret waits for player detection
- **Detecting**: Player detected, brief detection phase
- **Targeting**: Turret rotates to face player
- **Preparing**: Turret charges up (glow effect increases)
- **Firing**: Turret fires laser projectile
- **Cooldown**: Turret cannot act for duration

## Setup Instructions

### 1. Create Turret Prefab

1. Create an empty GameObject named "TurretEnemy"
2. Add the following structure:
   ```
   TurretEnemy (GameObject)
   ├── Base (static body mesh)
   ├── Head (rotating part)
   │   └── FirePoint (where laser spawns)
   ├── DetectionRadius (trigger collider, optional)
   ├── TurretEnemy.cs
   ├── TurretGlowEffect.cs
   ├── Collider (for homing attacks)
   ├── AudioSource (for sound effects)
   └── Audio Sources (multiple for different sounds)
   ```

### 2. Configure Components

**TurretEnemy Component:**
- **Head Transform**: Assign the rotating head transform
- **Fire Point**: Assign the projectile spawn point
- **Glow Effect**: Assign TurretGlowEffect component
- **Laser Projectile Prefab**: Assign projectile prefab
- **Audio Source**: Assign main audio source

**Detection Settings:**
- **Detection Radius**: Set player detection range (default: 20f)
- **Player Layer Mask**: Set which layers to detect

**Combat Settings:**
- **Rotation Speed**: Degrees per second for head rotation (default: 90f)
- **Preparation Time**: Charging duration (default: 2f)
- **Cooldown Time**: Recovery duration (default: 3f)
- **Laser Damage**: Damage dealt to player (default: 20f)
- **Laser Speed**: Projectile speed (default: 30f)

**Visual Effects:**
- **Detection Effect Prefab**: Effect when player detected
- **Charging Effect Prefab**: Effect during charging
- **Firing Effect Prefab**: Effect when firing
- **Death Effect Prefab**: Effect when destroyed

**Audio:**
- Assign SoundType for each action (detection, targeting, charging, firing, death)

### 3. Create Laser Projectile Prefab

1. Create GameObject "TurretProjectile"
2. Add components:
   - **TurretProjectile.cs**
   - **TrailRenderer** (optional, for visual trail)
   - **ParticleSystem** (optional, for particle effects)
   - **Collider** (trigger, for collision detection)
   - **Rigidbody** (optional, for physics)

**TurretProjectile Component:**
- **Speed**: Projectile travel speed
- **Damage**: Damage to deal to player
- **Lifetime**: How long projectile exists
- **Collision Layer Mask**: What layers to collide with
- **Impact Effect Prefab**: Effect on hit

### 4. Configure Glow Effect

**TurretGlowEffect Component:**
- **Glow Material**: Material with "_Glow" property
- **Glow Property Name**: "_Glow" (or custom name)
- **Glow Curve**: Animation curve for glow intensity
- **Affected Renderers**: Renderers to apply glow material to

## Material Setup

### Glow Material

1. Create material for turret glow effect
2. Add float property named "_Glow"
3. Configure shader to use this property for emission/intensity
4. Assign to TurretGlowEffect component

### Layer Setup

Recommended layers:
- **Player**: Layer for player GameObject
- **Enemy**: Layer for enemy GameObjects  
- **Projectile**: Layer for projectiles
- **Default**: For environment/obstacles

## Integration with Existing Systems

### Player Pressure System

The turret integrates with the player's Pressure system:
- Laser projectiles call `player.SetPressure(player.Pressure - damage)`
- Damage amount is configurable per turret

### Homing Attack System

The turret is targetable by player homing attacks:
- Implements `HomingTarget` interface
- `TargetTransform` points to turret head
- `OnHomingHit` destroys turret with effects
- `IsActive` controls targetability

### Audio System

Uses existing AudioManager:
- SoundType enum for different sounds
- AudioSource components for playback
- Injection of AudioManager via VContainer

## Usage Examples

### Basic Turret Setup

```csharp
// Create turret with default settings
GameObject turretObj = new GameObject("Turret");
TurretEnemy turret = turretObj.AddComponent<TurretEnemy>();

// Configure via inspector or code
turret.DetectionRadius = 25f;
turret.LaserDamage = 30f;
turret.PreparationTime = 1.5f;
```

### Custom Turret Behavior

```csharp
// Create custom turret state
public class CustomTurretState : TurretStateBase
{
    public override string Name => "Custom";
    
    public override void LogicUpdate()
    {
        // Custom behavior logic
        if (someCondition)
        {
            ChangeState(new TurretIdleState(_turret, _stateMachine));
        }
    }
}
```

### Extending the System

#### New Enemy Types

1. Inherit from `Enemy` base class
2. Implement required abstract methods
3. Add custom behaviors and states
4. Follow same patterns as TurretEnemy

#### New Projectile Types

1. Inherit from `TurretProjectile` or create new class
2. Add custom behaviors (homing, splitting, etc.)
3. Implement collision detection
4. Configure in prefab

#### New Effect Types

1. Create effect components following existing patterns
2. Use DOTween for smooth animations
3. Hook into enemy state methods
4. Configure in inspector

## Performance Considerations

### Optimization Tips

1. **Object Pooling**: Use object pooling for projectiles
2. **LOD**: Implement level of detail for distant turrets
3. **Culling**: Disable updates when off-screen
4. **Physics**: Use efficient collision detection
5. **Audio**: Limit concurrent audio sources

### Memory Management

1. **Destroy Effects**: Clean up effect GameObjects
2. **Stop Coroutines**: Ensure coroutines are stopped
3. **Unsubscribe Events**: Remove event listeners
4. **Null References**: Clear references in OnDestroy

## Troubleshooting

### Common Issues

1. **Turret Not Detecting Player**
   - Check layer mask configuration
   - Verify detection radius
   - Ensure player has correct layer

2. **Glow Effect Not Working**
   - Verify material has "_Glow" property
   - Check TurretGlowEffect references
   - Ensure renderer references are correct

3. **Projectile Not Firing**
   - Check fire point transform
   - Verify projectile prefab
   - Ensure TurretProjectile component exists

4. **State Machine Not Working**
   - Check state initialization in Awake
   - Verify state transitions
   - Debug with state name logging

### Debug Tools

1. **Gizmos**: Enable detection radius visualization
2. **State Logging**: Add Debug.Log to state methods
3. **Visual Indicators**: Add debug effects for states
4. **Inspector**: Use custom editors for better visualization

## Future Enhancements

### Planned Features

1. **Advanced AI**: Behavior trees for complex decisions
2. **Network Support**: Multiplayer synchronization
3. **Customization**: Modular weapon systems
4. **Performance**: Advanced optimization techniques
5. **Tools**: Editor tools for level design

### Extension Points

The system is designed to be easily extended:

- **New Enemy Types**: Follow Enemy base class pattern
- **New States**: Inherit from TurretStateBase
- **New Effects**: Create components following existing patterns
- **New Projectiles**: Extend or replace TurretProjectile
- **New Behaviors**: Add to state machine flow

This architecture provides a solid foundation for implementing various enemy types while maintaining consistency and performance.