# IP-0028 Stage B - Authority and Limiter Design Freeze

- IP: `IP-0028`
- DP: `DP-0012`
- Stage: `B`
- Timestamp: `2026-02-16_124209`
- Input baseline: `Governance/Issues/IP-0028_StageA_BaselineFreeze_2026-02-16_124043.md`

## 1) Numeric Freeze (from Stage A baseline + explicit design freeze)

### 1.1 Solid/startup pressure and operating targets

| Item | Frozen value | Units | Source of authority |
|---|---|---|---|
| Solid control low band | `320` | psig | Stage A traceability (`PZR_Baseline_Profile DOC-CF-03`) |
| Solid control high band | `400` | psig | Stage A traceability (`PZR_Baseline_Profile DOC-CF-03`) |
| Solid hold setpoint (current implementation) | `365` (`350 psig`) | psia | Stage A runtime/code baseline (`SOLID_PLANT_P_SETPOINT_PSIA`) |
| Startup transition target window (documentation baseline) | `400-425` | psig | Stage A traceability (`NRC_HRTD_Startup_Pressurization_Reference`, `PZR_Baseline_Profile`) |
| RCP permissive pressure | `>=400` | psig | Stage A traceability + runtime baseline |

### 1.2 Heater/spray control thresholds

| Item | Frozen value | Units | Source of authority |
|---|---|---|---|
| Normal pressure setpoint | `2235` | psig | Stage A traceability |
| Proportional full-on / zero | `2220 / 2250` | psig | Stage A traceability |
| Backup ON / OFF | `2210 / 2217` | psig | Stage A traceability |
| Spray start / full | `2260 / 2310` | psig | Stage A traceability |
| Spray maximum | `840` | gpm | Stage A traceability |
| Startup heater pressure-rate limiter | `100` | psi/hr | Stage A runtime/code baseline |
| Startup heater minimum fraction | `0.20` | fraction | Stage A runtime/code baseline |
| Heater ramp limiter | `6.0` | fraction/hr | Stage A runtime/code baseline |
| Heater mode transition threshold | `2200` | psia | Stage A runtime/code baseline |

### 1.3 Startup hold release gating (Stage B freeze for Stage C implementation)

| Gate | Frozen value/rule | Units | Fail behavior |
|---|---|---|---|
| Minimum hold time gate | `>= 15` | sec | Keep hold active |
| Pressure-rate stability gate | `|dP/dt| <= 200` sustained for `>=10` sec | psi/hr, sec | Keep hold active |
| State-quality gate | pressure, pressure-rate, and PZR thermal state values must be finite each tick | boolean | Keep hold active |
| Blocked-log interval | `30` | sec | Emit one blocked-status log at interval while hold persists |

Design rule: hold release is **not** time-only; all three gates must pass in the same evaluation window.

## 2) Authority Hierarchy Table (startup + steady-state)

### 2.1 Heater authority precedence (highest to lowest)

| Priority | Authority/limiter state | Activation condition | Effective output | Override relationship |
|---|---|---|---|---|
| 1 | `STARTUP_HOLD_LOCKED` | `startupHoldActive == true` | Heater output forced to `0` | Overrides all heater modes and all downstream limiters |
| 2 | `MODE_OFF` | Heater mode explicitly `OFF` while hold not active | Heater output forced to `0` | Overrides automatic demand and rate/ramp limiters |
| 3 | `LOW_LEVEL_INTERLOCK` | PZR level below low-level isolation threshold (heater protection path) | Heater output forced to `0` | Overrides automatic demand when not in priorities 1-2 |
| 4 | `AUTO_DEMAND_PATH` | Hold not active, not `OFF`, interlocks satisfied | Demand computed from selected automatic mode | Subject to internal limiter ordering below |

### 2.2 AUTO demand internal limiter ordering

| Order | Limiter | Activation condition | Resolution rule |
|---|---|---|---|
| A | Pressure-rate clamp | `|dP/dt| > 100 psi/hr` in startup auto modes | Compute target fraction reduced toward min floor |
| B | Ramp limiter | Any step with target-change magnitude above per-step slew limit | Apply slew-clamped movement toward target |
| C | Mode-specific output mapping | Mode = `PRESSURIZE_AUTO` / `BUBBLE_FORMATION_AUTO` / `AUTOMATIC_PID` | Final heater output derived from post-limiter command |

Tie-break rule: pressure-rate clamp computes target first; ramp limiter is applied second on that target; no parallel conflict path remains.

### 2.3 Spray authority precedence

| Priority | Spray state | Activation condition | Effective behavior | Override relationship |
|---|---|---|---|---|
| 1 | `SPRAY_INHIBIT_NO_RCP` | `rcpCount <= 0` | Spray forced disabled; status reason logged | Overrides pressure-driven spray demand |
| 2 | `SPRAY_THERMAL_SHOCK_CLAMP` | `?T` exceeds spray thermal shock limit | Bypass-only/limited spray path | Overrides full demand to protect thermal limits |
| 3 | `SPRAY_PRESSURE_MODULATED` | RCP available and pressure above spray-start | Modulate between 2260-2310 psig | Normal spray control path |

## 3) Startup vs Steady-State Distinction Freeze

1. Startup regime:
   - Control anchored on solid/bubble formation path and startup auto heater logic.
   - Hold lock can fully inhibit heater authority.
   - Pressure-rate clamp and ramp limiter are active in startup auto modes.
2. Steady-state (post-transition toward operating pressure control):
   - `AUTOMATIC_PID` becomes valid authority mode when transition threshold reached.
   - Heater/spray setpoint-driven logic governs around normal operating pressure baseline.

## 4) Ambiguity Resolution Register (explicitly closed)

1. `HOLD_LOCKED` vs `OFF`: resolved by precedence (`HOLD_LOCKED` wins).
2. `OFF` vs auto demand: resolved by precedence (`OFF` wins).
3. No-RCP spray behavior: resolved as hard inhibit (`SPRAY_INHIBIT_NO_RCP`) with status reason.
4. Pressure-rate clamp vs ramp limiter: resolved by strict ordering (clamp target, then slew).
5. Hold release semantics: resolved by all-gates-required release rule (not time-only).

## 5) Stage B Exit Criteria

- No unresolved authority ambiguity remains: **PASS**
- All thresholds and gating rules explicitly declared: **PASS**
- Authority hierarchy and limiter precedence complete with no undefined conflict path: **PASS**

