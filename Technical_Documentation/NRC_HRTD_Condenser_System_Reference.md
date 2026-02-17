# NRC HRTD Condenser System Reference — Westinghouse 4-Loop PWR

**Source Documents:**
- NRC HRTD Section 7.2 — Condensate and Feedwater System (ML11223A246)
- NRC HRTD Section 11.2 — Steam Dump Control (ML11223A294)
- NRC HRTD Section 6.2 — Cooling Water Systems (ML11221A133)
- EPRI TR-107422-V2 — Thermal Performance Engineers Handbook
- EPRI TR-112819 — Condenser In-Leakage Guideline

**Retrieved:** 2026-02-17  
**Purpose:** Comprehensive condenser system reference for simulation development

---

## 1. Condenser Overview

### 1.1 Main Condenser Configuration

| Parameter | Value | Source |
|-----------|-------|--------|
| Type | Single-pass, three-shell, multipressure, deaerating, surface condenser | HRTD 7.2 |
| Tube material | Titanium | HRTD 7.2 |
| Number of tubes | ~60,000 | HRTD 7.2 |
| Tube diameter | 1.25 in. | HRTD 7.2 |
| Total heat transfer area | ~900,000 ft² | HRTD 7.2 |

### 1.2 Shell Configuration and Pressures (Full Power)

| Shell | Pressure (in. HgA) | Pressure (psia) | T_sat (°F) | Tube Length |
|-------|-------------------|-----------------|-----------|-------------|
| A (Low pressure) | 3.30 | ~1.62 | ~117 | 35 ft |
| B (Intermediate) | 4.00 | ~1.97 | ~126 | 45 ft |
| C (High pressure) | 5.11 | ~2.51 | ~133 | 55 ft |
| Hotwells | 6.73 | ~3.31 | ~141 | — |

**Note:** Shell pressures are dependent on circulating water flow and inlet temperature. The values above represent design full-power conditions.

### 1.3 Multipressure Condenser Benefits

The multipressure design provides improved thermal efficiency:
- Circulating water flows in series: A → B → C shells
- Each shell operates at progressively higher pressure
- Steam passages sized and orificed to maintain appropriate pressures
- Heating steam from shell C supplies intermediate hotwells via crossover piping

---

## 2. C-9 Interlock: Condenser Available

### 2.1 Purpose

The C-9 interlock ("Condenser Available") protects the condenser from overpressure by ensuring adequate heat rejection capacity exists before allowing steam dump operation.

### 2.2 C-9 Logic Requirements

**Both conditions must be satisfied:**

| Condition | Setpoint | Logic |
|-----------|----------|-------|
| Condenser vacuum | > 22 in. Hg (below atmospheric) | 2/2 pressure switches |
| Circulating water pump | At least 1 running | 1/2 breaker status (closed) |

### 2.3 C-9 Actuation and Reset

| Parameter | Value |
|-----------|-------|
| **Actuation (blocks steam dumps)** | Vacuum < 22 in. Hg OR no CW pumps running |
| **Reset (enables steam dumps)** | Vacuum ≥ 22 in. Hg AND ≥ 1 CW pump running |
| **Backpressure trip** | 7.6 in. Hg absolute (~22.4 in. Hg vacuum loss) |

### 2.4 Implementation Notes for Simulator

```
condenser_available = (condenser_vacuum_inHg > 22.0) AND (cw_pump_running_count >= 1)

// When C-9 is FALSE:
// - Steam dump valves are blocked from opening
// - Steam dump control signals are blocked
// - Existing valve positions may be maintained or closed (plant-specific)
```

**Critical:** If condenser is unavailable, steam must be diverted to atmospheric relief valves (10% capacity) or SG safety valves.

---

## 3. Condenser Backpressure/Vacuum Dynamics

### 3.1 Vacuum Fundamentals

| Parameter | Value | Notes |
|-----------|-------|-------|
| Design vacuum | 25-28 in. Hg | Varies with CW temperature |
| Minimum operating vacuum | 22 in. Hg | C-9 interlock setpoint |
| Turbine trip setpoint | ~20 in. Hg | Plant-specific |
| Hotwell temperature (typical) | ~120°F | At design vacuum |

### 3.2 Vacuum Establishment (Startup)

**Equipment:**
- **Hogging air ejectors:** Initial vacuum establishment
  - Capacity: 1,200 scfm (single-stage)
  - Used during startup until vacuum reaches ~15-20 in. Hg
  
- **Main air ejectors:** Maintain vacuum during operation
  - Two 100% capacity units (two-stage)
  - Minimum maintained vacuum: 25 in. Hg

**Startup Sequence:**
1. Start circulating water pumps
2. Establish water flow through condenser tubes
3. Start hogging air ejectors
4. Pull vacuum from atmospheric to ~15-20 in. Hg
5. Transfer to main air ejectors (or vacuum pumps)
6. Continue pulling vacuum to design value
7. C-9 permissive satisfied at >22 in. Hg

### 3.3 Vacuum Response Dynamics

**Factors affecting vacuum:**
1. **Circulating water temperature** — Higher inlet temp = higher backpressure
2. **Circulating water flow rate** — Lower flow = reduced heat rejection
3. **Steam load** — Higher steam flow = higher condenser pressure
4. **Air in-leakage** — Degrades heat transfer, raises pressure
5. **Tube fouling** — Reduces heat transfer coefficient

**Dynamic Response Model:**

For simulation purposes, condenser pressure responds to the energy balance:

```
dP_cond/dt = f(Q_steam_in - Q_cw_removed - Q_losses)

Where:
Q_steam_in = steam dump flow × latent heat
Q_cw_removed = m_cw × Cp × ΔT_cw
Q_losses = air removal, radiation, etc.
```

**Time Constants:**
| Condition | Response Time |
|-----------|--------------|
| Loss of one CW pump | Pressure rises over 30-60 seconds |
| Loss of all CW pumps | Pressure rises rapidly (10-20 seconds to trip) |
| Steam dump opening | Pressure rises within 2-5 seconds |
| Air ejector failure | Gradual pressure rise over minutes |

### 3.4 Backpressure Impact on Plant

| Backpressure Change | Effect |
|--------------------|--------|
| +1 in. Hg | ~1% loss in turbine output |
| +1 psia | ~10 MW loss (typical 1100 MW PWR) |

---

## 4. Condenser Sink Capacity

### 4.1 Design Heat Rejection Capacity

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Full-power thermal rejection** | ~7-8 × 10⁹ BTU/hr | Turbine exhaust + auxiliaries |
| **Steam dump capacity** | 40% of full-power steam flow | 12 valves total |
| **Single dump valve limit** | 895,000 lb/hr at 1106 psia | Limits overcooling transient |

### 4.2 Heat Loads Accepted by Condenser

| Source | Heat Load | Flow Path |
|--------|-----------|-----------|
| LP turbine exhaust | Primary load | Direct to condenser shells |
| Steam dump valves | Up to 40% rated | Via steam dump header |
| MFP turbine exhaust | ~50-100 MW_th | Recirculation to shell C |
| Heater drains (low-load) | Variable | Cascading drain system |
| SG blowdown | ~400,000 BTU/hr (100 gpm max/SG) | Flashes to condenser |
| Gland seal steam | Minor | Via gland steam condenser |
| Steam line drains | Minor | MSIV bypass warming |

### 4.3 Steam Dump to Condenser

**Valve Configuration:**
| Group | Valves | Function |
|-------|--------|----------|
| 1 (Cooldown) | 3 | First to open, last to close |
| 2 | 3 | Second priority |
| 3 | 3 | Third priority |
| 4 | 3 | Last to open, first to close |

**Steam Dump Header:**
- Each valve discharges to different condenser shell (distribution)
- Main steam pressure at dump: ~1092 psig (no-load)
- Steam enthalpy at dump: ~1192 BTU/lb

**Condenser Capacity Calculation:**
```
Q_dump = m_steam × (h_steam - h_condensate)
       = m_steam × h_fg (approximately)

At 1092 psig: h_fg ≈ 650 BTU/lb
Full 40% steam dump: ~6.4 × 10⁶ lb/hr × 650 BTU/lb = 4.2 × 10⁹ BTU/hr
```

### 4.4 MSIV Bypass/Steam Line Warming

During plant heatup, MSIV bypass valves warm main steam lines:
- Bypass valve capacity: Small (warming only)
- Steam condenses in steam lines
- Condensate drains to condenser via steam line drains
- Heat load: Minor compared to full steam dump

---

## 5. Return-Path Inventory Link (Hotwell/Feedwater/AFW)

### 5.1 Hotwell Configuration

| Parameter | Value | Source |
|-----------|-------|--------|
| Configuration | 3 hotwells (A, B, C), each divided A/B trains | HRTD 7.2 |
| Cross-connection | Trains interconnected at B condenser hotwell | HRTD 7.2 |
| Normal level setpoint | 24 in. | HRTD 7.2 |
| High level (reject opens) | 28 in. | HRTD 7.2 |
| High level (reject full open) | 40 in. | HRTD 7.2 |
| Low level (makeup opens) | 21 in. | HRTD 7.2 |
| Low level (makeup full open) | 8 in. | HRTD 7.2 |

### 5.2 Inventory Flow Paths

**Inputs to Hotwell:**
| Source | Path | Control |
|--------|------|---------|
| Condensed LP turbine exhaust | Direct | Steam flow determines |
| Condensed steam dumps | Direct | Valve position |
| Condensate pump recirculation | Recirculation line | Auto on low flow |
| MFP recirculation | To condenser C | Auto on low flow |
| LP heater drains | Cascading | Level controlled |
| CST makeup | From CST | Level < 21 in. |

**Outputs from Hotwell:**
| Destination | Path | Control |
|-------------|------|---------|
| Condensate pumps | Suction on hotwells | Continuous |
| CST reject | Via reject valve | Level > 28 in. |

### 5.3 Condensate Storage Tank (CST)

| Parameter | Value |
|-----------|-------|
| Type | Covered, outdoor storage |
| Total capacity | 450,000 gallons |
| Tech Spec minimum | 239,000 gallons |
| Unusable volume | 27,700 gallons |
| Instrument error allowance | 14,400 gallons |

**Minimum Volume Basis:**
- Hot standby for 2 hours
- Cooldown to 350°F in 4 hours

**CST Connections:**
- AFW pump suction (safety-related)
- Startup AFW pump suction
- Hotwell makeup supply
- Hotwell reject destination

### 5.4 Mass Conservation Flow Diagram

```
                    ┌─────────────────┐
                    │ Steam Generators │ ←── Feedwater
                    └────────┬────────┘
                             │ Steam
                             ▼
          ┌──────────────────┼──────────────────┐
          │                  │                  │
          ▼                  ▼                  ▼
    ┌──────────┐      ┌────────────┐      ┌─────────────┐
    │ Turbine  │      │Steam Dumps │      │Atmospheric  │
    │ (normal) │      │(to cond)   │      │Relief Valves│
    └────┬─────┘      └─────┬──────┘      └──────┬──────┘
         │                  │                    │
         └──────────────────┼────────────────────┘
                            │ (mass lost to ATM relief)
                            ▼
                    ┌───────────────┐
                    │   CONDENSER   │←── CW cooling
                    │   (Hotwells)  │
                    └───────┬───────┘
                            │
              ┌─────────────┼─────────────┐
              │             │             │
              ▼             │             ▼
        ┌─────────┐         │       ┌─────────┐
        │ Reject  │←───High Level───│ Makeup  │
        └────┬────┘         │       └────┬────┘
             │              │            │
             ▼              │            │
       ┌─────────────┐      │      ┌─────────────┐
       │     CST     │◄─────┼─────►│     CST     │
       │  (storage)  │      │      │  (makeup)   │
       └─────────────┘      │      └──────┬──────┘
             │              │             │
             │              ▼             │
             │      ┌───────────────┐     │
             │      │ Condensate    │     │
             │      │ Pumps         │     │
             │      └───────┬───────┘     │
             │              │             │
             │              ▼             │
             │      ┌───────────────┐     │
             │      │  Feedwater    │     │
             │      │  Heaters      │     │
             │      └───────┬───────┘     │
             │              │             │
             │              ▼             │
             │      ┌───────────────┐     │
             │      │  Feedwater    │     │
             │      │  Pumps        │     │
             │      └───────┬───────┘     │
             │              │             │
             │              └─────────────┘
             │                    │
             │                    ▼
             │           To Steam Generators
             │
             └──────────► AFW Pump Suction
```

### 5.5 AFW Return Path

**Auxiliary Feedwater Sources:**
| Source | Suction | Capacity | Drive |
|--------|---------|----------|-------|
| Motor-driven AFW pumps (2) | CST | ~350 gpm each | Electric |
| Turbine-driven AFW pump (1) | CST | ~700 gpm | Steam-driven |
| Startup AFW pump | CST | 1,020 gpm | Electric |

**AFW Flow Path:**
1. CST → AFW pump suction
2. AFW pump → discharge header
3. Header → individual SG feed lines
4. Entry point: Downstream of FWIVs
5. Steam generated → condenser (if available) or atmosphere

---

## 6. Circulating Water System

### 6.1 System Configuration

| Parameter | Value | Notes |
|-----------|-------|-------|
| Pumps | 2-4 depending on plant | 50-70% capacity each |
| Pump type | Vertical, single-stage | Large capacity |
| Flow rate | 140,000-200,000 gpm per pump | Plant-specific |
| Temperature rise | 15-25°F | Across condenser |
| Flow path | Series through A → B → C shells | Multipressure design |

### 6.2 CW Pump Specifications (Typical)

| Parameter | Value |
|-----------|-------|
| Capacity | ~150,000 gpm |
| Head | ~35 ft |
| Motor power | ~1,500 hp |
| Power supply | 12.47 kVac or 4.16 kVac |

### 6.3 Heat Rejection Calculation

```
Q_rejected = m_cw × Cp × ΔT_cw

Where:
m_cw = CW mass flow rate (lb/hr)
Cp = 1.0 BTU/lb-°F (water)
ΔT_cw = CW outlet - CW inlet temperature

Example at full power:
m_cw = 4 pumps × 150,000 gpm × 500 lb/min/gpm = 7.2 × 10⁸ lb/hr
ΔT_cw = 20°F
Q_rejected = 7.2 × 10⁸ × 1.0 × 20 = 1.44 × 10¹⁰ BTU/hr
```

---

## 7. Simulator Implementation Parameters

### 7.1 Minimum Required State Variables

```csharp
// Condenser State
public class CondenserState
{
    // C-9 Logic Inputs
    public float VacuumInHg { get; set; }           // 0-30 in. Hg (vacuum)
    public float BackpressurePsia { get; set; }     // 0-5 psia (absolute)
    public int CWPumpsRunning { get; set; }         // 0-4
    
    // C-9 Output
    public bool CondenserAvailable => (VacuumInHg > 22.0f) && (CWPumpsRunning >= 1);
    
    // Thermal State
    public float HeatLoadBtuPerHr { get; set; }     // Total heat rejection
    public float HotwellTempF { get; set; }         // ~120°F at design
    public float HotwellLevelIn { get; set; }       // 8-40 in. range
    
    // CW State
    public float CWInletTempF { get; set; }         // Ambient dependent
    public float CWOutletTempF { get; set; }        // Inlet + rise
    public float CWFlowGpm { get; set; }            // Total flow
    
    // Mass Balance
    public float SteamFlowLbPerHr { get; set; }     // Into condenser
    public float CondensateFlowLbPerHr { get; set; }// Out of hotwell
    public float HotwellMassLbm { get; set; }       // Current inventory
}
```

### 7.2 Key Setpoints and Constants

| Parameter | Value | Use |
|-----------|-------|-----|
| C-9 vacuum setpoint | 22.0 in. Hg | Condenser available logic |
| Hotwell normal level | 24 in. | Level control setpoint |
| Reject valve opens | 28 in. | High level |
| Makeup valve opens | 21 in. | Low level |
| Design vacuum | 26-28 in. Hg | Full-power operation |
| Turbine trip vacuum | ~20 in. Hg | Low vacuum trip |
| Latent heat (at condenser) | ~950-1000 BTU/lb | For heat balance |
| CW specific heat | 1.0 BTU/lb-°F | Standard |
| CW density | 62 lb/ft³ | Standard |

### 7.3 Simplified Vacuum Dynamics Model

```csharp
// First-order vacuum response
public void UpdateVacuum(float dt)
{
    // Heat balance determines equilibrium pressure
    float Q_in = SteamFlowLbPerHr * LatentHeatBtuPerLb;  // Steam load
    float Q_out = CWFlowGpm * 500 * DeltaTCW;             // CW rejection
    
    // Net heat determines pressure trend
    float Q_net = Q_in - Q_out;
    
    // Pressure change (simplified first-order response)
    float tau = 30.0f; // Time constant in seconds
    float P_equilibrium = CalculateEquilibriumPressure(Q_in, CWFlowGpm, CWInletTempF);
    
    BackpressurePsia += (P_equilibrium - BackpressurePsia) * dt / tau;
    
    // Convert to vacuum (in. Hg)
    VacuumInHg = 29.92f - (BackpressurePsia * 2.036f);
    
    // Clamp to physical limits
    VacuumInHg = Mathf.Clamp(VacuumInHg, 0f, 29.92f);
}
```

### 7.4 Hotwell Mass Balance

```csharp
public void UpdateHotwellLevel(float dt)
{
    // Mass in
    float massIn = SteamFlowLbPerHr * dt / 3600f;  // From condenser
    
    // Mass out
    float massOut = CondensatePumpFlowGpm * 500f * dt / 60f;
    
    // Makeup/Reject based on level
    if (HotwellLevelIn < 21f)
    {
        massIn += MakeupFlowGpm * 500f * dt / 60f;
    }
    if (HotwellLevelIn > 28f)
    {
        massOut += RejectFlowGpm * 500f * dt / 60f;
    }
    
    // Update mass
    HotwellMassLbm += massIn - massOut;
    
    // Convert mass to level (simplified linear approximation)
    HotwellLevelIn = HotwellMassLbm / HotwellMassPerInch;
}
```

---

## 8. Operating Procedures Reference

### 8.1 Condenser Startup Sequence

1. **Establish CW flow**
   - Start CW pumps (at least one for C-9)
   - Verify flow through all shells
   - Check CW outlet temperatures rising

2. **Pull vacuum**
   - Start hogging air ejectors
   - Monitor vacuum increase
   - Transfer to main air ejectors at ~15-20 in. Hg

3. **Verify C-9 satisfied**
   - Vacuum > 22 in. Hg
   - At least 1 CW pump running
   - Steam dump availability confirmed

4. **Establish hotwell level**
   - Verify makeup valve operational
   - Start condensate pump
   - Establish recirculation if needed

### 8.2 Loss of Condenser Vacuum

**Immediate Actions:**
1. Reduce reactor power / insert control rods
2. Verify CW pumps running
3. Check air ejector/vacuum pump operation
4. Check for air in-leakage

**If vacuum continues degrading:**
1. Manual turbine trip may be required
2. Shift to atmospheric steam dump if available
3. Initiate plant cooldown via PORVs/SG relief valves

### 8.3 Loss of All CW Pumps

**Consequences:**
- Rapid loss of condenser vacuum
- C-9 interlock actuates (blocks steam dumps to condenser)
- Turbine trip on low vacuum
- Steam relief via atmospheric valves or SG safeties

**Response:**
1. Reactor trip (automatic or manual)
2. Atmospheric steam dumps for decay heat removal
3. Initiate AFW for SG inventory
4. Transition to RHR cooling when conditions permit

---

## 9. References

1. NRC HRTD Section 7.2 — Condensate and Feedwater System
2. NRC HRTD Section 11.2 — Steam Dump Control System  
3. NRC HRTD Section 6.2 — Cooling Water Systems
4. EPRI NP-7382 — Design and Operating Guidelines for Nuclear Power Plant Condensers
5. EPRI TR-107422-V2 — Thermal Performance Engineers Handbook
6. EPRI TR-112819 — Condenser In-Leakage Guideline
7. Heat Exchange Institute (HEI) — Standards for Steam Surface Condenser

---

## 10. Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-17 | Initial release - comprehensive condenser reference |

---
