# PZR_Baseline_Profile (IP-0024 Stage A Authority Freeze)

Date: 2026-02-15
Status: Approved for runtime authority (single-source constants)
Scope: Pressurizer-only baseline used by simulation runtime and controls

## DOC Conflict Resolutions

| Conflict | Resolution |
|---|---|
| DOC-CF-01 (RCP sequence ambiguity) | Startup sequence is fixed to: pressurize with CVCS first, establish bubble, then permit RCP starts. |
| DOC-CF-02 (RCP pressure threshold ambiguity) | RCP automatic start permissive is fixed at `>= 400 psig`. |
| DOC-CF-03 (320-400 band vs 400-425 target) | `320-400 psig` is the solid-plant control band; startup transition target is `400-425 psig` before RCP start permissive. |
| DOC-CF-04 (spray max 840 vs 900) | Authoritative spray maximum flow is fixed at `840 gpm`. |

## PZR_Baseline_Profile

| Parameter Family | Approved Value | Runtime Authority Constant |
|---|---|---|
| Total PZR volume | `1800 ft^3` | `PlantConstants.PZR_BASELINE_TOTAL_VOLUME_FT3` |
| Heater total capacity | `1794 kW` | `PlantConstants.PZR_BASELINE_HEATER_TOTAL_KW` |
| Heater proportional capacity | `414 kW` | `PlantConstants.PZR_BASELINE_HEATER_PROP_KW` |
| Heater backup capacity | `1380 kW` | `PlantConstants.PZR_BASELINE_HEATER_BACKUP_KW` |
| Spray max flow | `840 gpm` | `PlantConstants.PZR_BASELINE_SPRAY_MAX_GPM` |
| Pressure setpoint | `2235 psig` | `PlantConstants.PZR_BASELINE_PRESSURE_SETPOINT_PSIG` |
| Proportional heaters full-on | `2220 psig` | `PlantConstants.PZR_BASELINE_PROP_HEATER_FULL_ON_PSIG` |
| Proportional heaters zero-output | `2250 psig` | `PlantConstants.PZR_BASELINE_PROP_HEATER_ZERO_PSIG` |
| Backup heaters ON | `2210 psig` | `PlantConstants.PZR_BASELINE_BACKUP_HEATER_ON_PSIG` |
| Backup heaters OFF | `2217 psig` | `PlantConstants.PZR_BASELINE_BACKUP_HEATER_OFF_PSIG` |
| Spray start | `2260 psig` | `PlantConstants.PZR_BASELINE_SPRAY_START_PSIG` |
| Spray full-open threshold | `2310 psig` | `PlantConstants.PZR_BASELINE_SPRAY_FULL_PSIG` |
| PORV open threshold | `2335 psig` | `PlantConstants.PZR_BASELINE_PORV_OPEN_PSIG` |
| Level program no-load anchor | `25% @ 557F` | `PlantConstants.PZR_BASELINE_LEVEL_NO_LOAD_PERCENT`, `PlantConstants.PZR_BASELINE_LEVEL_TAVG_NO_LOAD_F` |
| Level program full-power anchor | `61.5% @ 584.7F` | `PlantConstants.PZR_BASELINE_LEVEL_FULL_POWER_PERCENT`, `PlantConstants.PZR_BASELINE_LEVEL_TAVG_FULL_POWER_F` |

## Runtime Alias Rule

All legacy duplicate PZR setpoint/capacity families in `PlantConstants.Pressure.cs` and `PlantConstants.CVCS.cs` are aliases to this baseline authority set.
