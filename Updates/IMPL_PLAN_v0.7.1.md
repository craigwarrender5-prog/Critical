# IMPLEMENTATION PLAN v0.7.1
## Heatup Simulator Bug Fixes

**Version Type:** Patch (bug fixes only, no new features)  
**Previous Version:** 0.7.0 (Dashboard UI redesign)  
**Date:** 2026-02-07

---

## PROBLEM STATEMENT

Four critical bugs have been identified in the v0.7.0 heatup simulator implementation:

### Issue #1: T_AVG Calculation - Wrong Definition
**Symptom:** T_AVG is displaying higher than both T_HOT and T_COLD, and rising while T_HOT and T_COLD are falling/flat.

**Root Cause Analysis (CORRECTED):**
The current implementation incorrectly calculates T_avg as a volume-weighted average INCLUDING the pressurizer:
```csharp
T_avg = (T_rcs * PlantConstants.RCS_WATER_VOLUME + T_pzr * pzrWaterVolume) /
        (PlantConstants.RCS_WATER_VOLUME + pzrWaterVolume);
```

This is **WRONG** per Westinghouse definition. Looking at PlantConstants.cs line 65:
```csharp
/// <summary>Average coolant temperature in °F at 100% power</summary>
public const float T_AVG = 588.5f;  // = (T_HOT + T_COLD) / 2 = (619 + 558) / 2
```

**The pressurizer is NOT part of T_avg!**

This causes the observed problem:
1. **Phase 1 (no RCPs):** T_pzr is being heated (rises), T_rcs stays flat → T_avg incorrectly rises above both T_hot and T_cold
2. **Violates industry standard** where T_avg = (T_hot + T_cold) / 2
3. **Creates nonsensical parameter** that doesn't represent any physical location

**Westinghouse Definition (from FSAR, Tech Spec 3.4.1, RPS inputs):**
- T_avg is the **loop average ONLY**, excluding pressurizer
- T_avg = (T_hot + T_cold) / 2
- T_pzr is tracked separately (can vary independently during transients)
- T_avg is used for reactor protection and core reactivity feedback

**Expected Behavior (After Fix):**
- **Phase 1 (no RCPs):** T_hot ≈ T_cold ≈ T_rcs → T_avg ≈ T_rcs (stays flat, correct!)
- **Phase 2 (RCP ramp):** T_avg = (T_hot + T_cold) / 2 (always between them)
- **At 100% power:** T_avg = (619 + 558) / 2 = 588.5°F (matches PlantConstants)
- **T_pzr tracks independently** and does NOT influence T_avg

### Issue #2: Time Acceleration Hotkeys Not Working
**Symptom:** The `+` and `-` hotkeys no longer change the time scale.

**Root Cause Analysis:**
In `HeatupValidationVisual.cs` Update() method (lines ~115-125), code only checks digit keys 1-5. There is NO code for `minusKey` or `equalsKey`.

**Expected Behavior:**
- `+` (or `=`) increments time acceleration (wrap: 1x → 2x → 4x → 8x → 10x → 1x)
- `-` decrements time acceleration (wrap: 1x → 10x → 8x → 4x → 2x → 1x)
- Digit keys 1-5 continue as direct selection (already working)

### Issue #3: Incorrect Logging Interval
**Symptom:** Logs saved every 30 minutes instead of every 15 minutes.

**Root Cause:** Line ~184 in HeatupSimEngine.cs: `if (logTimer >= 0.5f)` = 30 min

**Expected:** `if (logTimer >= 0.25f)` = 15 min for greater detail

### Issue #4: Missing Log Fields
**Symptom:** New v0.6.0/v0.7.0 parameters not in interval logs.

**Missing Fields:**
- CVCS: `totalCCPOutput`, `divertFraction`, `orificeLetdownFlow`, `rhrLetdownFlow`
- RCP Ramp: `rcpContribution.TotalFlowFraction` (α), per-pump flow fractions/heat/status
- Seal flows: per-pump breakdown (currently only aggregate)

---

## PROPOSED FIXES

### Fix #1: Correct T_AVG to Westinghouse Definition

**Implementation:**
Replace the current T_avg calculation with the simple loop average:

```csharp
// Step 1: Calculate T_HOT and T_COLD FIRST (move this BEFORE T_avg)
var loopTemps = LoopThermodynamics.CalculateLoopTemperatures(
    T_rcs, pressure, rcpCount, rcpHeat, T_pzr);
T_hot = loopTemps.T_hot;
T_cold = loopTemps.T_cold;

// Step 2: T_avg is simple average of loop temperatures (Westinghouse definition)
// Per PlantConstants.T_AVG = 588.5°F = (619 + 558) / 2
// The pressurizer is NOT included in T_avg
T_avg = (T_hot + T_cold) / 2.0f;
```

**Physical Behavior After Fix:**
- **Phase 1 (no RCPs):** T_hot ≈ T_cold ≈ T_rcs → **T_avg ≈ T_rcs** (stays flat - CORRECT!)
- **Phase 2 (RCP ramp):** T_avg always between T_hot and T_cold (CORRECT!)
- **Phase 3 (full RCPs):** T_avg = (T_hot + T_cold) / 2 (Westinghouse definition)
- **T_pzr is independent** - no longer affects T_avg

**Validation:**
- T_COLD ≤ T_AVG ≤ T_HOT (always, by definition)
- At 100% power: (619 + 558) / 2 = 588.5°F ✓
- At HZP: T_avg ≈ 557°F ✓
- During Phase 1: T_avg ≈ T_rcs (within 1-2°F from natural circ ΔT) ✓

**Risk:** LOW - Display only, simpler formula, aligns with industry standard

### Fix #2: Add Time Acceleration Hotkeys

Add after existing digit key checks in `HeatupValidationVisual.cs` Update():
```csharp
if (engine != null && engine.isRunning)
{
    // Increment with + or = key
    if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
    {
        int newIndex = (engine.currentSpeedIndex + 1) % 5;  // Wrap: 0→1→2→3→4→0
        engine.SetTimeAcceleration(newIndex);
    }
    
    // Decrement with - key
    if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
    {
        int newIndex = (engine.currentSpeedIndex - 1 + 5) % 5;  // Wrap: 4→3→2→1→0→4
        engine.SetTimeAcceleration(newIndex);
    }
}
```

**Risk:** MINIMAL - Pure UI, uses existing method, no new dependencies

### Fix #3: Correct Logging Interval

In `HeatupSimEngine.cs` line ~184:
```csharp
// OLD: if (logTimer >= 0.5f)  // 30 minutes
// NEW:
if (logTimer >= 0.25f)  // 15 minutes
```

**Impact:** 2x more log files (4/hour vs 2/hour), better granularity for thermal transients

**Risk:** MINIMAL - Only timing threshold, text files are small

### Fix #4: Add Missing Log Fields

Add to `HeatupSimEngine.Logging.cs` SaveIntervalLog():

**Section 1: Enhanced CVCS Flow Breakdown:**
```csharp
sb.AppendLine("  CVCS FLOW BREAKDOWN (Detailed):");
sb.AppendLine($"    Total CCP Output: {totalCCPOutput,10:F1} gpm (before splits)");
sb.AppendLine($"    → To RCS (chg):   {chargingToRCS,10:F1} gpm");
sb.AppendLine($"    → To VCT (div):   {totalCCPOutput - chargingToRCS,10:F1} gpm");
sb.AppendLine($"    Divert Fraction:  {divertFraction,10:F3} (0=all RCS, 1=all VCT)");
sb.AppendLine($"    Letdown (total):  {letdownFlow,10:F1} gpm");
if (!letdownIsolatedFlag)
{
    sb.AppendLine($"    → Via Orifice:    {orificeLetdownFlow,10:F1} gpm");
    sb.AppendLine($"    → Via RHR:        {rhrLetdownFlow,10:F1} gpm");
}
sb.AppendLine($"    Net to RCS:       {chargingToRCS - letdownFlow,10:F1} gpm");
sb.AppendLine();
```

**Section 2: Enhanced RCP Status (replace existing):**
```csharp
sb.AppendLine("  RCP STATUS (Staged Ramp-Up per v0.4.0):");
sb.AppendLine($"    RCPs Running:     {rcpCount,10} / 4");
sb.AppendLine($"    Fully Running:    {rcpContribution.PumpsFullyRunning,10} / {rcpCount}");
sb.AppendLine($"    Coupling Factor:  {rcpContribution.TotalFlowFraction,10:F3} (α: 0=isolated, 1=coupled)");
sb.AppendLine($"    Physics Regime:   {GetPhysicsRegimeString(),10}");

for (int i = 0; i < rcpCount; i++)
{
    float rampTime = simTime - rcpStartTimes[i];
    float flowFrac = rcpContribution.FlowFractions[i];
    string status = flowFrac >= 0.99f ? "RATED" : (flowFrac > 0.01f ? "RAMPING" : "OFF");
    float pumpHeat = flowFrac * PlantConstants.RCP_HEAT_MW_EACH;
    
    sb.AppendLine($"    RCP #{i + 1,-8} {status,-10} Flow: {flowFrac,5:F3}  Heat: {pumpHeat,5:F2} MW  (T+{rampTime,5:F2} hr)");
}

sb.AppendLine($"    Effective Heat:   {effectiveRCPHeat,10:F2} MW (vs {rcpCount * PlantConstants.RCP_HEAT_MW_EACH:F2} MW rated)");
sb.AppendLine($"    Ramp Efficiency:  {(rcpCount > 0 ? effectiveRCPHeat / (rcpCount * PlantConstants.RCP_HEAT_MW_EACH) * 100f : 0f),10:F1} %");
sb.AppendLine();
```

**Section 3: Enhanced Seal Injection Breakdown:**
```csharp
sb.AppendLine($"    Seal Inj (total): {(rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM),10:F1} gpm");
for (int i = 0; i < rcpCount; i++)
    sb.AppendLine($"      → RCP #{i + 1}:     {PlantConstants.SEAL_INJECTION_PER_PUMP_GPM,10:F1} gpm");
sb.AppendLine($"    Seal Ret (total): {(rcpCount * PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM),10:F1} gpm");
for (int i = 0; i < rcpCount; i++)
    sb.AppendLine($"      → RCP #{i + 1}:     {PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM,10:F1} gpm");
```

**Section 4: Helper Method** (add to HeatupSimEngine.cs):
```csharp
string GetPhysicsRegimeString()
{
    if (rcpCount == 0)
        return "REGIME 1 (Isolated)";
    else if (!rcpContribution.AllFullyRunning)
        return $"REGIME 2 (Blended α={rcpContribution.TotalFlowFraction:F2})";
    else
        return "REGIME 3 (Coupled)";
}
```

**Risk:** MINIMAL - Pure logging, all fields exist, no physics impact

---

## VALIDATION TESTS

### Test 1: T_AVG Consistency
- [ ] T_COLD ≤ T_AVG ≤ T_HOT (always)
- [ ] Phase 1: T_avg ≈ T_rcs (stays flat, not rising)
- [ ] At 100% power: T_avg = 588.5°F
- [ ] No anomalous jumps or reversals

### Test 2: Hotkey Functionality
- [ ] `+` cycles 1x → 2x → 4x → 8x → 10x → 1x
- [ ] `-` cycles 1x → 10x → 8x → 4x → 2x → 1x
- [ ] Digit keys 1-5 still work
- [ ] Event log shows "TIME WARP" messages

### Test 3: Logging Interval
- [ ] Logs at T+0.00, T+0.25, T+0.50, T+0.75, ... hr
- [ ] 8 files per sim-hour (15 min intervals)

### Test 4: Log Field Completeness
- [ ] CVCS Flow Breakdown with totalCCPOutput, divertFraction
- [ ] RCP Status with per-pump flow/heat/status
- [ ] Seal Injection per-pump breakdown
- [ ] Physics Regime indicator

---

## FILES TO BE MODIFIED

1. **HeatupSimEngine.cs** - T_avg formula (line ~720), logging interval (line ~184), GetPhysicsRegimeString()
2. **HeatupValidationVisual.cs** - +/- hotkeys in Update()
3. **HeatupSimEngine.Logging.cs** - Enhanced log sections

---

## WESTINGHOUSE VALIDATION

**T_AVG Definition (CRITICAL):**
- Per Westinghouse FSAR Section 5.1.2.3: T_avg = (T_hot + T_cold) / 2
- Used for Reactor Protection System (RPS) inputs and core reactivity feedback
- Tech Spec 3.4.1 requires accurate T_avg for pressure/temperature/flow monitoring
- **Pressurizer is explicitly excluded** from T_avg definition

**Logging Interval:**
- NRC HRTD 19.0 requires sufficient resolution for thermal transient documentation
- Tech Spec 3.4.3 requires heatup rate ≤ 50°F/hr
- 15-min intervals provide 4 points/hour for accurate rate calculations
- Industry standard (EPRI AP-1000) uses 10-15 min intervals

---

## SEMANTIC VERSIONING: 0.7.1 (Patch)

- ✅ Backwards-compatible bug fixes only
- ❌ No new features
- ❌ No breaking changes
- ❌ No physics changes (except correcting display calculation)

---

## IMPLEMENTATION STAGING

**Stage 1: T_AVG Fix** (CRITICAL - Fixes primary issue)
**Stage 2: Hotkey Fix** (HIGH - Usability)
**Stage 3: Logging Fixes** (MEDIUM - Data quality)
**Stage 4: Integration Testing** (Full heatup run)

---

## SIGN-OFF REQUIREMENTS

- [ ] User approval of corrected implementation plan
- [ ] v0.7.0 codebase backed up
- [ ] Agreement on stage-by-stage vs all-at-once approach
- [ ] Test criteria acceptable

---

**END OF IMPLEMENTATION PLAN v0.7.1**
