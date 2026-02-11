# RCS Primary Loop - Technical Specifications

## Source Documentation
- **NRC HRTD Section 3.2** - Reactor Coolant System (ML11223A213)
- **Westinghouse 4-Loop PWR FSAR** - Design Parameters
- **Operator_Screen_Layout_Plan_v1_0_0.md** - Screen 2 Specifications

---

## 1. System Overview

The Westinghouse 4-Loop PWR Reactor Coolant System consists of:
- **1 Reactor Vessel** (center)
- **4 Hot Legs** (29" ID, radiating from RV to SGs)
- **4 Steam Generators** (one per loop)
- **4 Intermediate/Crossover Legs** (31" ID, SG outlet to RCP suction)
- **4 Cold Legs** (27.5" ID, RCP discharge to RV)
- **4 Reactor Coolant Pumps** (one per loop, in cold leg)
- **1 Pressurizer** (connected to Loop 2 hot leg via surge line)

---

## 2. Piping Dimensions (Per NRC HRTD Table 3.2-1)

### Hot Leg (Reactor Vessel Outlet to Steam Generator Inlet)
| Parameter | Value |
|-----------|-------|
| Inside Diameter | 29.0 inches (73.66 cm) |
| Nominal Wall Thickness | 2.84 inches (7.21 cm) |
| Outside Diameter | 34.68 inches (88.09 cm) |
| Design Pressure | 2,485 psig |
| Design Temperature | 650°F |
| Material | Austenitic Stainless Steel |

### Cold Leg (RCP Discharge to Reactor Vessel Inlet)
| Parameter | Value |
|-----------|-------|
| Inside Diameter | 27.5 inches (69.85 cm) |
| Nominal Wall Thickness | 2.69 inches (6.83 cm) |
| Outside Diameter | 32.88 inches (83.52 cm) |
| Design Pressure | 2,485 psig |
| Design Temperature | 650°F |
| Material | Austenitic Stainless Steel |

### Intermediate/Crossover Leg (SG Outlet to RCP Suction)
| Parameter | Value |
|-----------|-------|
| Inside Diameter | 31.0 inches (78.74 cm) |
| Nominal Wall Thickness | 2.99 inches (7.59 cm) |
| Outside Diameter | 36.98 inches (93.93 cm) |
| Design Pressure | 2,485 psig |
| Design Temperature | 650°F |
| Material | Austenitic Stainless Steel |

### Surge Line (Pressurizer to Loop 2 Hot Leg)
| Parameter | Value |
|-----------|-------|
| Inside Diameter | 14.0 inches (35.56 cm) |
| Wall Thickness | 1.40 inches (3.56 cm) |
| Design Temperature | 680°F |

---

## 3. Major Component Dimensions

### Reactor Vessel
| Parameter | Value |
|-----------|-------|
| Inside Diameter | ~173 inches (4.39 m) |
| Overall Height | ~40 feet (12.2 m) |
| Wall Thickness | ~8.5 inches (21.6 cm) |
| Inlet Nozzles | 4 (cold leg connections) |
| Outlet Nozzles | 4 (hot leg connections) |
| Material | Low-alloy steel, SS clad interior |

### Steam Generator (Model 51/F)
| Parameter | Value |
|-----------|-------|
| Overall Height | 67.75 feet (20.65 m) |
| Shell Outside Diameter | 175.75 inches (4.46 m) upper |
| Lower Shell OD | ~134 inches (3.4 m) |
| Number of U-Tubes | 3,388 per SG |
| U-Tube OD | 0.875 inches |
| U-Tube Wall Thickness | 0.050 inches |
| Primary Inlet Nozzle | 29" ID (hot leg) |
| Primary Outlet Nozzle | 31" ID (crossover leg) |
| Design Pressure (Primary) | 2,485 psig |
| Design Pressure (Secondary) | 1,185 psig |

### Reactor Coolant Pump
| Parameter | Value |
|-----------|-------|
| Overall Height | 28.5 feet (8.69 m) |
| Overall Weight | 188,900 lb (85,730 kg) |
| Suction Nozzle ID | 31.0 inches |
| Discharge Nozzle ID | 27.5 inches |
| Pump Capacity | 88,500 gpm per pump |
| Speed Rating | 1,200 rpm |
| Motor Horsepower | 6,000 HP |
| Discharge Head | 277 feet |

### Pressurizer
| Parameter | Value |
|-----------|-------|
| Total Volume | 1,800 ft³ |
| Shell Inside Diameter | 84.0 inches (2.13 m) |
| Overall Height | ~53 feet (16.2 m) |
| Full Power Water Volume | 1,080 ft³ (60%) |
| Full Power Steam Volume | 720 ft³ (40%) |
| Design Pressure | 2,485 psig |
| Design Temperature | 680°F |
| Heater Capacity | 1,794 kW (78 heaters) |

---

## 4. Loop Layout Geometry

### Typical Loop Arrangement (Plan View)
```
                         SG-1 (Loop 1)
                            ○
                           /|\
                          / | \
                    Hot  /  |  \ Cold
                    Leg /   |   \ Leg
                       /    |    \
    SG-4 (Loop 4)     /     |     \      SG-2 (Loop 2)
         ○───────────┼─────[RV]─────┼───────────○
                      \     |     /        │
                       \    |    /     Surge Line
                        \   |   /          │
                         \  |  /           ▼
                          \ | /         [PZR]
                           \|/
                            ○
                         SG-3 (Loop 3)
```

### Elevation Differences (Critical for Natural Circulation)
| Component | Elevation Reference |
|-----------|---------------------|
| Reactor Vessel Core Midplane | 0 (reference) |
| RV Outlet Nozzle Centerline | +10 feet above core |
| Hot Leg Horizontal Run | +10 feet |
| SG Tube Sheet | +12 feet |
| SG U-Tube Top | +45 feet |
| Cold Leg Horizontal Run | +8 feet |
| RCP Suction | +8 feet |
| Pressurizer Bottom | +15 feet |
| Pressurizer Top | +68 feet |

### Loop Piping Lengths (Approximate)
| Segment | Length |
|---------|--------|
| Hot Leg (RV to SG) | ~25-30 feet |
| Crossover Leg (SG to RCP) | ~15-20 feet |
| Cold Leg (RCP to RV) | ~20-25 feet |
| Surge Line | ~50 feet |

---

## 5. Operating Parameters

### Normal Full Power Conditions
| Parameter | Value |
|-----------|-------|
| RCS Pressure | 2,235 psig (2,250 psia) |
| T-hot (Hot Leg) | 618-620°F |
| T-cold (Cold Leg) | 555-558°F |
| T-avg | 586-588°F |
| Core ΔT | 62-65°F |
| Total RCS Flow | 354,000 gpm (4 × 88,500) |
| Core Thermal Power | 3,411 MWt (typical) |

### Heatup Conditions (Mode 4-5)
| Parameter | Value |
|-----------|-------|
| RCS Pressure | 400-2,235 psig |
| T-avg Range | 100-557°F |
| Heatup Rate | ~50°F/hr (4 RCPs) |
| RCS Flow (4 RCPs) | 354,000 gpm |

---

## 6. Flow Paths and Directions

### Normal Power Operation Flow Path:
1. **Core Exit** → Hot coolant exits reactor vessel through outlet nozzles (~618°F)
2. **Hot Leg** → Flows through 29" pipe to steam generator inlet
3. **SG Primary Side** → Flows down through inlet plenum, up through U-tubes, down through outlet plenum
4. **Crossover Leg** → Cooled water (~557°F) flows through 31" pipe to RCP suction
5. **RCP** → Pump adds head, discharges through 27.5" pipe
6. **Cold Leg** → Flows to reactor vessel inlet nozzle
7. **Core Inlet** → Enters downcomer, flows down, turns up through core
8. **Repeat**

### Pressurizer Connection:
- Surge line connects Loop 2 hot leg to pressurizer bottom
- Spray lines connect Loop 2 and 3 cold legs to pressurizer top
- In-surge: Hot leg expansion pushes water into PZR
- Out-surge: RCS contraction draws water from PZR

---

## 7. Visual Representation Requirements

### For Blender Model - Central Visualization

**Components to Model:**
1. Reactor Vessel (simplified cylindrical with hemispherical heads)
2. 4 Hot Legs (29" pipes, red/orange color for hot)
3. 4 Steam Generators (simplified vertical cylinders)
4. 4 Crossover Legs (31" pipes, transitioning color)
5. 4 Reactor Coolant Pumps (simplified pump symbols)
6. 4 Cold Legs (27.5" pipes, blue color for cold)
7. Pressurizer (vertical cylinder with surge line to Loop 2)
8. Flow direction arrows (animated)

**Color Coding:**
- Hot Leg: Red/Orange (#FF4500 to #FF6B35)
- Cold Leg: Blue (#1E90FF to #4169E1)
- Crossover Leg: Gradient orange-to-blue
- Reactor Vessel: Gray metallic (#708090)
- Steam Generators: Silver/Gray (#C0C0C0)
- RCPs: Dark gray with green "running" indicator
- Pressurizer: Yellow/Gold (#FFD700)

**Animation Requirements:**
- Flow arrows pulsing/moving along pipe direction
- RCP rotation indicator (spinning symbol when running)
- Temperature color gradient (adjustable based on T-hot/T-cold)
- Pressurizer level indicator (water level animation)

---

## 8. Screen 2 Layout Summary (from Operator_Screen_Layout_Plan)

### Central Visual (15-65% width, 3-74% height)
- 4-loop RCS schematic with animated flow
- Color-coded temperature gradient
- RCP status indicators

### Left Gauges (0-15% width)
1. Loop 1 T-hot (°F)
2. Loop 2 T-hot (°F)
3. Loop 3 T-hot (°F)
4. Loop 4 T-hot (°F)
5. Loop 1 T-cold (°F)
6. Loop 2 T-cold (°F)
7. Loop 3 T-cold (°F)
8. Loop 4 T-cold (°F)

### Right Gauges (65-100% width)
1. Total RCS Flow (gpm)
2. Loop 1 Flow (gpm)
3. Loop 2 Flow (gpm)
4. Loop 3 Flow (gpm)
5. Loop 4 Flow (gpm)
6. Core Thermal Power (MWt)
7. Core ΔT (°F)
8. Average T-avg (°F)

### Bottom Controls (0-100% width, 74-100% height)
- RCP start/stop buttons (4 pumps)
- RCP status indicators
- RCP speed indicators (rpm)
- Natural circulation mode indicator
- Alarm panel

---

## 9. References

1. NRC HRTD Section 3.2 - Reactor Coolant System (ML11223A213)
2. NRC HRTD Section 10.1 - RCS Instrumentation (ML11223A281)
3. Westinghouse Technology Systems Manual
4. Typical 4-Loop PWR FSAR Chapter 5
5. NUREG-1150 - PRA Insights

---

**Document Version:** 1.0.0  
**Date:** 2026-02-09  
**Status:** Technical Reference for Screen 2 Implementation
