# UPDATE v1.0.5.1 — Bug #2: Missing VCT Mass Conservation Tracking in Two-Phase Operations

**Date:** 2026-02-06  
**Version:** 1.0.5.1  
**Type:** Minor Revision (bug fix)  
**Backwards Compatible:** Yes (no API changes)  
**Reference:** HANDOVER_PZR_BubbleFormation_and_RCP_Bugs.md — Bug #2

---

## Summary

Added missing `VCTPhysics.AccumulateRCSChange()` call for two-phase (bubble exists) operations. Previously, RCS inventory changes from CVCS net charging/letdown flow were only tracked during solid plant operations, causing mass conservation error to explode from 1.46 gal (PASS) to 649 gal (FAIL) when RCPs started.

## Root Cause

The comment at the VCT update section stated "AccumulateRCSChange() is called EXPLICITLY by each operational branch" — but this was only true for the solid pressurizer branch (line ~750). Neither the Phase 1 (bubble exists, no RCPs) nor Phase 2 (RCPs running) branches performed the RCS mass update or called AccumulateRCSChange. When RCPs started and CVCS drove 82+ gpm net charging with letdown isolated, this untracked flow accumulated as a phantom mass conservation error.

## Changes

### HeatupSimEngine.cs

**Added:** RCS inventory update block in common CVCS section (after PI controller, before VCT update) for two-phase operations:

- Calculates `netCVCS_gpm = chargingFlow - letdownFlow`
- Converts to mass change: `massChange_lb = netCVCS × dt × GPM_TO_FT3_SEC × ρ_rcs`
- Updates `physicsState.RCSWaterMass` 
- Feeds RCS change to VCT conservation tracking via `AccumulateRCSChange()`
- Guarded by `if (!solidPressurizer && bubbleFormed)` to avoid double-counting with solid branch

**Updated:** Comment at VCT update to accurately document where AccumulateRCSChange is called from both branches.

## Physics Verification

The fix mirrors the exact pattern used in the solid plant branch (lines ~743-754):
```
netCVCS_gpm → massChange_lb → physicsState.RCSWaterMass += → AccumulateRCSChange()
```

Sign convention preserved:
- Positive net (charging > letdown) → mass enters RCS → positive RCS change
- Negative net (letdown > charging) → mass leaves RCS → negative RCS change (VCT gains)

## Impact

- Mass conservation error should now remain within tolerance (<10 gal) through RCP start transitions
- VCT level tracking will correctly reflect CVCS flow balance during all operational phases
- No impact on solid plant operations (unchanged)
- No API changes

## Files Changed

| File | Changes |
|------|---------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Added RCS inventory update + AccumulateRCSChange for two-phase ops (~line 1032) |
| `Assets/Documentation/Updates/UPDATE-v1.0.5.1.md` | This changelog |
