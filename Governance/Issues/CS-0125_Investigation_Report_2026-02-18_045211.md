# CS-0125 Preliminary Investigation Report

**Issue ID:** CS-0125  
**Title:** Rotary selector switch UI control plan viability and integration risk assessment  
**Date:** 2026-02-18T04:52:11Z  
**Status:** DEFERRED (scope clarified; pending resumed investigation)  
**Assigned DP:** DP-0012 (Pressurizer & Startup Control)

---

## Scope

Preliminary viability review of the proposed rotary selector switch implementation plan in:
- `Updates/RotarySelectorSwitch.md`
- `Updates/v2_RotarySelectorSwitch.md` (clarified 2D heater-mode control intent)

---

## Preliminary Viability Opinion

**Verdict: Conditionally viable with scope changes.**

The concept is technically feasible in Unity, but the current plan is not fully aligned with existing operator-screen architecture and control-authority patterns.

---

## Findings

1. **Architecture mismatch risk (medium):**
- The plan proposes a 3D model + RenderTexture + dedicated camera path.
- Existing operator screens are standardized around 2D uGUI mimic controls.
- `Assets/Scripts/UI/RCSPrimaryLoopScreen.cs` explicitly documents a prior migration away from 3D/RenderTexture to enforce this consistency.

2. **Control-authority definition risk (medium):**
- Clarified scope is PZR heater mode control, not simulator/scenario on-off control.
- OFF/AUTO/MANUAL semantics still need explicit mapping to heater authority paths so OFF does not trigger simulation shutdown.

3. **Feature value is valid (positive):**
- A realistic physical-style selector can improve usability and authenticity.
- The plan is structured, staged, and implementable if integrated within existing UI patterns.

---

## Recommended Direction (Preliminary)

1. **Preferred:** Implement a 2D rotary-style control in the existing Pressurizer screen uGUI stack first (no RenderTexture/camera pipeline).
2. Define authoritative PZR-heater control contract before implementation:
- OFF/AUTO/MANUAL semantics
- interaction with existing heater control authority paths
- explicit guarantee that simulation continues while OFF suppresses heater effect.
3. Keep 3D Blender/RenderTexture as optional follow-on, requiring explicit architecture waiver.

---

## Next Step

Promote to full investigation and produce a bounded implementation decision:
- Option A (2D-native) vs Option B (3D-render-texture)
- heater-authority integration contract, regression risks, and acceptance gates.
