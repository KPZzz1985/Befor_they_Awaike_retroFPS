using UnityEngine;

/// <summary>
/// Enumerates the types of voice events used by the VoiceSystem.
/// </summary>
public enum VoiceEventType
{
    /// <summary>Ability to throw grenade.</summary>
    ThrowAbility,

    /// <summary>Burst fire ability.</summary>
    BurstFire,

    /// <summary>Player has been seen.</summary>
    PlayerSeen,
    /// <summary>Enemy lost sight of the player.</summary>
    PlayerLost,

    /// <summary>Character started dashing.</summary>
    Dashing,

    /// <summary>Health dropped below low threshold.</summary>
    HealthLow,

    /// <summary>Health dropped to critical threshold.</summary>
    HealthCritical,

    /// <summary>Player has died.</summary>
    PlayerDie,

    /// <summary>Enemy has died.</summary>
    EnemyDie
}