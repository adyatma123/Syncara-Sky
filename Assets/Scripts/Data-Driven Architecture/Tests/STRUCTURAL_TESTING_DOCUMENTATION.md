# Structural Testing Suite untuk Data-Driven Architecture

## Overview

Dokumen ini berisi dokumentasi structural testing untuk implementasi data-driven architecture pada project Syncara-Sky. Fokus pengujian adalah bagaimana sistem weapon, payload, vehicle, selector, dan gameplay integration menggunakan ScriptableObject sebagai sumber data utama, bukan hard-coded factory method.

Pengujian dilakukan berdasarkan skenario D1-D10 dari dokumen ArsitekturalTesting.docx, dengan cakupan utama pada:

- `Guns` ScriptableObject
- `Payload` ScriptableObject
- `Vehicles` ScriptableObject
- `GunSelector`
- `PayloadSelector`
- `GameSelectionManager`
- `Gun`
- `PayloadManager`
- Integrasi loadout ke gameplay

## Test Coverage

### 1. Gun Data Structural Tests

**Files Tested:**
- `Assets/Scripts/Scriptable Object's Script/Guns.cs`
- `Assets/Scripts/Player/Gun.cs`
- `Assets/Scripts/Selector/GunSelector.cs`

#### Objects Tested:
- `Guns` ScriptableObject
- Gun parameter validation
- Gun selection and application flow
- Runtime gun behavior in `Gun`

#### Test Cases:
1. **ScriptableObject Structure**: Verify gun data is separated from runtime logic.
2. **Parameter Validation**: Verify damage, fire rate, bullet speed, heat rate, tier, and price are clamped to valid ranges.
3. **Runtime Application**: Verify selected gun data can be applied through `ApplyGunProperties()`.
4. **Prefab Reference Handling**: Verify missing bullet prefab is detected before instantiation.
5. **Tier Filtering**: Verify guns are filtered based on selected vehicle tier.
6. **Projectile Limit**: Verify active projectile count is limited.
7. **Gun Stage Limit**: Verify each firing stage has a maximum active gun count.
8. **Spawn Point Safety**: Verify null or inactive spawn points are skipped.
9. **Sound Key Safety**: Verify missing shoot sound key does not force playback.
10. **Reuse Support**: Verify one `Gun` component can use different `Guns` data assets.

### 2. Payload Data Structural Tests

**Files Tested:**
- `Assets/Scripts/Scriptable Object's Script/Payload.cs`
- `Assets/Scripts/Managers/PayloadManager.cs`
- `Assets/Scripts/Selector/PayloadSelector.cs`

#### Objects Tested:
- `Payload` ScriptableObject
- Payload slot data
- Payload selection flow
- Runtime payload firing flow

#### Test Cases:
1. **ScriptableObject Structure**: Verify payload properties are defined as editable data.
2. **Parameter Validation**: Verify damage, speed, reload time, lifetime, ammo, tier, price, radius, and homing angle are validated.
3. **Tier Filtering**: Verify payloads are filtered based on selected vehicle tier.
4. **Slot Count Adaptation**: Verify payload slot UI follows selected vehicle payload slot count.
5. **Duplicate Payload Merge**: Verify identical payload references are grouped into one processed slot.
6. **Ammo Calculation**: Verify ammo is calculated from payload max ammo and hardpoint count.
7. **Prefab Reference Handling**: Verify missing payload prefab is detected before instantiation.
8. **Hardpoint Safety**: Verify null hardpoints are skipped.
9. **Reload Safety**: Verify reload time is clamped before coroutine wait.
10. **Runtime Reinitialization**: Verify loadout is reprocessed when a slot changes.

### 3. Vehicle Data Structural Tests

**Files Tested:**
- `Assets/Scripts/Scriptable Object's Script/Vehicles.cs`
- `Assets/Scripts/SelectionMenu/VhcChgr.cs`
- `Assets/Scripts/SelectionMenu/VhcDis.cs`
- `Assets/Scripts/Player/PlayerController.cs`

#### Objects Tested:
- `Vehicles` ScriptableObject
- Vehicle selection flow
- Vehicle prefab assignment
- Runtime player aircraft instantiation

#### Test Cases:
1. **Vehicle Data Structure**: Verify aircraft data is stored in `Vehicles`.
2. **Prefab-Based Creation**: Verify selected vehicle prefab is used for gameplay instantiation.
3. **UI Preview Integration**: Verify vehicle preview uses the selected vehicle prefab.
4. **Payload Slot Discovery**: Verify slot count is read from selected vehicle prefab's `PayloadManager`.
5. **Tier Propagation**: Verify selected vehicle tier is used by gun and payload filters.
6. **Scene Persistence**: Verify selected prefab is stored before loading gameplay.
7. **Fallback Handling**: Verify default slot count is used when `PayloadManager` is missing.
8. **Selection Refresh**: Verify selector data refreshes after vehicle changes.

### 4. Loadout Integration Tests

**Files Tested:**
- `Assets/Scripts/Managers/GameSelectionManager.cs`
- `Assets/Scripts/Selector/GunSelector.cs`
- `Assets/Scripts/Selector/PayloadSelector.cs`
- `Assets/Scripts/Player/PlayerController.cs`

#### Integration Test Cases:
1. **Gun Persistence**: Verify confirmed gun is stored in `GameSelectionManager`.
2. **Payload Persistence**: Verify confirmed payload array is stored and resized based on vehicle slot count.
3. **Vehicle Slot Count Persistence**: Verify vehicle payload slot count persists across menu flow.
4. **Gameplay Application**: Verify selected payloads are applied to the spawned aircraft.
5. **Gun Runtime Application**: Verify confirmed gun data is applied to active `Gun`.
6. **Slot Mismatch Detection**: Verify mismatch between selected slot count and physical aircraft slots is detected.
7. **Null Loadout Handling**: Verify empty payload slots do not crash processing.
8. **Tier Compatibility**: Verify lower-tier vehicles cannot access higher-tier guns/payloads through selector filtering.
9. **Runtime Reconfiguration**: Verify payload slots can be updated and reprocessed.
10. **Scene-to-Scene State**: Verify selected loadout data survives scene loading through singleton manager/static references.

## Structural Testing Checklist

### Data-Driven Pattern Structure
- [x] Weapon data separated into ScriptableObject assets
- [x] Payload data separated into ScriptableObject assets
- [x] Vehicle data separated into ScriptableObject assets
- [x] Runtime behavior reads from data assets
- [x] Selector UI reads from configurable arrays
- [x] Gameplay systems apply confirmed data at runtime

### Object/Data Initialization
- [x] Gun data contains combat, artwork, prefab, tier, and price fields
- [x] Payload data contains combat, prefab, audio, ammo, tier, and missile-specific fields
- [x] Vehicle data contains prefab, movement, health, tier, and price fields
- [x] Runtime `Gun` component can switch active data asset
- [x] Runtime `PayloadManager` can reinitialize after slot changes

### Data Validation
- [x] Gun damage clamped to valid range
- [x] Gun fire rate clamped to valid range
- [x] Gun bullet speed forced positive
- [x] Gun heat rate forced non-negative
- [x] Gun tier and price forced valid
- [x] Payload damage clamped to valid range
- [x] Payload speed forced positive
- [x] Payload reload time forced positive
- [x] Payload lifetime forced positive
- [x] Payload ammo forced non-negative
- [x] Payload tier and price forced valid
- [x] Payload homing values constrained

### Scalability & Reuse
- [x] New guns can be added as new data assets
- [x] New payloads can be added as new data assets
- [x] Existing runtime logic can reuse multiple data assets
- [x] Payload slots can reuse identical payload data
- [x] Tier filtering allows controlled content progression
- [ ] UI item registration is still manual
- [ ] No centralized data registry is present yet

### Runtime Safety
- [x] Null gun data is rejected
- [x] Missing bullet prefab is detected before instantiate
- [x] Missing payload prefab is detected before instantiate
- [x] Null spawn points are skipped
- [x] Null hardpoints are skipped
- [x] Empty payload loadout does not crash processing
- [x] Slot mismatch is detected
- [ ] Projectile counter can become negative if destroyed bullets were not counted
- [ ] Some singleton calls still require consistent null checks

## Test Results Summary

### Scenario Results

| ID | Scenario | Result | Status |
|---|---|---|---|
| D1 | Penambahan Senjata | New guns can be added through ScriptableObject data, but UI registration remains manual. | Partial Pass |
| D2 | Perubahan Parameter | Damage, fire rate, bullet speed, and heat rate are editable and validated in data/runtime layers. | Pass |
| D3 | Variasi Loadout | Gun and payload combinations are supported through selectors and `GameSelectionManager`. | Pass |
| D4 | Skalabilitas Sistem | Data can scale, but manual UI mapping limits practical scalability. | Partial Pass |
| D5 | Kolaborasi Designer | Designers can edit values through Inspector without code changes. | Pass |
| D6 | Maintainability | Data is separated, but `Gun.cs` still owns many responsibilities. | Partial Pass |
| D7 | Reusability | `Guns` and `Payload` assets can be reused across aircraft/loadouts. | Pass |
| D8 | Error Handling | Validation and null checks improved, but some edge cases remain. | Partial Pass |
| D9 | Iterasi Balancing | Balancing is fast because parameters are data assets. | Pass |
| D10 | Integrasi Sistem | Data-driven selections integrate into gameplay through managers and runtime components. | Pass |

### Expected Test Outcomes

- **Total Structural Scenarios**: 10
- **Pass**: 6
- **Partial Pass**: 4
- **Fail**: 0
- **Primary Risk Areas**: manual UI registration, concentrated runtime logic in `Gun.cs`, incomplete central registry, and projectile counter safety.

## Architectural Compliance

### Design Patterns Verified

**Data-Driven Architecture**
- Data is stored in ScriptableObject assets.
- Runtime systems consume data through references.
- Parameter tuning does not require code changes.

**Runtime Configuration Pattern**
- `Gun.ApplyGunProperties()` applies selected data to active weapon behavior.
- `PayloadManager.SetPayloadAtSlotIndex()` applies selected payloads to physical slots.
- `GameSelectionManager` persists selections across menu/gameplay flow.

**Inspector-Driven Content Pipeline**
- Designers can create and modify assets through Unity Inspector.
- `OnValidate()` provides immediate data correction for invalid values.

**Prefab-Based Composition**
- Vehicles and projectiles are spawned from prefab references stored in data assets.
- Runtime behavior is composed from prefab components plus ScriptableObject data.

## Quality Metrics

### Code Quality
- Data definitions are centralized in ScriptableObjects.
- Validation exists in both asset and runtime layers.
- Null prefab checks prevent several runtime crashes.
- Direct instantiation remains present and could later be replaced by pooling/factory services.

### Test Quality
- Scenarios focus on architectural structure, not moment-to-moment gameplay feel.
- Testing evaluates extensibility, maintainability, scalability, reuse, and integration.
- Results identify both passed behavior and residual architectural risk.

### Coverage Quality
- Covers gun data flow.
- Covers payload data flow.
- Covers vehicle selection data flow.
- Covers loadout persistence.
- Covers runtime instantiation and application.

## Test Execution Instructions

### Manual Structural Review

1. Open Unity project.
2. Inspect `Guns`, `Payload`, and `Vehicles` ScriptableObject definitions.
3. Verify data fields and `OnValidate()` constraints.
4. Inspect `GunSelector` and `PayloadSelector` for tier filtering.
5. Inspect `GameSelectionManager` for confirmed loadout persistence.
6. Inspect `Gun` and `PayloadManager` for runtime application and safety checks.

### Suggested Unity Test Runner Expansion

Future automated tests can be added as EditMode tests:

```bash
unity -projectPath . -runTests -testPlatform EditMode
```

Recommended test categories:

- `GunDataStructuralTests`
- `PayloadDataStructuralTests`
- `VehicleDataStructuralTests`
- `LoadoutDataIntegrationTests`
- `DataDrivenArchitectureIntegrationTests`

## Future Considerations

1. **Data Registry**: Add centralized asset registry to remove manual array management.
2. **UI Auto Generation**: Generate gun/payload UI from data assets automatically.
3. **Projectile Pooling**: Replace repeated `Instantiate()` calls with pooling.
4. **Validation Utility**: Centralize validation rules to avoid duplication between data and runtime code.
5. **Weapon Service Layer**: Move firing/spawning responsibility out of `Gun.cs`.
6. **Automated Tests**: Add Unity EditMode tests for data validation and selector filtering.

## Notes

- Tests verify architecture and code structure, not final gameplay balance.
- Existing system is primarily data-driven, not hard-coded native factory.
- Some creation still uses direct Unity `Instantiate()`.
- Manual UI registration remains the main scalability limitation.
- Current data-driven structure is more suitable for frequent balancing and designer iteration.

---

**Last Updated**: 2026-06-13  
**Test Suite Version**: 1.0  
**Target Project**: Syncara-Sky  
**Architecture Under Test**: Data-Driven ScriptableObject Architecture
