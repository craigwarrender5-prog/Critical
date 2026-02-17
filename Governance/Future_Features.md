# Future Features

**Last Updated:** 2026-02-17

This document tracks planned features that are explicitly deferred from current implementation plans. Items are added here when an IP identifies work as "out of scope" or "planned for future release."

---

## Dashboard & UI

### In-Game Help System (F1)
**Source:** IP-0041 (deferred), IP-0042 Section 10  
**Priority:** Medium  
**Description:** Context-sensitive help overlay accessible via F1 key. Would display parameter definitions, normal ranges, and procedural guidance based on current plant state.

### Scenario Selector
**Source:** IP-0041 (deferred), IP-0042 Section 10  
**Priority:** Medium  
**Description:** Pre-simulation screen allowing selection of different initial plant conditions (e.g., cold shutdown, hot shutdown, mid-heatup) for training scenarios.

### Multi-Tab Validation Dashboard Redesign
**Source:** IP-0040, IP-0041  
**Priority:** Low (superseded by IP-0042)  
**Description:** Original uGUI-based dashboard redesign. Superseded by UI Toolkit approach in IP-0042.

---

## Physics & Modeling

### Reactor Core Parameters
**Source:** IP-0042 Section 10  
**Priority:** High (Phase 2)  
**Description:** Modeling of reactor power, decay heat, rod worth, reactivity breakdown (moderator temp feedback, Doppler, boron worth). Currently not modeled in HeatupSimEngine.

### Loop-by-Loop Temperatures
**Source:** IP-0042 Section 10  
**Priority:** Medium  
**Description:** Per-loop (A/B/C/D) hot leg and cold leg temperatures. Engine currently calculates aggregate T_hot/T_cold only.

### Charging/Letdown Line Temperatures
**Source:** IP-0042 Section 10  
**Priority:** Low  
**Description:** Temperature instrumentation for CVCS charging and letdown lines. Would enable thermal mixing calculations.

### VCT Gas Blanket Pressure
**Source:** IP-0042 Section 10  
**Priority:** Low  
**Description:** Separate tracking of VCT nitrogen blanket pressure vs. liquid level pressure.

### Feedwater/Steam Flow Modeling
**Source:** IP-0042 Section 10  
**Priority:** High (Phase 2)  
**Description:** Complete SG secondary system with feedwater makeup to balance mass exiting via steam dump. Currently steam exits but no feedwater replaces it.

---

## Procedural Enhancements

### 200°F Temperature Hold
**Source:** Project memory  
**Priority:** Medium  
**Description:** Procedural temperature hold at 200°F during heatup to align with NRC Mode 5 to Mode 4 transition requirements.

### Expanded Operator Screens
**Source:** Project memory  
**Priority:** Medium  
**Description:** Additional operator interface screens beyond current Reactor and RCS screens. Would include dedicated CVCS, Electrical, and Auxiliary screens.

---

## Format

When adding items, use this template:

```markdown
### Feature Name
**Source:** IP-XXXX Section Y (or conversation reference)  
**Priority:** High / Medium / Low  
**Description:** Brief description of the feature and why it was deferred.
```

---

*Document created as part of IP-0042 preparation*
