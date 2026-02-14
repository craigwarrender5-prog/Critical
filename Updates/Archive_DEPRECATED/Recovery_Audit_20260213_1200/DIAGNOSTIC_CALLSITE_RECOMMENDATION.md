# STAGE 3B — DEAD DIAGNOSTIC RESURRECTION PLAN
## Recovery Audit 2026-02-13

---

## CONTEXT

`UpdatePrimaryMassLedgerDiagnostics()` (Logging.cs:470-583) is a fully-implemented independent mass conservation cross-check that is **never called**. This document specifies how to resurrect it correctly, given the findings from Stage 3A.

**Prerequisite:** This plan assumes ISSUE-0001 (canonical mass mode activation) will be addressed first. The diagnostic's value depends on the ledger being meaningful.

---

## 1. RECOMMENDED CALL SITE

### Location: StepSimulation(), Section 9 — after UpdateInventoryAudit()

**File:** `HeatupSimEngine.cs`
**Insertion point:** After line 1513 (`UpdateInventoryAudit(dt)`) and before `UpdatePrimaryMassLedgerDiagnostics()` comment block.

**Exact placement:**
```
// Section 9: Inventory Audit (line 1513)
UpdateInventoryAudit(dt);
UpdatePrimaryMassLedgerDiagnostics();   // ← INSERT HERE
```

### Rationale for This Position

| Consideration | Why This Location |
|--------------|-------------------|
| **After solver** | All Regime 1/2/3 physics are complete. PZR and RCS masses are in their final post-solver state. |
| **After CVCS** | `UpdateCVCSFlows()` runs at Section 5 (line 1492). By Section 9, all CVCS boundary flows are finalized and RCSWaterMass reflects the net CVCS effect. |
| **After bubble formation** | `UpdateBubbleFormation()` runs at Section 4 (line 1487). Mass transfers during DRAIN are complete before the diagnostic runs. |
| **After UpdateInventoryAudit** | The inventory audit (Section 9, line 1513) is a separate conservation check at the system level (RCS+PZR+VCT+BRS). The primary mass ledger diagnostic operates at the primary level only (RCS+PZR). Running it immediately after keeps all diagnostics together. |
| **Before time advance** | `simTime += dt` is at Section 6 (line 1494) — wait, this is BEFORE Section 9. Actually, simTime is already advanced. This is fine — the diagnostic uses state, not time. |

### Alternative: Inside UpdateInventoryAudit()

An alternative is to call `UpdatePrimaryMassLedgerDiagnostics()` from within `UpdateInventoryAudit()` at Logging.cs:435 (just before the method returns). This keeps all mass diagnostics co-located. The trade-off is slightly reduced separation of concerns — the inventory audit would own both system-level and primary-level diagnostics.

**Recommendation:** Call from StepSimulation directly (not nested) for transparency and debuggability.

---

## 2. REGIME-AWARE COMPARISONS

The diagnostic already has regime-aware logic (Logging.cs:474-489 for solid, 492-583 for two-phase). However, it needs corrections based on Stage 3A findings:

### Solid Operations (solidPressurizer && !bubbleFormed)

**Current behavior (Logging.cs:474-489):** Correct.
- `primaryMassLedger_lb = TotalPrimaryMass_lb` (which = `TotalPrimaryMassSolid` via line 1085)
- `primaryMassComponents_lb = TotalPrimaryMassSolid`
- Drift = 0 by construction

No changes needed.

### Two-Phase Operations (post-bubble)

**Current behavior (Logging.cs:492-583):** Partially broken.

**What it compares:**
1. `primaryMassLedger_lb = TotalPrimaryMass_lb` — **PROBLEM**: This field is stale/frozen (ISSUE-0002)
2. `primaryMassComponents_lb = RCSWaterMass + PZRWaterMass + PZRSteamMass` — Correct (live solver output)
3. `primaryMassExpected_lb = InitialPrimaryMass_lb + CumulativeCVCSIn_lb - CumulativeCVCSOut_lb - CumulativeReliefMass_lb` — **PROBLEM**: All cumulative fields are always 0f (ISSUE-0003)

**Corrections needed BEFORE resurrection:**

| Fix | What | Why |
|-----|------|-----|
| **Fix A** (depends on ISSUE-0001) | If canonical mode activated: ledger is maintained by solver, so comparison 1 becomes valid | Ledger tracks actual mass |
| **Fix B** (if ISSUE-0001 deferred) | Replace ledger read with live component sum for now: `primaryMassLedger_lb = componentSum` and skip the drift calculation | Avoids false alarms from stale ledger |
| **Fix C** (ISSUE-0003) | Increment `CumulativeCVCSIn_lb` / `CumulativeCVCSOut_lb` in CVCS pre-application blocks, and `CumulativeReliefMass_lb` in relief valve code | Makes expected-mass calculation valid |

### Minimum Viable Resurrection (without ISSUE-0001)

If canonical mode activation is deferred, the diagnostic can still provide value by checking:

1. **Component-sum stability:** Track `componentSum` between timesteps. Large single-step jumps indicate solver instability.
   - Threshold: `|Δ(componentSum)| / dt > 10,000 lbm/hr` → WARNING
2. **Component-sum vs system-level audit:** Compare primary `componentSum` against the inventory audit's `RCS_Mass_lbm + PZR_Water_Mass_lbm + PZR_Steam_Mass_lbm`. These should agree exactly (both read from physicsState). If they disagree, there's a code path that modified one but not the other.
3. **Boundary error tracking:** With Fix C, the expected-mass calculation becomes valid and detects whether CVCS boundary flows are correctly reflected in the solver output.

---

## 3. EXPECTED SIDE EFFECTS

### UI Fields Populated

The diagnostic writes to these engine fields (all currently at default values):

| Field | Expected Behavior | Consumed By |
|-------|-------------------|-------------|
| `primaryMassLedger_lb` | Current ledger value | Display only |
| `primaryMassComponents_lb` | RCS + PZR_water + PZR_steam | Display only |
| `primaryMassDrift_lb` | Ledger - Components | Display, alarm |
| `primaryMassDrift_pct` | |drift| / components × 100 | Display, alarm |
| `primaryMassExpected_lb` | Initial + cumulative boundary | Display |
| `primaryMassBoundaryError_lb` | |Ledger - Expected| | Display |
| `primaryMassConservationOK` | drift ≤ 0.1% | Status indicator |
| `primaryMassAlarm` | drift > 1.0% | Alarm flag |
| `primaryMassStatus` | "OK" / "WARN: ..." / "ALARM: ..." | Status string |

### Alarm Edges Logged

| Transition | Severity | Message |
|-----------|----------|---------|
| OK → WARNING | ALERT | "PRIMARY MASS DRIFT WARNING: {lb} ({pct}%)" |
| OK/WARN → ALARM | ALARM | "PRIMARY MASS CONSERVATION ALARM: drift={lb}lb ({pct}%)" + detail line |
| WARNING → OK | INFO | "PRIMARY MASS DRIFT CLEARED: now {pct}%" |

### Alignment with Existing UI (TabValidation)

The TabValidation.cs checks (UI-01, UI-02) use different fields:
- **UI-01:** `massError_lbm` — system-level conservation (CVCS.cs:300), thresholds 100/500 lbm
- **UI-02:** `massConservationError` — VCT flow imbalance (VCTPhysics.cs:346), thresholds 10/50 gal

The resurrected diagnostic adds a **third** check at the primary level:
- **New:** `primaryMassDrift_pct` — ledger-vs-components drift, thresholds 0.1%/1.0%

These are complementary:
- `massError_lbm` detects system-wide mass creation/destruction
- `massConservationError` detects VCT/RCS accounting imbalance
- `primaryMassDrift_pct` detects solver-vs-ledger divergence (the gap that ISSUE-0001 creates)

**Recommendation:** Add a third check row to `DrawValidationChecks()` in TabValidation.cs:
```
DrawCheckRowThreeState(ref y, x, w, "Primary Ledger Drift",
    engine.primaryMassDrift_lb, 100f, 1000f,
    $"Drift: {engine.primaryMassDrift_pct:F3}%");
```

---

## 4. DEPENDENCY CHAIN

The correct order of fixes for the full conservation architecture:

```
ISSUE-0001 (activate canonical mode)
    ├── Enables TotalPrimaryMass_lb as real authority
    ├── Solver enforces R5 (RCS = Total - PZR)
    └── Ledger becomes meaningful
         │
         ├── ISSUE-0002 (auto-resolved if solver updates ledger)
         │
         ├── ISSUE-0003 (increment boundary accumulators)
         │   └── Makes expected-mass check valid
         │
         └── Resurrect UpdatePrimaryMassLedgerDiagnostics()
             ├── Drift check: ledger vs components (should be ~0)
             ├── Boundary check: ledger vs expected (detects missing flows)
             └── UI: third validation row in TabValidation
```

**Minimum path:** ISSUE-0001 → ISSUE-0003 → Resurrect diagnostic → Add UI row.
**Quick win (no code changes to solver):** Just call the diagnostic now. It will immediately alarm because the ledger is stale, which is actually useful: it proves ISSUE-0001 and ISSUE-0002 exist at runtime. Use this as a smoke test before fixing the canonical mode.
