# Startup Boundary and Steam Dump Authoritative Spec

**Status:** CANONICAL INTERNAL SPECIFICATION  
**Created:** 2026-02-17  
**Purpose:** Resolve startup inconsistencies for MSIV/MSIV-bypass roles, steam-dump bridge logic, and SG participation expectations below 350F.

---

## 1) Scope and Authority

This document is the authoritative startup reference for:

1. Main steam boundary valve sequencing during line warming.
2. Steam-dump availability and modulation bridge logic.
3. SG thermodynamic participation through Mode 5/Mode 4 startup.

If any internal document conflicts with this specification, this specification governs.

---

## 2) Canonical Startup Boundary Rule (MSIV vs MSIV Bypass)

### 2.1 Normative Valve Role Definition

1. Main steam line warming shall be performed with **MSIVs closed** and **MSIV bypass valves open**.
2. MSIV bypass valves are used to:
   - Warm downstream main steam piping.
   - Equalize pressure across MSIV disks.
3. MSIVs remain closed during dedicated line-warming states and open only after line warmup and acceptable disk differential-pressure conditions.

### 2.2 Supersession Statement

Any prior internal wording that line warming is initiated by opening MSIVs is superseded by this behavior.

---

## 3) Steam Dump Startup Bridge (C-9 and P-12 Gated)

### 3.1 Interlock and Permissive Contract

1. **C-9 (Condenser Available)** must be satisfied for steam dumps to open.
2. **P-12 (Low-Low Tavg)** blocks steam dumps when active unless deliberately bypassed per operator action.
3. Steam pressure mode selection does not override C-9 or P-12 blocking.

### 3.2 Required Bridge States

1. **Dumps Unavailable**
   - Entry condition: `!C9` OR `(P12 active AND no bypass)`.
   - Behavior: dump valves forced closed; SG pressure follows thermodynamics and boundary-state mass balance.

2. **Dumps Armed (Closed)**
   - Entry condition: `C9` AND `(P12 not blocking OR P12 bypassed)` AND steam-pressure mode selected.
   - Behavior: controller armed; valves remain closed while pressure error is within deadband.

3. **Dumps Modulating (Steam Pressure Mode)**
   - Entry condition: state 2 plus SG pressure above `(setpoint + deadband)`.
   - Behavior: valves modulate to hold steam pressure near setpoint during hot standby, startup, and initial loading.

---

## 4) SG Participation Contract Across Startup Temperatures

1. SGs are thermodynamically active whenever secondary inventory exists.
2. Below ~200F, SG secondary pressure is near atmospheric/N2 conditions with venting and condensation dominant.
3. From ~220F to no-load conditions, SG pressure and steam behavior are active and must be modeled as the pressure-source bridge into steam-dump control.
4. The statement "SGs do not meaningfully participate until >350F" is obsolete and superseded.

---

## 5) Cross-Document Conformance Targets

This authority shall be reflected in:

1. `Technical_Documentation/PWR_Startup_State_Sequence.md`
2. `Technical_Documentation/SG_Startup_Pressure_Bridge_Specification.md`
3. `Technical_Documentation/SG_MODEL_RESEARCH_HANDOFF.md`

