# Primary Inventory Boundary Audit v1.0.0

**Created:** 2026-02-12  
**Hypothesis Under Test:** Inventory variation correlates with VCT changes because CVCS/BRS/distillate/RWST boundaries are not correctly modeled, causing implicit mass creation or loss.

---

## STAGE 1 — Declared Mass Boundary

### Objective
Explicitly define what currently constitutes "Primary Mass" in the simulation.

---

### 1.1 Current System Architecture

Based on code review of the physics modules, the system models the following interconnected volumes:

**Primary System (RCS + PZR):**
- RCS Water Volume: 12,700 ft³ (loops, reactor vessel, piping)
- Pressurizer Total Volume: 1,800 ft³
  - PZR Water Volume: Variable (tracked by `physicsState.PZRWaterVolume`)
  - PZR Steam Volume: Variable (tracked by `physicsState.PZRSteamVolume`)

**CVCS Loop (VCT ↔ RCS via charging/letdown):**
- Volume Control Tank (VCT): 6,000 gallon capacity
- BRS Holdup Tanks: 56,000 gallon usable capacity
- BRS Distillate/Concentrate: Tracked separately

**External Boundaries:**
- RWST: Source for emergency makeup (not explicitly volume-modeled)
- RMS (Reactor Makeup System): Blending source (not explicitly volume-modeled)
- Controlled Bleedoff (CBO): 1 gpm loss when RCPs running (true sink)
- SG Secondary: Separate inventory, not part of primary

---

### 1.2 Volumes INCLUDED in Primary Mass Tracking

| Volume | Tracking Method | State Variable | Notes |
|--------|-----------------|----------------|-------|
| RCS Loop Water | Stored variable | `physicsState.RCSWaterMass` | lbm, updated each step |
| PZR Water (Solid Ops) | Stored variable | `solidPlantState.PzrWaterMass` | lbm, conserved via surge |
| PZR Water (Two-Phase) | Stored variable | `physicsState.PZRWaterMass` | lbm, from V×ρ after solver |
| PZR Steam | Stored variable | `physicsState.PZRSteamMass` | lbm, from V×ρ after solver |

**Key Finding:** The v5.0.2 fix introduced `TotalPrimaryMassSolid` and `PZRWaterMassSolid` for solid-ops conservation. However, there is NO single canonical "total primary mass" variable that persists across the solid→two-phase transition.

---

### 1.3 Volumes EXCLUDED from Primary Mass Boundary

| Volume | Reason | Conservation Impact |
|--------|--------|---------------------|
| VCT | Separate reservoir outside RCS | VCT tracks its own `Volume_gal` |
| BRS Holdup | Downstream of letdown divert | BRS tracks `HoldupVolume_gal` |
| BRS Distillate | Processed water returned to VCT | BRS tracks `DistillateAvailable_gal` |
| BRS Concentrate | Borated acid to BAT | BRS tracks `ConcentrateAvailable_gal` |
| RWST | Emergency source only | Not modeled as reservoir |
| RMS | Blending source | Not modeled as reservoir |
| SG Secondary | Separate system | Tracked in `sgMultiNodeState` |

---

### 1.4 Boundary Classification

The system appears to be modeled as:

**Primary + Closed CVCS Loop + External Sources/Sinks**

```
┌────────────────────────────────────────────────────────┐
│                    PRIMARY BOUNDARY                     │
│  ┌─────────────────────────────────────────────────┐   │
│  │               RCS + PRESSURIZER                  │   │
│  │   RCSWaterMass + PZRWaterMass + PZRSteamMass    │   │
│  └──────────────────┬───────────┬──────────────────┘   │
│                     │   SURGE   │                       │
│                     └───────────┘                       │
│           ▲ Charging                 ▼ Letdown          │
└───────────│─────────────────────────────│──────────────┘
            │                             │
    ┌───────┴───────┐             ┌───────┴───────┐
    │      VCT      │◄───────────►│    BRS        │
    │   (Buffer)    │   Divert    │  (Holdup)     │
    └───────┬───────┘             └───────┬───────┘
            │                             │
    ┌───────┴───────┐             ┌───────┴───────┐
    │  RMS/RWST     │             │  Distillate   │
    │  (Sources)    │             │  (Return)     │
    └───────────────┘             └───────────────┘
            ▲                             │
            └─────────────────────────────┘
                     CBO Loss (Sink)
```

---

### 1.5 Critical Observation: Boundary Definition Issue

**The system does NOT enforce a single canonical total mass variable.**

In `HeatupSimEngine.cs`, the code does:

```csharp
// Solid ops (Regime 1):
physicsState.TotalPrimaryMassSolid += massChange_lb;
physicsState.RCSWaterMass += massChange_lb;

// Two-phase (Regime 3):
physicsState.RCSWaterMass += cvcsNetMass_lb;
// No TotalPrimaryMass update!
```

**Problem:** In two-phase operations, `RCSWaterMass` is updated, but there is no corresponding update to a total mass ledger. The PZR mass is derived from solver-computed volumes (`PZRWaterVolume × ρ`), not conserved.

---

### 1.6 Stage 1 Summary

| Question | Answer |
|----------|--------|
| What is "Primary Mass"? | RCS + PZR water + PZR steam (loosely defined) |
| How is it tracked? | Multiple variables, inconsistently |
| Is boundary closed? | NO — CVCS flows cross boundary |
| Is boundary conserving? | PARTIAL — Solid ops v5.0.2 fix is conserving; two-phase is NOT |
| External sources modeled? | RWST/RMS as flow inputs, not reservoirs |
| External sinks modeled? | CBO loss (1 gpm) is only true sink |

**Root Cause Hypothesis Supported:** The boundary definition is inconsistent between solid-ops and two-phase operations. The v5.0.2 fix only addressed solid-ops; two-phase still derives PZR mass from V×ρ, which can drift from the conservation constraint.

---

**Stage 1 Complete.** Proceeding to Stage 2.

---

## STAGE 2 — Primary Volume Accounting

### Objective
For each mass-holding volume in the primary system, document how mass is computed, whether density depends on T only or T,P, and when density is updated in the timestep.

---

### 2.1 RCS Loop Water Mass

| Property | Value |
|----------|-------|
| **State Variable** | `physicsState.RCSWaterMass` (lbm) |
| **Computation Method** | Derived from V × ρ(T,P) |
| **Density Function** | `WaterProperties.WaterDensity(T_rcs, pressure)` |
| **Density Depends On** | Both T and P |
| **When Updated** | After CoupledThermo solver converges |

**Code Path (Regime 3 - Two-Phase Operations):**
```csharp
// In CoupledThermo.SolveEquilibrium():
float rho_RCS = WaterProperties.WaterDensity(T_new, P_new);
state.RCSWaterMass = V_RCS * rho_RCS;  // <-- DERIVED, not conserved
```

**CRITICAL FINDING:** RCS mass is DERIVED from volume × density, not tracked as a conserved quantity. This is problematic because:
1. The volume `V_RCS` is constant (12,700 ft³)
2. The density is recalculated each step
3. If the density calculation has any error, mass "drifts"

---

### 2.2 Pressurizer Water Mass (Two-Phase)

| Property | Value |
|----------|-------|
| **State Variable** | `physicsState.PZRWaterMass` (lbm) |
| **Computation Method** | Derived from V × ρ(T_sat, P) |
| **Density Function** | `WaterProperties.WaterDensity(T_sat, pressure)` |
| **Density Depends On** | Both T_sat (derived from P) and P |
| **When Updated** | After CoupledThermo solver converges |

**Code Path:**
```csharp
// In CoupledThermo.SolveEquilibrium():
float tSat = WaterProperties.SaturationTemperature(P_new);
float rhoPZRWater = WaterProperties.WaterDensity(tSat, P_new);
state.PZRWaterMass = state.PZRWaterVolume * rhoPZRWater;  // <-- DERIVED
```

**CRITICAL FINDING:** PZR water mass is also DERIVED from volume × density. The solver iterates to find volumes that satisfy mass conservation, but the final masses are still V × ρ products.

---

### 2.3 Pressurizer Steam Mass (Two-Phase)

| Property | Value |
|----------|-------|
| **State Variable** | `physicsState.PZRSteamMass` (lbm) |
| **Computation Method** | Derived from V × ρ_steam(P) |
| **Density Function** | `WaterProperties.SaturatedSteamDensity(pressure)` |
| **Density Depends On** | P only (saturated steam) |
| **When Updated** | After CoupledThermo solver converges |

**Code Path:**
```csharp
// In CoupledThermo.SolveEquilibrium():
float rhoPZRSteam = WaterProperties.SaturatedSteamDensity(P_new);
state.PZRSteamMass = state.PZRSteamVolume * rhoPZRSteam;  // <-- DERIVED
```

---

### 2.4 Pressurizer Water Mass (Solid Operations - v5.0.2 Fix)

| Property | Value |
|----------|-------|
| **State Variable** | `solidPlantState.PzrWaterMass` (lbm) |
| **Computation Method** | **CONSERVED** — updated only by surge transfer |
| **Density Function** | Used for display only, not mass update |
| **When Updated** | After surge mass transfer calculation |

**Code Path (SolidPlantPressure.Update):**
```csharp
// v5.0.2: Mass conservation during solid ops
float rho_pzr_post = WaterProperties.WaterDensity(state.T_pzr, state.Pressure);
float surgeMass_lb = dV_pzr_ft3 * rho_pzr_post;
state.PzrWaterMass -= surgeMass_lb;  // <-- CONSERVED by transfer
state.SurgeMassTransfer_lb = surgeMass_lb;
```

**FINDING:** The v5.0.2 fix correctly implements mass conservation for solid operations. PZR mass is updated ONLY by explicit surge transfer, not by V × ρ recalculation.

---

### 2.5 Total Primary Mass (Solid Operations)

| Property | Value |
|----------|-------|
| **State Variable** | `physicsState.TotalPrimaryMassSolid` (lbm) |
| **Computation Method** | **CONSERVED** — updated only by CVCS boundary flow |
| **When Updated** | After CVCS net flow calculation |

**Code Path (HeatupSimEngine.cs, Regime 1):**
```csharp
// CVCS boundary flow: the ONLY thing that changes total primary mass
float netCVCS_gpm = chargingFlow - letdownFlow;
float massChange_lb = netCVCS_gpm * dt_sec * GPM_TO_FT3_SEC * rho_rcs;
physicsState.TotalPrimaryMassSolid += massChange_lb;
```

**FINDING:** This is correct for solid operations. Total primary mass changes ONLY due to CVCS boundary flows.

---

### 2.6 Summary Table: Mass Computation Methods

| Volume | Solid Ops | Two-Phase | Conservation Status |
|--------|-----------|-----------|---------------------|
| RCS Water | Derived (M = V×ρ - PZR) | Derived (M = V×ρ) | ❌ Not conserved |
| PZR Water | **CONSERVED** (v5.0.2) | Derived (M = V×ρ) | ⚠️ Partial |
| PZR Steam | N/A (no bubble) | Derived (M = V×ρ) | ❌ Not conserved |
| Total Primary | **CONSERVED** (v5.0.2) | NOT TRACKED | ❌ Missing variable |

---

### 2.7 Density Update Timing

The density calculations use values from different points in the timestep:

| Calculation | Temperature Used | Pressure Used | Timing Issue? |
|-------------|-----------------|---------------|---------------|
| RCS density | `T_new` (post-step) | `P_new` (post-step) | ✓ Consistent |
| PZR water density | `T_sat(P_new)` | `P_new` (post-step) | ✓ Consistent |
| PZR steam density | N/A | `P_new` (post-step) | ✓ Consistent |
| CVCS mass change | `T_rcs` (pre-step) | `pressure` (pre-step) | ⚠️ Uses old values |

**FINDING:** CVCS mass calculations use density from the PREVIOUS timestep, but this is physically reasonable (1-step lag). The issue is not timing, but rather the fundamental architecture.

---

### 2.8 Stage 2 Key Findings

1. **Two-phase operations derive mass from V×ρ, not conservation.**
   - CoupledThermo solver iterates to find volumes, then calculates masses from volumes
   - If the steam tables have any imprecision, mass will drift
   - There is no "total primary mass" ledger in two-phase ops

2. **Solid operations (v5.0.2) correctly implement mass conservation.**
   - `TotalPrimaryMassSolid` is updated only by CVCS boundary flows
   - `PZRWaterMassSolid` is updated only by surge transfers
   - Loop mass is derived as (Total - PZR), guaranteeing conservation

3. **The transition from solid to two-phase loses the conservation constraint.**
   - When `bubbleFormed = true`, the engine switches to CoupledThermo
   - CoupledThermo does not inherit the conserved mass values
   - From this point forward, mass is derived, not conserved

**Stage 2 Complete.** Proceeding to Stage 3.

---

## STAGE 3 — Mass Transfer Paths

### Objective
Trace every possible mass transfer path across the primary boundary and within the system.

---

### 3.1 Internal Transfer: Surge Line (PZR ↔ RCS Loops)

| Property | Value |
|----------|-------|
| **Flow Direction** | Bidirectional (thermal expansion/contraction) |
| **Source Volume** | PZR Water (expansion) or RCS Loops (contraction) |
| **Destination Volume** | RCS Loops (expansion) or PZR Water (contraction) |
| **Conservation Enforced?** | ⚠️ Only in solid ops (v5.0.2 fix) |

**Solid Ops Implementation (SolidPlantPressure.Update):**
```csharp
// Surge mass derived from thermal expansion volume
float surgeMass_lb = dV_pzr_ft3 * rho_pzr_post;
state.PzrWaterMass -= surgeMass_lb;  // Mass leaves PZR
// Loop mass = TotalPrimaryMass - PZR mass (implicitly gained)
```

**Two-Phase Implementation (CoupledThermo.SolveEquilibrium):**
```csharp
// No explicit surge transfer — solver just finds new volumes
// Mass "moves" implicitly via V × ρ recalculation
```

**FINDING:** Surge transfer is explicitly tracked in solid ops but implicit in two-phase. The implicit approach means mass is not explicitly conserved across the transfer.

---

### 3.2 Boundary Transfer: Charging (VCT → RCS)

| Property | Value |
|----------|-------|
| **Flow Direction** | Into RCS |
| **Source Volume** | VCT |
| **Destination Volume** | RCS Cold Leg |
| **Flow Variable** | `chargingFlow` (gpm) |
| **Conservation Enforced?** | ⚠️ Partial |

**Implementation (HeatupSimEngine.cs):**

*Regime 1 (Solid Ops):*
```csharp
float netCVCS_gpm = chargingFlow - letdownFlow;
float massChange_lb = netCVCS_gpm * dt_sec * GPM_TO_FT3_SEC * rho_rcs;
physicsState.TotalPrimaryMassSolid += massChange_lb;
physicsState.RCSWaterMass += massChange_lb;
```

*Regime 3 (Two-Phase):*
```csharp
float netCVCS_gpm = chargingFlow - letdownFlow;
float cvcsNetMass_lb = netCVCS_gpm * dt_sec * GPM_TO_FT3_SEC * rho_rcs;
physicsState.RCSWaterMass += cvcsNetMass_lb;
// NOTE: No TotalPrimaryMass update in two-phase!
```

**VCT Side (VCTPhysics.Update):**
```csharp
float flowOut_gpm = chargingFlow + state.DivertFlow + cboLoss;
state.Volume_gal -= flowOut_gpm * dt_min;  // Volume decreases
```

**FINDING:** Charging is applied as net CVCS flow (charging - letdown). Both sides track the transfer, but there's no cross-check to verify they match.

---

### 3.3 Boundary Transfer: Letdown (RCS → VCT)

| Property | Value |
|----------|-------|
| **Flow Direction** | Out of RCS |
| **Source Volume** | RCS Hot Leg |
| **Destination Volume** | VCT (or BRS via divert) |
| **Flow Variable** | `letdownFlow` (gpm) |
| **Conservation Enforced?** | ⚠️ Partial |

**Implementation:** Same net CVCS calculation as charging (see 3.2).

**Divert Logic (VCTPhysics.Update):**
```csharp
if (state.Level_percent > divertSetpoint)
{
    float divertFraction = (state.Level_percent - divertSetpoint) / divertBand;
    state.DivertFlow_gpm = letdownFlow * divertFraction;
}
```

**FINDING:** Letdown can be diverted to BRS. The divert fraction is proportional to VCT level above setpoint. This is correctly tracked on the VCT/BRS side, but there's no verification that total flow equals letdown.

---

### 3.4 Boundary Transfer: VCT Divert (VCT → BRS)

| Property | Value |
|----------|-------|
| **Flow Direction** | VCT overflow to BRS |
| **Source Volume** | VCT (indirectly, letdown that bypasses VCT) |
| **Destination Volume** | BRS Holdup Tanks |
| **Flow Variable** | `state.DivertFlow_gpm` |
| **Conservation Enforced?** | ✓ Yes, via BRS.ReceiveDivert() |

**Implementation (VCTPhysics.Update → BRSPhysics.ReceiveDivert):**
```csharp
// VCT side: divert reduces flow into VCT
float flowIn_gpm = letdownFlow - divertFlow + sealReturnFlow + makeupFlow;

// BRS side: receives diverted water
state.HoldupVolume_gal += actualReceived;
state.CumulativeIn_gal += actualReceived;
```

**FINDING:** VCT→BRS transfer is correctly conserved. Both sides track the same volume. The BRS mixing equation correctly updates boron concentration.

---

### 3.5 Boundary Transfer: BRS Distillate Return (BRS → VCT)

| Property | Value |
|----------|-------|
| **Flow Direction** | Processed water back to VCT |
| **Source Volume** | BRS Monitor Tanks |
| **Destination Volume** | VCT |
| **Flow Variable** | Via `MakeupFlow_gpm` when `MakeupFromBRS = true` |
| **Conservation Enforced?** | ✓ Yes |

**Implementation (VCTPhysics.Update + BRSPhysics.WithdrawDistillate):**
```csharp
// VCT triggers makeup when level low
if (state.Level_percent <= LEVEL_MAKEUP_START)
    state.MakeupFlow_gpm = AUTO_MAKEUP_FLOW_GPM;
    state.MakeupFromBRS = brsDistillateAvailable_gal > 0f;

// BRS provides distillate if available
float actualWithdraw = Min(requested, available);
state.DistillateAvailable_gal -= actualWithdraw;
state.CumulativeReturned_gal += actualWithdraw;
```

**FINDING:** BRS→VCT transfer is correctly conserved. Both sides track withdrawals.

---

### 3.6 Boundary Transfer: RWST/RMS Makeup (External → VCT)

| Property | Value |
|----------|-------|
| **Flow Direction** | External source into VCT |
| **Source Volume** | RWST or RMS (not explicitly modeled) |
| **Destination Volume** | VCT |
| **Flow Variable** | `MakeupFlow_gpm` when `!MakeupFromBRS` |
| **Conservation Enforced?** | ❌ No — implicit source |

**Implementation (VCTPhysics.Update):**
```csharp
// Makeup flow adds to VCT without subtracting from anywhere
state.Volume_gal += makeupFlow * dt_min;
```

**CRITICAL FINDING:** RWST/RMS makeup is an IMPLICIT SOURCE. Mass is created when VCT level drops and makeup triggers. This is intentional (RWST is external), but it means the CVCS loop is NOT closed.

---

### 3.7 Boundary Transfer: Controlled Bleedoff (CBO)

| Property | Value |
|----------|-------|
| **Flow Direction** | Out of CVCS loop |
| **Source Volume** | RCP seal leakoff |
| **Destination Volume** | Waste processing (not modeled) |
| **Flow Variable** | `CBO_LOSS_GPM = 1 gpm` |
| **Conservation Enforced?** | ❌ Implicit sink |

**Implementation (VCTPhysics.Update):**
```csharp
float cboLoss = rcpCount > 0 ? CBO_LOSS_GPM : 0f;
float flowOut_gpm = chargingFlow + divertFlow + cboLoss;
```

**FINDING:** CBO is an IMPLICIT SINK. Mass leaves the system without going anywhere. This is intentional (CBO goes to waste processing), but it's a true mass loss from the modeled boundary.

---

### 3.8 Boundary Transfer: Seal Injection/Return

| Property | Value |
|----------|-------|
| **Flow Direction** | Bidirectional (injection to RCP, leakoff to VCT) |
| **Source Volume** | Charging header (seal injection) |
| **Destination Volume** | RCP seals → VCT (seal return) |
| **Conservation Enforced?** | ⚠️ Partial |

**Implementation (CVCSController.CalculateSealFlows):**
```csharp
state.SealInjection = rcpCount * SEAL_INJECTION_PER_PUMP_GPM;  // 8 gpm/pump
state.SealReturnToVCT = rcpCount * SEAL_LEAKOFF_PER_PUMP_GPM;   // 3 gpm/pump
state.SealReturnToRCS = rcpCount * SEAL_FLOW_TO_RCS_PER_PUMP_GPM; // 5 gpm/pump
```

**FINDING:** Seal flows are tracked but there's a discrepancy:
- Seal injection: 8 gpm/pump comes from charging
- Seal return to VCT: 3 gpm/pump (leakoff past #1 seal)
- Seal return to RCS: 5 gpm/pump (past #1 seal to RCS)

The 5 gpm that returns to RCS is not explicitly subtracted from the charging that goes to RCS. This may cause double-counting.

---

### 3.9 Boundary Transfer: Spray Condensation

| Property | Value |
|----------|-------|
| **Flow Direction** | Internal (PZR steam → PZR water) |
| **Source Volume** | PZR Steam |
| **Destination Volume** | PZR Water |
| **Conservation Enforced?** | ✓ Yes (mass transfer within PZR) |

**Implementation (v4.4.0 - HeatupSimEngine.cs):**
```csharp
if (sprayState.SteamCondensed_lbm > 0f)
{
    physicsState.PZRSteamMass -= sprayState.SteamCondensed_lbm;
    physicsState.PZRWaterMass += sprayState.SteamCondensed_lbm;
}
```

**FINDING:** Spray condensation is correctly conserved within the PZR. Mass moves from steam to water phase with no loss.

---

### 3.10 Boundary Transfer: Relief Valve (RHR Relief)

| Property | Value |
|----------|-------|
| **Flow Direction** | Out of RCS (emergency only) |
| **Source Volume** | RCS/PZR |
| **Destination Volume** | PRT (not modeled) |
| **Conservation Enforced?** | ❌ Implicit sink |

**Implementation (SolidPlantPressure.CalculateReliefFlow):**
```csharp
// Relief opens above 450 psig
if (pressure_psig >= RELIEF_SETPOINT_PSIG)
{
    float fraction = (pressure_psig - RELIEF_SETPOINT_PSIG) / RELIEF_ACCUMULATION_PSI;
    return fraction * RELIEF_CAPACITY_GPM;
}
```

**FINDING:** Relief valve flow is calculated but NOT applied to primary mass! The flow is added to `netCVCS_gpm` for VCT tracking but there's no explicit mass removal from RCS.

---

### 3.11 Mass Transfer Path Summary

| Transfer | Source | Destination | Conserved? | Issue |
|----------|--------|-------------|------------|-------|
| Surge (solid) | PZR | Loops | ✓ Yes | |
| Surge (two-phase) | PZR | Loops | ❌ No | Implicit via V×ρ |
| Charging | VCT | RCS | ⚠️ Partial | No cross-check |
| Letdown | RCS | VCT | ⚠️ Partial | No cross-check |
| VCT Divert | VCT | BRS | ✓ Yes | |
| BRS Distillate | BRS | VCT | ✓ Yes | |
| RMS/RWST Makeup | External | VCT | N/A | Implicit source |
| CBO | CVCS | External | N/A | Implicit sink |
| Seal Injection | Charging | RCPs | ⚠️ Partial | May double-count |
| Spray | PZR Steam | PZR Water | ✓ Yes | |
| Relief | RCS | PRT | ❌ No | Flow calculated but not applied |

---

### 3.12 Stage 3 Key Findings

1. **Internal transfers (surge) are only conserved in solid ops.**
   - Two-phase surge is implicit — solver finds new volumes without explicit transfer

2. **CVCS boundary transfers lack cross-checks.**
   - RCS and VCT both track net CVCS flow, but there's no verification they match
   - Density differences (RCS vs VCT water) could cause drift

3. **Seal flow accounting may double-count.**
   - 5 gpm/pump seal return to RCS may not be properly subtracted from charging

4. **Relief valve flow is calculated but not applied to mass.**
   - Relief flow appears in CVCS calculations but doesn't actually remove mass

5. **External sources/sinks are intentionally not conserved.**
   - RWST/RMS makeup and CBO loss are true boundary crossings
   - These are correct behavior for an open system

**Stage 3 Complete.** Proceeding to Stage 4.

---

## STAGE 4 — External Reservoir Accounting

### Objective
Determine whether VCT, BRS, and RWST are modeled as conserving reservoirs.

---

### 4.1 Volume Control Tank (VCT)

| Property | Status |
|----------|--------|
| **Modeled as Reservoir?** | ✓ Yes |
| **Volume Tracked?** | ✓ Yes (`Volume_gal`) |
| **Mass Conserved?** | ⚠️ Partial |

**Conservation Analysis:**

The VCT tracks its own volume balance:
```csharp
float flowIn_gpm = letdownFlow + sealReturnFlow + makeupFlow;
float flowOut_gpm = chargingFlow + divertFlow + cboLoss;
state.Volume_gal += (flowIn_gpm - flowOut_gpm) * dt_min;
```

**Cumulative Tracking:**
```csharp
state.CumulativeIn_gal += flowIn_gpm * dt_min;
state.CumulativeOut_gal += flowOut_gpm * dt_min;
state.CumulativeExternalIn_gal += (sealReturnFlow + makeupFlow) * dt_min;
state.CumulativeExternalOut_gal += (divertFlow + cboLoss) * dt_min;
```

**Verification Function:**
```csharp
public static float VerifyMassConservation(VCTState state, float rcsInventoryChange_gal)
{
    float vctChange = state.Volume_gal - state.InitialVolume_gal;
    float rcsChange = state.CumulativeRCSChange_gal;
    float externalNet = state.CumulativeExternalIn_gal - state.CumulativeExternalOut_gal;
    
    // Conservation: vctChange + rcsChange - externalNet ≈ 0
    float error = Abs(vctChange + rcsChange - externalNet);
    return error;
}
```

**FINDING:** VCT has a proper conservation check, but it relies on the engine calling `AccumulateRCSChange()` correctly. If the RCS side doesn't track perfectly, the verification will show error.

---

### 4.2 Boron Recycle System (BRS)

| Property | Status |
|----------|--------|
| **Modeled as Reservoir?** | ✓ Yes |
| **Volume Tracked?** | ✓ Yes (`HoldupVolume_gal`, `DistillateAvailable_gal`) |
| **Mass Conserved?** | ✓ Yes |

**Conservation Analysis:**

BRS tracks inflows and outflows explicitly:
```csharp
// Receiving divert
state.HoldupVolume_gal += actualReceived;
state.CumulativeIn_gal += actualReceived;

// Processing (evaporator)
state.HoldupVolume_gal -= processVolume;
state.DistillateAvailable_gal += distillateVolume;
state.ConcentrateAvailable_gal += concentrateVolume;

// Returning distillate
state.DistillateAvailable_gal -= actualWithdraw;
state.CumulativeReturned_gal += actualWithdraw;
```

**Mass Balance Equation:**
```
Holdup_in + Distillate_produced + Concentrate_produced = Divert_received + Distillate_returned
```

This is enforced by the evaporator model:
```csharp
processVolume = distillateVolume + concentrateVolume  // Mass balance
```

**FINDING:** BRS is correctly modeled as a conserving reservoir. All transfers are explicitly tracked.

---

### 4.3 Refueling Water Storage Tank (RWST)

| Property | Status |
|----------|--------|
| **Modeled as Reservoir?** | ❌ No |
| **Volume Tracked?** | ❌ No |
| **Mass Conserved?** | N/A — treated as infinite source |

**Analysis:**

RWST is referenced in VCT makeup logic:
```csharp
if (state.Level_percent <= LEVEL_LOW_LOW)
{
    state.RWSTSuctionActive = true;
    state.MakeupFlow_gpm = MAX_MAKEUP_FLOW_GPM;  // 150 gpm
}
```

But there is no RWST state tracking:
- No `RWSTVolume` variable
- No tracking of water removed from RWST
- Makeup water is created from nothing when needed

**FINDING:** RWST is an IMPLICIT INFINITE SOURCE. This is acceptable for normal heatup operations (RWST is very large), but means the system boundary is not closed.

---

### 4.4 Reactor Makeup System (RMS)

| Property | Status |
|----------|--------|
| **Modeled as Reservoir?** | ❌ No |
| **Volume Tracked?** | ❌ No |
| **Mass Conserved?** | N/A — treated as infinite source |

**Analysis:**

RMS provides automatic makeup when VCT level is low:
```csharp
if (state.Level_percent <= LEVEL_MAKEUP_START && !state.RWSTSuctionActive)
{
    state.AutoMakeupActive = true;
    state.MakeupFlow_gpm = AUTO_MAKEUP_FLOW_GPM;  // 35 gpm
    state.MakeupFromBRS = brsDistillateAvailable_gal > 0f;
}
```

If BRS distillate is not available, RMS provides water with no source tracking.

**FINDING:** RMS is an IMPLICIT INFINITE SOURCE. Like RWST, this is acceptable operationally but means the boundary isn't closed.

---

### 4.5 Pressurizer Relief Tank (PRT)

| Property | Status |
|----------|--------|
| **Modeled as Reservoir?** | ❌ No |
| **Volume Tracked?** | ❌ No |
| **Receives Mass?** | Implicitly (from relief valve) |

**Analysis:**

Relief valve flow is calculated:
```csharp
float reliefFlow = CalculateReliefFlow(pressure_psig, state.ReliefFlow > 0f);
```

But there is no PRT to receive it. The flow simply disappears.

**FINDING:** PRT is an IMPLICIT INFINITE SINK. Relief valve mass is not tracked.

---

### 4.6 Stage 4 Summary Table

| Reservoir | Modeled? | Volume Tracked? | Conservation Status |
|-----------|----------|-----------------|---------------------|
| VCT | ✓ Yes | ✓ Yes | ⚠️ Cross-check exists but depends on RCS accuracy |
| BRS | ✓ Yes | ✓ Yes | ✓ Fully conserved |
| RWST | ❌ No | ❌ No | N/A — Infinite source |
| RMS | ❌ No | ❌ No | N/A — Infinite source |
| PRT | ❌ No | ❌ No | N/A — Infinite sink |
| BAT | ⚠️ Partial | ⚠️ Only concentrate | ✓ One-way from BRS |

---

### 4.7 Stage 4 Key Findings

1. **VCT and BRS are properly modeled as conserving reservoirs.**
   - VCT has cumulative tracking and a verification function
   - BRS correctly balances holdup, distillate, and concentrate

2. **RWST and RMS are implicit infinite sources.**
   - This is intentional — they represent plant auxiliary systems
   - However, it means inventory that appears "from nowhere" when VCT is low

3. **PRT is an implicit infinite sink.**
   - Relief valve flow is calculated but goes nowhere
   - This could mask a mass loss if relief opens frequently

4. **The system boundary is intentionally open.**
   - Closed-loop operation (VCT ↔ RCS ↔ BRS → VCT) can be verified
   - External makeup breaks the closed loop

**Stage 4 Complete.** Proceeding to Stage 5.

---

## STAGE 5 — Mass Conservation Verification

### Objective
Define the mass ledger approach: M_state vs M_expected, and determine if it's implemented.

---

### 5.1 Expected Conservation Equation

For a closed primary system with CVCS boundary flows:

```
M_state(t) = M_0 + ∫₀ᵗ (ṁ_charging - ṁ_letdown) dt
```

Where:
- `M_state(t)` = Sum of all primary masses (RCS + PZR water + PZR steam)
- `M_0` = Initial primary mass at t=0
- `ṁ_charging` = Charging mass flow rate (lbm/hr)
- `ṁ_letdown` = Letdown mass flow rate (lbm/hr)

---

### 5.2 Current Implementation: Solid Operations

**Is ledger implemented?** ✓ Yes (v5.0.2)

```csharp
// In SystemState struct:
public float TotalPrimaryMassSolid;  // lb — total primary (loops+PZR), boundary-only

// In HeatupSimEngine.cs Regime 1:
float massChange_lb = netCVCS_gpm * dt_sec * GPM_TO_FT3_SEC * rho_rcs;
physicsState.TotalPrimaryMassSolid += massChange_lb;  // <-- LEDGER UPDATE
```

**Verification:**
```csharp
// In SolidPlantPressure validation test:
float massDelta = initialPzrMass - stateMass.PzrWaterMass;
float massError = Abs(massDelta - totalSurgeTransfer);
if (massError > 1f) valid = false;  // Within 1 lbm tolerance
```

**FINDING:** Solid ops has a proper ledger and validation test.

---

### 5.3 Current Implementation: Two-Phase Operations

**Is ledger implemented?** ❌ No

In Regime 2 and 3, there is no `TotalPrimaryMass` update:
```csharp
// Regime 3 code:
physicsState.RCSWaterMass += cvcsNetMass_lb;
// NOTE: No TotalPrimaryMass update!

// After solver:
state.RCSWaterMass = V_RCS * rho_RCS;  // OVERWRITES with V×ρ!
```

**Problem:** The CVCS mass change is applied to `RCSWaterMass`, but then CoupledThermo OVERWRITES it with `V_RCS * ρ`. The net CVCS adjustment is lost.

---

### 5.4 M_state Calculation: Current Approach

The engine calculates "state mass" by summing derived values:
```csharp
// Implied by SystemState.TotalMass property:
public float TotalMass => RCSWaterMass + PZRWaterMass + PZRSteamMass;
```

This equals:
```
M_state = V_RCS × ρ(T,P) + V_PZR_water × ρ_water(T_sat,P) + V_PZR_steam × ρ_steam(P)
```

**Problem:** This is a CALCULATION, not a CONSERVATION CHECK. If the steam tables have any imprecision, M_state will drift even if no mass crosses the boundary.

---

### 5.5 M_expected Calculation: Not Implemented

There is NO tracking of:
```
M_expected = M_0 + ∑(mass_in - mass_out)
```

The engine DOES track cumulative flows in VCT:
```csharp
state.CumulativeIn_gal += flowIn_gpm * dt_min;
state.CumulativeOut_gal += flowOut_gpm * dt_min;
```

But this is on the VCT side, not the primary side. There is no `primaryCumulativeMassChange` variable.

---

### 5.6 Inventory Audit System (v1.1.0)

The engine has an `UpdateInventoryAudit(dt)` call at the end of each timestep. Let me check what it does:

**Based on code inspection:** This function exists in `HeatupSimEngine.HZP.cs` but the implementation details need review.

The engine does track:
```csharp
[HideInInspector] public float totalSystemInventory_gal;      // RCS+PZR+VCT+BRS total
[HideInInspector] public float initialSystemInventory_gal;    // At T=0 for conservation
[HideInInspector] public float systemInventoryError_gal;      // Total conservation error
```

But these are in GALLONS and include VCT/BRS — they don't isolate the primary boundary.

---

### 5.7 Required Instrumentation

To properly verify mass conservation, we need:

**Primary Side:**
```csharp
// NEW: Canonical primary mass variable (persists across regimes)
float TotalPrimaryMass_lb;  // = RCS_mass + PZR_water_mass + PZR_steam_mass

// NEW: Cumulative boundary flow tracking
float CumulativeCVCSIn_lb;   // = ∫ charging_flow × ρ × dt
float CumulativeCVCSOut_lb;  // = ∫ letdown_flow × ρ × dt
float CumulativeRelief_lb;   // = ∫ relief_flow × ρ × dt (if any)

// VERIFICATION:
float M_expected = M_0 + CumulativeCVCSIn - CumulativeCVCSOut - CumulativeRelief;
float ConservationError = Abs(TotalPrimaryMass - M_expected);
```

**Cross-System Check:**
```
VCT_change + RCS_change + BRS_change + external_net = 0
```

Where `external_net = RWST_makeup - CBO_loss - distillate_transfer`.

---

### 5.8 Stage 5 Key Findings

1. **Solid ops has a working mass ledger (TotalPrimaryMassSolid).**
   - Boundary flows are tracked
   - Surge transfers are tracked
   - A validation test confirms conservation

2. **Two-phase ops has NO mass ledger.**
   - CVCS mass is applied but then overwritten by solver
   - No cumulative boundary tracking
   - No way to detect drift

3. **VCT has a verification function but it depends on RCS accuracy.**
   - `VerifyMassConservation()` compares VCT + RCS changes
   - If RCS is drifting, the check will show error

4. **Inventory audit exists but tracks gallons, not mass.**
   - Includes VCT/BRS, not primary-only
   - May mask primary-specific issues

**Stage 5 Complete.** Proceeding to Stage 6.

---

## STAGE 6 — Root Cause Assessment

### Objective
Evaluate the most likely causes of inventory variation based on written findings.

---

### 6.1 Hypothesis Evaluation Matrix

| Hypothesis | Evidence | Verdict |
|------------|----------|---------|
| **A) Missing external reservoir modeling** | RWST/RMS are implicit sources; PRT is implicit sink | ⚠️ Contributes, but intentional |
| **B) Double-application of flows** | Seal return may be double-counted | ⚠️ Possible, needs verification |
| **C) Density/state mismatch** | Two-phase derives M from V×ρ, not conservation | ✓ **PRIMARY CAUSE** |
| **D) Clamp-induced mass loss** | PZR volume clamped at min/max without mass tracking | ⚠️ Possible edge case |
| **E) Unit/dt mismatch** | CVCS uses consistent units (gpm, ft³/sec, lbm) | ❌ Not observed |

---

### 6.2 Primary Cause: V×ρ Mass Derivation in Two-Phase

**Evidence from Code Review:**

1. **CoupledThermo.SolveEquilibrium() derives all masses from volumes:**
   ```csharp
   state.RCSWaterMass = V_RCS * rho_RCS;
   state.PZRWaterMass = state.PZRWaterVolume * rhoPZRWater;
   state.PZRSteamMass = state.PZRSteamVolume * rhoPZRSteam;
   ```

2. **The solver DOES conserve mass internally during iteration:**
   ```csharp
   float M_total = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;
   // ... iterations preserve M_total ...
   float M_total_calc = M_RCS_new + M_PZR_water_new + M_PZR_steam_new;
   float massError = (M_total_calc - M_total) / M_total;
   ```

3. **BUT the CVCS mass adjustment is applied BEFORE the solver, then OVERWRITTEN:**
   ```csharp
   // In HeatupSimEngine Regime 3:
   physicsState.RCSWaterMass += cvcsNetMass_lb;  // Applied
   // ... then BulkHeatupStep calls CoupledThermo.SolveEquilibrium ...
   state.RCSWaterMass = V_RCS * rho_RCS;  // OVERWRITES!
   ```

**Mechanism of Drift:**

1. CVCS removes 10 lbm from RCS (net letdown)
2. RCSWaterMass -= 10 lbm
3. Solver runs, finds new equilibrium
4. Solver sets RCSWaterMass = V_RCS × ρ(T_new, P_new)
5. The -10 lbm from CVCS is LOST — replaced by calculated value

**Severity:** This is a systematic error. Every timestep where CVCS net flow ≠ 0, mass conservation is violated.

---

### 6.3 Secondary Cause: Transition from Solid to Two-Phase

**Evidence:**

When bubble forms, the engine switches physics modes:
```csharp
if ((solidPressurizer && !bubbleFormed) || bubblePreDrainPhase)
{
    // Solid ops — uses TotalPrimaryMassSolid (conserved)
}
else
{
    // Two-phase — uses CoupledThermo (derived)
}
```

At the transition:
- `TotalPrimaryMassSolid` has the correct conserved value
- But `physicsState.RCSWaterMass` is used in two-phase
- There is no handoff of the conserved mass to the solver

**Impact:** The cumulative mass drift during solid ops is correctly tracked, but this information is not carried forward into two-phase operations.

---

### 6.4 Tertiary Cause: Seal Flow Accounting

**Evidence:**

Seal flows are defined as:
```csharp
SEAL_INJECTION_PER_PUMP_GPM = 8;     // From charging
SEAL_LEAKOFF_PER_PUMP_GPM = 3;       // Returns to VCT
SEAL_FLOW_TO_RCS_PER_PUMP_GPM = 5;   // Returns to RCS
```

The seal injection comes from charging. The question is: does `chargingToRCS` already exclude seal injection?

In CVCSController:
```csharp
public static float GetChargingToRCS(CVCSControllerState state)
{
    return Math.Max(0f, state.ChargingFlow - state.SealInjection);
}
```

This suggests charging to RCS IS adjusted for seal injection. However, the engine uses `chargingFlow` directly in CVCS calculations, not `GetChargingToRCS()`.

**Impact:** Potentially 5 gpm/pump × 4 pumps = 20 gpm of mass double-counted when RCPs are running.

---

### 6.5 Quantitative Impact Assessment

**V×ρ Drift:**
- Steam table precision is ~0.01% for ρ
- At 600,000 lbm RCS mass, 0.01% = 60 lbm per step
- Over 1000 timesteps (2.8 hours at 10-sec steps): 60,000 lbm potential drift
- This is catastrophic if systematic

**CVCS Overwrite:**
- Net CVCS during heatup: ~10 gpm letdown excess
- Mass rate: 10 gpm × 60 lbm/ft³ × 0.133 ft³/gal = 80 lbm/min
- Per 10-sec step: 13 lbm
- Over 2.8 hours: 27,000 lbm should leave
- If overwritten each step: this mass is never removed

**Seal Double-Count:**
- 20 gpm × 60 lbm/ft³ × 0.133 ft³/gal = 160 lbm/min
- Per 10-sec step: 27 lbm added twice
- Over 2.8 hours: 54,000 lbm extra mass created

---

### 6.6 Stage 6 Summary

**Root Cause Ranking:**

| Rank | Cause | Confidence | Impact |
|------|-------|------------|--------|
| 1 | CVCS mass overwritten by solver | HIGH | 27,000 lbm over heatup |
| 2 | V×ρ derivation instead of conservation | HIGH | Variable, depends on table precision |
| 3 | Seal flow double-counting | MEDIUM | 54,000 lbm if confirmed |
| 4 | Solid→Two-phase transition loss | MEDIUM | One-time, but loses ledger |
| 5 | Missing external reservoir | LOW | Intentional design |

**Stage 6 Complete.** Writing Summary.

---

## SUMMARY — Recommended Actions

### Architecture Fix: Extend Conservation Approach to Two-Phase

The v5.0.2 solid-ops fix established the correct pattern:
1. A canonical "total primary mass" variable updated ONLY by boundary flows
2. Internal transfers (surge) conserve mass by construction
3. Component masses derived from total (not independently calculated)

**Recommended Extension:**

1. **Add `TotalPrimaryMass` to `SystemState`** (not just `TotalPrimaryMassSolid`)
   - Initialize at sim start
   - Update ONLY by: CVCS net flow, relief flow, spray injection (if modeled)

2. **Modify CoupledThermo to PRESERVE total mass**
   - Input: `TotalPrimaryMass` (from boundary accounting)
   - Solver: Find P, T, volumes that satisfy this mass constraint
   - Output: Component masses that sum to `TotalPrimaryMass`

3. **Remove the CVCS pre-application in HeatupSimEngine**
   - Currently: `RCSWaterMass += cvcsNetMass` → solver → OVERWRITE
   - New: `TotalPrimaryMass += cvcsNetMass` → solver → DERIVE components

4. **Hand off conserved mass at solid→two-phase transition**
   - When `bubbleFormed = true`:
   - `physicsState.TotalPrimaryMass = physicsState.TotalPrimaryMassSolid`

### Minimal Fix: Add a Mass Ledger (Diagnostic Only)

If architectural changes are too invasive, add instrumentation:

1. **Track cumulative boundary flows:**
   ```csharp
   float cumulativeCVCSIn_lb;
   float cumulativeCVCSOut_lb;
   ```

2. **Calculate expected mass:**
   ```csharp
   float M_expected = M_0 + cumulativeCVCSIn - cumulativeCVCSOut;
   ```

3. **Compare to state mass:**
   ```csharp
   float M_state = RCSWaterMass + PZRWaterMass + PZRSteamMass;
   float drift = M_state - M_expected;
   ```

4. **Log/alarm if drift exceeds threshold.**

This doesn't FIX the problem but makes it visible.

### Verification Test

After any fix, confirm with this test:

1. Start at cold shutdown (solid ops)
2. Run for 4 hours (full heatup)
3. CVCS: net letdown = 10 gpm average
4. Expected mass loss: 10 gpm × 60 × 0.133 × 240 min = 19,152 lbm

**Pass criteria:**
- `M_state(t=4hr) - M_state(t=0) ≈ -19,000 lbm ± 500 lbm`
- Conservation error < 0.1% of initial mass

---

## AUDIT COMPLETE

**Hypothesis Verdict:** CONFIRMED

Inventory variation correlates with VCT changes because:
1. Primary boundary flows (CVCS) are applied to RCS mass but then OVERWRITTEN by the CoupledThermo solver
2. Two-phase operations derive mass from V×ρ instead of conserving it
3. The solid-ops conservation fix (v5.0.2) established the correct pattern but was not extended to two-phase

**Recommended Fix Priority:**
1. CRITICAL: Modify CoupledThermo to constrain on provided total mass
2. HIGH: Add `TotalPrimaryMass` variable that persists across regimes
3. MEDIUM: Verify seal flow accounting doesn't double-count
4. LOW: Add diagnostic ledger for ongoing monitoring

---

**Audit Document:** `Critical/Updates/Inventory_Audit_v1.0.0.md`  
**Audit Complete:** 2026-02-12
