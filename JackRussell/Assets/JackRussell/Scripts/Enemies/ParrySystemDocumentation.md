# Parry Attack System Documentation

## Overview

The Parry Attack System is a combat mechanic that allows the player to instantly defeat enemies during their vulnerable preparation phases. This system adds a high-risk, high-reward gameplay element that rewards precise timing and situational awareness.

## Architecture

### Core Components

#### 1. IParryable Interface
- **Location**: `Assets/JackRussell/Scripts/Enemies/IParryable.cs`
- **Purpose**: Defines the contract for enemies that can be parried
- **Key Properties**:
  - `IsInParryWindow`: Indicates if the enemy is currently vulnerable to parry
  - `ParryTargetTransform`: Transform where player should teleport for parry
- **Key Methods**:
  - `OnParried(Player player)`: Called when player successfully parries the enemy

#### 2. ParryAttackState
- **Location**: `Assets/JackRussell/Scripts/StateMachine/Action/ParryAttackState.cs`
- **Purpose**: Handles the player's parry attack execution
- **Key Features**:
  - Teleports player to enemy position
  - Triggers instant kill on enemy
  - Plays visual and audio effects
  - Manages timing and state transitions

#### 3. ParryExitState
- **Location**: `Assets/JackRussell/Scripts/StateMachine/Action/ParryExitState.cs`
- **Purpose**: Handles recovery phase after successful parry
- **Key Features**:
  - Recovery animation and effects
  - Smooth transition back to ActionNoneState
  - Applies gentle deceleration

#### 4. ActionNoneState Integration
- **Location**: `Assets/JackRussell/Scripts/StateMachine/Action/ActionNoneState.cs`
- **Purpose**: Detects parry opportunities and initiates parry attacks
- **Key Features**:
  - Checks for parryable enemies in range
  - Prioritizes parry over regular attacks when available
  - Uses existing Attack input (no new input required)

## Implementation Details

### Parry Detection Logic

1. **Range Check**: System scans for enemies within 15-unit radius
2. **Parry Window Check**: Validates if enemy is in vulnerable state
3. **Target Selection**: Chooses nearest valid parry target
4. **State Transition**: Enters ParryAttackState if valid target found

### Parry Execution Sequence

1. **Initial Phase** (0.15s):
   - Player dashes toward target at high speed
   - Visual effects enabled (smoke trails)
   - Audio feedback played

2. **Teleport Phase** (0.15s):
   - Instant teleport to enemy position
   - Enemy's `OnParried()` method called
   - Camera shake and impact effects
   - Player bounce-back applied

3. **Recovery Phase** (0.4s):
   - Controlled by ParryExitState
   - Gentle deceleration applied
   - Smooth return to normal state

### Enemy Integration

#### TurretEnemy Parry Implementation

The TurretEnemy implements IParryable through its state machine:

```csharp
public class TurretEnemy : Enemy, IParryable
{
    public bool IsInParryWindow => _currentState is TurretPreparingState;
    public Transform ParryTargetTransform => transform;
    
    public void OnParried(Player player)
    {
        // Instant death logic
        TakeDamage(MaxHealth);
        
        // Visual effects
        CreateDeathEffects();
        
        // State transition
        ChangeState(new TurretDeathState(this, _stateMachine));
    }
}
```

#### Parry Window in TurretPreparingState

```csharp
public class TurretPreparingState : TurretStateBase
{
    private float _parryWindowStart = 0.3f; // Start parry window after 30% of preparation
    private float _parryWindowEnd = 0.8f;   // End parry window at 80% of preparation
    
    public bool IsInParryWindow => 
        _timer >= _preparationTime * _parryWindowStart && 
        _timer <= _preparationTime * _parryWindowEnd;
}
```

## Visual and Audio Feedback

### Player Effects
- **Smoke Trails**: Enabled during parry dash and recovery
- **Particle Systems**: Impact effects at target position
- **Camera Shake**: Intensified impact feedback (2.0 magnitude, 0.5s duration)
- **Audio Cues**: 
  - Start: HomingAttackStart sound
  - Impact: Kick sound
  - Recovery: Jump sound

### Enemy Effects
- **Instant Death**: Enemy destroyed immediately on successful parry
- **Death Animation**: Optional death state transition
- **Particle Effects**: Enemy-specific death effects

## Usage Guidelines

### For Players
1. **Timing**: Press Attack when enemy is glowing/preparing (typically during middle 50% of preparation)
2. **Range**: Must be within 15 units of target enemy
3. **Priority**: Parry takes precedence over regular attacks when available
4. **Recovery**: Brief recovery period after parry before normal action resumes

### For Developers
1. **Implement IParryable**: Add interface to enemy classes that should be parryable
2. **Define Parry Window**: Set specific timing windows in enemy states
3. **Handle OnParried**: Implement instant death or custom parry response
4. **Visual Feedback**: Add appropriate effects for parry success

## Configuration

### Tunable Parameters
- **Parry Range**: 15 units (configurable in ActionNoneState)
- **Teleport Speed**: 50 units/second (configurable in ParryAttackState)
- **Recovery Duration**: 0.4 seconds (configurable in ParryExitState)
- **Parry Window**: Enemy-specific (typically 30-80% of preparation time)

### Layer and Tag Requirements
- **Parryable Enemies**: Must be on layers included in Player's HomingMask
- **Colliders**: Enemy must have collider for overlap detection
- **Transform**: Enemy must provide valid ParryTargetTransform

## Performance Considerations

### Optimization
- **OverlapSphere**: Uses efficient physics overlap detection
- **Layer Filtering**: Respects HomingMask for targeted detection
- **Coroutine Management**: Proper cleanup of effect coroutines
- **State Machine**: Efficient state transitions prevent redundant checks

### Memory Management
- **Effect Cleanup**: Auto-destroy temporary effect objects
- **Coroutine Cleanup**: Proper stopping and cleanup on state exit
- **Reference Management**: Clear target references on state exit

## Future Enhancements

### Potential Extensions
1. **Parry Chains**: Allow multiple parries in quick succession
2. **Parry Upgrades**: Enhanced effects or damage based on player upgrades
3. **Enemy Variations**: Different parry responses per enemy type
4. **Visual Customization**: Player-customizable parry effects
5. **Audio Variation**: Context-aware parry sound selection

### Integration Points
1. **UI Integration**: Parry availability indicators
2. **Tutorial System**: Interactive parry timing tutorials
3. **Achievement System**: Parry-specific achievements
4. **Statistics Tracking**: Parry success rates and counters

## Troubleshooting

### Common Issues
1. **Parry Not Triggering**: Check layer masks and collider setup
2. **Wrong Target**: Verify ParryTargetTransform is correctly assigned
3. **Timing Issues**: Adjust parry window percentages in enemy states
4. **Visual Glitches**: Ensure proper effect cleanup in state exits

### Debug Tools
- **Debug Logging**: Add debug logs for parry detection
- **Visual Debuggers**: Use Gizmos to show parry range
- **State Indicators**: Visual feedback for current enemy states
- **Timing Displays**: On-screen parry window indicators

## Conclusion

The Parry Attack System provides a robust, extensible framework for implementing high-risk, high-reward combat mechanics. It integrates seamlessly with the existing state machine architecture while maintaining clean separation of concerns and providing ample customization options for different enemy types and gameplay scenarios.