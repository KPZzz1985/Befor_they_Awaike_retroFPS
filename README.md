# Retro Shooter AI & Strategic System

This README summarizes the recent enhancements to the enemy AI, strategic patterns, and ability system in the retro shooter project.

## Key Features Implemented

1. **Dynamic Cover Generation**
   - `CoverFormation` now generates cover points every frame around the player (no longer tied to movement threshold).
   - `StrategicSystem` broadcasts updated points via `OnCoverPointsUpdated` event to `EnemyMovement2`.

2. **Strategic Tick Loop**
   - Central `StrategicSystem` drives AI decisions each frame: cover updates, intruder alerts, pattern triggers, and activation logic.
   - Deprecated per-enemy raycasts; AI subscribes to precomputed cover points.

3. **Ambush Pattern (L1AmbushPattern)**
   - Implemented `IBehaviorPattern` interface and `L1AmbushPattern`: L1Soldier pairs advance to random cover points, sit in ambush, then repeat.
   - Uses UniTask for asynchronous, cancelable pattern execution.

4. **Ability System (Grenade Throws & Future Drones)**
   - Mana-based ability for L1Soldier: regenerates from 0→100 over `manaRechargeTime` seconds.
   - Strategic loop (`GrenadeLoop`) checks every 2.5–5 s per soldier, with a configurable chance (`grenadeThrowChance`) to throw.
   - Throws occur only if player within `grenadeThrowDistance` meters.
   - Round-robin/random selection prevents the same soldier from throwing twice in a row.
   - `ThrowAbility` disables movement (`EnemyMovement2`) and shooting (`EnemyWeapon`), triggers a "Throw" animation, waits `grenadeThrowDelay` seconds, instantiates a grenade prefab (spawns from a child named `GrenadeSpawnPoint` if present), applies physics, then resumes AI.
   - If a soldier is killed during the throw delay, the grenade is dropped immediately at the spawn point and falls down without applying throw force.

5. **Visual Debugging**
   - `OnGUI` in `StrategicSystem` displays current `Mana`, `Reload rate`, and `Grenade cost` on-screen for rapid tuning.

6. **Burst Fire Ability**
   - Added new Burst Fire ability: periodically selects an L1Soldier who sees the player to fire a rapid burst of `shotsPerBurst` bullets at an increased rate.
   - Uses configurable fields: `burstFireManaCost`, `burstFireChance`, `burstCheckIntervalMin`, `burstCheckIntervalMax`.
   - Guards ensure no overlap between burst fire and grenade throw.

7. **Weapon Switching Improvements**
   - Reset reload state on weapon change, preventing aborted reloads from blocking firing.
   - Prevent switching to the already active weapon and support scripts in child objects via `GetComponentInChildren`.

8. **3D Audio for Shooting & Explosions**
   - Added spatialized shooting audio in `EnemyWeapon` and `PM_Shooting` with configurable `shootClip`, `shootVolume`, `shootMinDistance`, and `shootMaxDistance`.
   - Added explosion sound in `BlastProjectile` with `explosionClip`, `explosionVolume`, `explosionMinDistance`, and `explosionMaxDistance`.

9. **Centralized Ammo Inventory**
   - Created `PlayerInventory` component to manage both clip and reserve ammo per weapon via `AmmoEntry` entries.
   - `AmmoEntry` holds a `PM_Shooting shooter`, `currentAmmo`, `maxAmmo`, `reserveAmmo`, and `reserveLimit`.
   - APIs: `AddAmmo`, `GetCurrentAmmo`, `GetMaxAmmo`, `AddReserve`, `RemoveReserve`, and `GetReserveAmmo`.
   - Fields auto-populate from assigned `shooter` in `Awake()` (manual assignment in inspector).

10. **Pickable Items & Ammo Pickups**
    - `PickableItem` script supports types: Health, Ammo, Shield, UpgradeCore, Key.
    - Configurable `amount` and optional `weaponIndex` (-1 for all weapons).
    - For Ammo items, calls `PlayerInventory.AddReserve(...)` and plays `pickupSound` at `pickupVolume`.

11. **HUD Integration**
    - `HUDController` displays health and ammo as `clip/reserve` using digit sprite arrays.
    - Set references: `PlayerHealth`, `WeaponSwitcher`, `PlayerInventory`, `clipDigits`, `slashImage`, `reserveDigits`, and `ammoIcon`.
    - Automatically hides UI when no weapon is active.

12. **Weapon Refactor for Inventory**
    - `PM_Shooting` no longer stores local `currentAmmo`; reads/writes from `PlayerInventory`.
    - Weapon determines its `weaponIndex` on startup and uses `inventory` calls to decrement on shot and refill on reload.
    - Manual reload (R) and burst reload only allowed when `reserveAmmo > 0`.

13. **Shotgun Multi-Pellet Fix**
    - `ShootPellet()` updated to loop `pelletCount` times, firing multiple raycasts with spread, restoring proper shotgun behavior.

14. **Blast Projectile Flight Audio**
    - `BlastProjectile` now plays looped `flightClip` via a second `AudioSource` during flight, stopping it on explosion before playing `explosionClip`.
    - Configurable `flightVolume`, `flightClip`, and spatial settings align with explosion audio.

15. **Special Death System** ✨**NEW**
    - Implemented a comprehensive special death system allowing diverse and dramatic enemy death sequences based on damage type.
    - **Core Architecture**:
      - `isHandlingDeath` flag: temporary buffer between damage and final death, allowing special death sequences
      - `IDeathHandler` interface: defines contract for different special death types
      - `DeathHandlerManager`: central component that selects and executes appropriate death handlers
      - `DamageType` enum: categorizes damage (Generic, Blast, Fire, Acid, Electric, DarkFire, Vampire, RliehGliphs)
    
    - **Fire Death Implementation** (first special death type):
      - **Duration**: Random 4-6 seconds of `isHandlingDeath` state
      - **Animation Logic**: Sets `isBurning=true` in animator, alternates between pause (`Speed=0` for 1-1.5s) and movement (`Speed=5-6` to random NavMesh points)
      - **Final Transition**: Uses final movement speed to determine death animation (`RndState=5/9` if moving, standard ragdoll if stationary)
      - **Visual Effects**: Spawns 3-5 random fire effect prefabs on each rigidbody with `Damageable` script during special death
      - **Ground Fire Trails**: When enemy runs (speed > 1), spawns fire effects on ground using raycast positioning
      - **Permanent Texture Changes**: Applies "burned" textures to all `Damageable` components that remain after death
    
    - **Smart Impulse Logic**:
      - **Generic Death** (normal weapons): Always applies ragdoll impulse as before
      - **Special Death**: Only applies impulse if enemy was moving at transition moment
      - Uses `hadSpecialDeath` flag to distinguish between death types
    
    - **System Integration**:
      - `StrategicSystem` ignores enemies with `isHandlingDeath=true` (no forced patterns/abilities)
      - `EnemyWeapon` disabled during special death to prevent shooting
      - Decal system disabled during `isHandlingDeath` to prevent blood spam
      - NavMeshAgent remains active for special death movement, disabled only at final death
    
    - **Configuration** (per enemy via Inspector):
      - `handleDeathAttachments[]`: Maps prefabs to damage types with configurable lifetimes
      - `specialDeathTextures[]`: Maps textures to damage types for permanent visual changes
      - `minAttachmentsPerRigidbody` / `maxAttachmentsPerRigidbody`: Controls effect density

## Unity Package & Dependencies

- **UniTask**: asynchronous toolset installed via OpenUPM registry (package `com.cysharp.unitask`).
- Ensure your **Package Manager → Project Settings → Scoped Registries** includes:
  ```json
  {
    "name": "openupm",
    "url": "https://package.openupm.com",
    "scopes": ["com.cysharp"]
  }
  ```
- Add `com.cysharp.unitask` version `2.3.3` under `Window → Package Manager`.

## Setup & Configuration

1. **StrategicSystem**
   - Attach `StrategicSystem` component to a central GameObject (e.g. `GameManager`).
   - Configure serialized fields:
     - Cover & pattern timing: `minPatternDelay`, `maxPatternDelay`, `ambushRadius`, `approachDistanceMin/Max`, `ambushWaitDuration`, `groupSpacingRadius`, `ambushGroupSize`.
     - Ability settings: assign a **grenadePrefab**, set `grenadeManaCost`, `manaRechargeTime`, `maxMana`, `grenadeThrowDelay`, `grenadeThrowChance`, `grenadeCheckIntervalMin/Max`, `grenadeThrowDistance`.

2. **L1Soldier Prefab**
   - Tag or name your AI units as **L1Soldier** (or prefix with "L1Soldier").
   - Add a child GameObject named `GrenadeSpawnPoint` at the desired throw origin.
   - Ensure components present: `EnemyMovement2`, `EnemyWeapon`, `LookRegistrator`, `NavMeshAgent`, `Animator` (with a `Throw` trigger).

3. **CoverFormation**
   - Place `CoverFormation` in the scene, assign the player `Transform` and `PlayerTargetSystem` reference.

4. **Special Death System Setup**
   - **EnemyHealth Configuration**:
     - Add `DeathAttachment[]` entries mapping prefab + damage type + lifetime (min/max seconds)
     - Add `DamageTypeTexture[]` entries mapping texture + damage type for permanent visual changes
     - Configure `minAttachmentsPerRigidbody` and `maxAttachmentsPerRigidbody` (recommended: 3-5)
   
   - **Damageable Configuration**:
     - Ensure all enemy parts have `Damageable` script for texture application
     - Configure `specialDeathTextures[]` array with texture mappings per damage type
   
   - **FireDeathHandler Settings** (configured automatically via code):
     - Burn duration: 4-6 seconds random
     - Movement pattern: pause (1-1.5s) → move to random point (speed 5-6) → repeat
     - Ground trail spawn: every 0.15s when speed > 1, min distance 0.5m between trails
   
   - **Effect Prefabs**:
     - Fire effects should have `FireEffectZone` script or similar with lifetime management
     - Ground fire effects spawn using same raycast logic as decal system (Ground tag required)

5. **Testing & Playmode**
   - Run the scene; watch on-screen debug for Mana and throw events.
   - Verify that L1Soldier units alternate grenade throws, stop movement/shooting for the throw animation, then resume.

6. **PSOneShaderGI Parameter Auto-Apply**
   - Use the **Tools → Set PSOneShaderGI Params for Materials** window to adjust shader parameters (_Tiling, _PixelationAmount, _ColorPrecision, _JitterAmount, _TextureFPS, _TextureJitterAmplitude).
   - Settings are saved in `EditorPrefs` and now automatically applied to all materials when entering Play Mode, so manual application is no longer required.

## Next Steps

- **Special Death Expansion**:
  - Implement additional death handlers: `AcidDeathHandler`, `ElectricDeathHandler`, `VampireDeathHandler`
  - Add corresponding visual effects and textures for each damage type
  - Create weapon-specific death sequences (e.g., electric weapons cause twitching/sparking)
- Implement **drone ability** similar to grenade but with flight and delayed explosion.
- Add more **behavior patterns** for other enemy types.
- Polish AI transitions: between ambush, cover, chase, and retreat.
- Integrate weapon animations and visual effects for abilities.
- Ensure movement animations update during all AI states by calling `UpdateAnimatorMovement()` early in `LateUpdate()`, so retreat/ambush/push states no longer block animation updates.

## Recent Changes Log

### Version: Special Death System Implementation
**Date**: January 2025
**Major Features Added**:
- Complete special death system architecture with `isHandlingDeath` buffer state
- Fire death handler with burning animations, movement patterns, and visual effects
- Smart ragdoll impulse logic distinguishing normal vs special deaths  
- Permanent texture application system for visual damage persistence
- Ground fire trail spawning system with proper positioning
- Gradual effect spawning over time (0.75s) with configurable attachment density
- Integration with existing systems (StrategicSystem, Decal System, NavMesh)

**Files Modified**:
- `Assets/Scripts/EnemyAI/EnemyHealth.cs` - Core death management and effect spawning
- `Assets/Scripts/EnemyAI/StrategicSystem.cs` - Updated to ignore special death enemies  
- `Assets/Scripts/EnemyAI/Damageable.cs` - Added texture mapping and application
- **New Files Created**:
  - `Assets/Scripts/EnemyAI/IDeathHandler.cs` - Death handler interface
  - `Assets/Scripts/EnemyAI/DeathHandlerManager.cs` - Death system coordinator
  - `Assets/Scripts/EnemyAI/FireDeathHandler.cs` - Fire-specific death implementation
  - `Assets/Scripts/EnemyAI/DeathAttachment.cs` - Attachment data structure

---

### Version: HUD, Fog & Inventory Enhancements
**Date**: August 2025
**Highlights**:
- **Fog & UI Redesign**: убран динамический Fog в `PlayerHealth` и заменён на UI плашки (`damageOverlay`, `deathOverlay`) в `HUDController` с плавным появлением через UniTask.
- **HUDController Updates**: добавлены поля `damageOverlay` и `deathOverlay`, методы `ShowDamageFlash()` и `ShowDeathOverlay()`, `InitializeOverlays()` растягивает плашки на весь экран и выводит поверх всех UI.
- **PlayerHealth Refactor**: убраны операции SetColor и корутины Fog; вместо них вызовы `TriggerDamageUIEffect()` и `TriggerDeathUIEffect()`.
- **SimpleFogEffect**: повторно включён на старте для работы с декалями, снимает ограничения Deferred.
- **PickableItem Simplification**: логика подбора патронов перенесена на использование `WeaponSwitcher.CurrentWeaponIndex`, убраны сложные проверки и ручные поля.
- **PlayerInventory Overhaul**: AmmoEntry теперь полностью генерируется из списка `WeaponSwitcher.weapons` в `Awake()`, добавлен метод `AddReserveReturn()` для атомарного обновления и получения нового значения.
- **PM_Shooting Fix**: перенёс вычисление `weaponIndex` в `Awake()` для корректной работы даже при неактивном оружии.
- **WeaponSwitcher Exposure**: новое свойство `CurrentWeaponIndex` публично возвращает индекс активного оружия для подбора.

*Updated by AI assistant to reflect HUD, Fog & Inventory system improvements.*
---