// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// VCTPhysics.cs - Volume Control Tank Physics Module
// 
// Sources:
//   - NRC ML11223A214 Section 4.1 - Chemical and Volume Control System
//   - NRC ML11223A342 Section 19.2.2 - Plant Heatup Operations
//   - Westinghouse 4-Loop FSAR Chapter 9 - Auxiliary Systems
//   - NRC IN 93-84 - RCP Seal Injection Requirements
//
// Purpose:
//   Tracks VCT inventory, level, and boron concentration as the closed-loop
//   buffer between letdown (RCS outlet) and charging (RCS inlet). Implements
//   automatic level control actions and mass balance verification.
//
// Version: 1.0 - February 2026

using UnityEngine;
using System;

namespace Critical.Physics
{
    public static class VCTPhysics
    {
        #region State Structure
        
        public struct VCTState
        {
            public float Level_percent;
            public float Volume_gal;
            public float BoronConcentration_ppm;
            public float BoronMass_lb;
            public float LetdownFlow_gpm;
            public float ChargingFlow_gpm;
            public float SealReturnFlow_gpm;
            public float MakeupFlow_gpm;
            public float DivertFlow_gpm;
            public float NetFlow_gpm;
            public bool DivertActive;
            public bool AutoMakeupActive;
            public bool RWSTSuctionActive;
            public bool ChargingPumpRunning;
            public bool HighHighLevelAlarm;
            public bool HighLevelAlarm;
            public bool LowLevelAlarm;
            public bool LowLowLevelAlarm;
            public float CumulativeIn_gal;
            public float CumulativeOut_gal;
            public float InitialVolume_gal;
            public float CumulativeRCSChange_gal;   // Track cumulative RCS inventory change
            public float CumulativeExternalIn_gal;  // Cumulative external additions (makeup + seal return)
            public float CumulativeExternalOut_gal; // Cumulative external removals (divert + CBO)
            public bool MakeupFromBRS;                   // True if current makeup is sourced from BRS distillate
        }
        
        #endregion
        
        #region Constants — Delegated to PlantConstants (Issue #1 consolidation)
        
        // All VCT constants now reference PlantConstants as the single source of truth.
        // These properties maintain the original public API while eliminating duplication.
        // See: AUDIT_Stage1D_Support_Systems.md — Issue #1
        
        public static float CAPACITY_GAL => PlantConstants.VCT_CAPACITY_GAL;
        public static float LEVEL_HIGH_HIGH => PlantConstants.VCT_LEVEL_HIGH_HIGH;
        public static float LEVEL_HIGH => PlantConstants.VCT_LEVEL_HIGH;
        public static float LEVEL_NORMAL_HIGH => PlantConstants.VCT_LEVEL_NORMAL_HIGH;
        public static float LEVEL_NORMAL_LOW => PlantConstants.VCT_LEVEL_NORMAL_LOW;
        public static float LEVEL_MAKEUP_START => PlantConstants.VCT_LEVEL_MAKEUP_START;
        public static float LEVEL_LOW => PlantConstants.VCT_LEVEL_LOW;
        public static float LEVEL_LOW_LOW => PlantConstants.VCT_LEVEL_LOW_LOW;
        public static float LETDOWN_NORMAL_GPM => PlantConstants.LETDOWN_NORMAL_GPM;
        public static float CHARGING_NORMAL_GPM => PlantConstants.CHARGING_NORMAL_GPM;
        public static float SEAL_RETURN_NORMAL_GPM => PlantConstants.SEAL_RETURN_NORMAL_GPM;
        public static float CBO_LOSS_GPM => PlantConstants.CBO_LOSS_GPM;
        public static float AUTO_MAKEUP_FLOW_GPM => PlantConstants.AUTO_MAKEUP_FLOW_GPM;
        public static float MAX_MAKEUP_FLOW_GPM => PlantConstants.MAX_MAKEUP_FLOW_GPM;
        public static float BORON_BAT_PPM => PlantConstants.BORON_BAT_PPM;
        public static float BORON_RWST_PPM => PlantConstants.BORON_RWST_PPM;
        public static float BORON_COLD_SHUTDOWN_PPM => PlantConstants.BORON_COLD_SHUTDOWN_BOL_PPM;
        public const float MIXING_TAU_SEC = 120f;  // VCT-specific mixing time, not in PlantConstants
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize VCT for cold shutdown with default boron (2000 ppm).
        /// </summary>
        public static VCTState InitializeColdShutdown()
        {
            return InitializeColdShutdown(BORON_COLD_SHUTDOWN_PPM);
        }
        
        public static VCTState InitializeColdShutdown(float boronConc_ppm)
        {
            // Per NRC ML11223A342: "Prior to drawing a steam bubble, charging and 
            // letdown must be in service" - both run at 75 gpm for purification loop
            VCTState state = new VCTState();
            state.Level_percent = 55f;
            state.Volume_gal = CAPACITY_GAL * state.Level_percent / 100f;
            state.BoronConcentration_ppm = boronConc_ppm;
            state.BoronMass_lb = boronConc_ppm * state.Volume_gal * 8.34f / 1e6f;
            state.LetdownFlow_gpm = LETDOWN_NORMAL_GPM;      // 75 gpm
            state.ChargingFlow_gpm = CHARGING_NORMAL_GPM;    // 75 gpm - BALANCED
            state.SealReturnFlow_gpm = 0f;                   // No RCPs running
            state.MakeupFlow_gpm = 0f;
            state.DivertFlow_gpm = 0f;
            state.NetFlow_gpm = 0f;  // Balanced: letdown = charging
            state.DivertActive = false;
            state.AutoMakeupActive = false;
            state.RWSTSuctionActive = false;
            state.ChargingPumpRunning = true;  // Charging pump IS running
            state.HighHighLevelAlarm = false;
            state.HighLevelAlarm = false;
            state.LowLevelAlarm = false;
            state.LowLowLevelAlarm = false;
            state.CumulativeIn_gal = 0f;
            state.CumulativeOut_gal = 0f;
            state.InitialVolume_gal = state.Volume_gal;
            state.CumulativeRCSChange_gal = 0f;
            state.CumulativeExternalIn_gal = 0f;
            state.CumulativeExternalOut_gal = 0f;
            return state;
        }
        
        public static VCTState InitializeNormal(float level_percent = 55f, float boronConc_ppm = 1000f)
        {
            VCTState state = new VCTState();
            state.Level_percent = Mathf.Clamp(level_percent, 5f, 95f);
            state.Volume_gal = CAPACITY_GAL * state.Level_percent / 100f;
            state.BoronConcentration_ppm = boronConc_ppm;
            state.BoronMass_lb = boronConc_ppm * state.Volume_gal * 8.34f / 1e6f;
            state.LetdownFlow_gpm = LETDOWN_NORMAL_GPM;
            state.ChargingFlow_gpm = CHARGING_NORMAL_GPM;
            state.SealReturnFlow_gpm = SEAL_RETURN_NORMAL_GPM;
            state.MakeupFlow_gpm = 0f;
            state.DivertFlow_gpm = 0f;
            state.NetFlow_gpm = state.LetdownFlow_gpm + state.SealReturnFlow_gpm - state.ChargingFlow_gpm - CBO_LOSS_GPM;
            state.DivertActive = false;
            state.AutoMakeupActive = false;
            state.RWSTSuctionActive = false;
            state.ChargingPumpRunning = true;
            UpdateAlarms(ref state);
            state.CumulativeIn_gal = 0f;
            state.CumulativeOut_gal = 0f;
            state.InitialVolume_gal = state.Volume_gal;
            state.CumulativeRCSChange_gal = 0f;
            state.CumulativeExternalIn_gal = 0f;
            state.CumulativeExternalOut_gal = 0f;
            return state;
        }
        
        #endregion
        
        #region Update
        
        public static void Update(ref VCTState state, float dt_sec, float letdownFlow_gpm, 
                                  float chargingFlow_gpm, float sealReturnFlow_gpm, int rcpCount,
                                  float brsDistillateAvailable_gal = 0f)
        {
            state.LetdownFlow_gpm = letdownFlow_gpm;
            state.ChargingFlow_gpm = chargingFlow_gpm;
            state.SealReturnFlow_gpm = sealReturnFlow_gpm;
            state.MakeupFromBRS = false;  // Reset each step; set true if BRS used
            
            // CBO only exists when RCPs are running (seal leakage past #1 seal)
            // Per NRC ML11223A214: "3 gpm/RCP" seal return, CBO is additional leakage
            float cboLoss = rcpCount > 0 ? CBO_LOSS_GPM : 0f;
            
            // ==========================================================
            // LCV-112A PROPORTIONAL DIVERT VALVE - Per NRC HRTD 4.1
            // NOT a simple on/off valve. Proportionally diverts letdown
            // to BRS holdup tanks based on VCT level error above setpoint.
            // Uses PlantConstants.VCT_DIVERT_SETPOINT and VCT_DIVERT_PROP_BAND
            // ==========================================================
            float divertSetpoint = PlantConstants.VCT_DIVERT_SETPOINT; // 70% default
            float divertBand = PlantConstants.VCT_DIVERT_PROP_BAND;   // 20% band
            
            if (state.Level_percent > divertSetpoint)
            {
                state.DivertActive = true;
                // Proportional: 0% divert at setpoint, 100% at setpoint+band
                float divertFraction = Mathf.Clamp01(
                    (state.Level_percent - divertSetpoint) / divertBand);
                // Divert fraction of letdown flow
                state.DivertFlow_gpm = letdownFlow_gpm * divertFraction;
            }
            else if (state.Level_percent < divertSetpoint - 3f)
            {
                // Hysteresis: stop diverting when level drops 3% below setpoint
                state.DivertActive = false;
                state.DivertFlow_gpm = 0f;
            }
            else if (!state.DivertActive)
            {
                state.DivertFlow_gpm = 0f;
            }
            
            // Low level: Auto makeup — BRS distillate preferred, then RMS
            // Per NRC HRTD 4.1: RMS auto mode blends BAT + primary water
            // to match RCS boron concentration at ~80 gpm total.
            // BRS distillate (≈ 0 ppm) is first-priority source when available
            // (closed-loop reclaim). RMS blending is fallback.
            if (state.Level_percent <= LEVEL_MAKEUP_START && !state.RWSTSuctionActive)
            {
                state.AutoMakeupActive = true;
                state.MakeupFlow_gpm = AUTO_MAKEUP_FLOW_GPM;  // 35 gpm simplified
                state.MakeupFromBRS = brsDistillateAvailable_gal > 0f;
            }
            else if (state.Level_percent > LEVEL_NORMAL_LOW && !state.RWSTSuctionActive)
            {
                state.AutoMakeupActive = false;
                state.MakeupFlow_gpm = 0f;
            }
            
            // Low-low level: RWST suction
            if (state.Level_percent <= LEVEL_LOW_LOW)
            {
                state.RWSTSuctionActive = true;
                state.MakeupFlow_gpm = MAX_MAKEUP_FLOW_GPM;
            }
            else if (state.Level_percent > LEVEL_LOW)
            {
                state.RWSTSuctionActive = false;
            }
            
            // Volume balance
            float flowIn_gpm = letdownFlow_gpm + sealReturnFlow_gpm + state.MakeupFlow_gpm;
            float flowOut_gpm = chargingFlow_gpm + state.DivertFlow_gpm + cboLoss;
            state.NetFlow_gpm = flowIn_gpm - flowOut_gpm;
            
            float dt_min = dt_sec / 60f;
            float deltaVolume_gal = state.NetFlow_gpm * dt_min;
            
            state.Volume_gal += deltaVolume_gal;
            state.Volume_gal = Mathf.Clamp(state.Volume_gal, 0f, CAPACITY_GAL);
            state.Level_percent = 100f * state.Volume_gal / CAPACITY_GAL;
            
            state.CumulativeIn_gal += flowIn_gpm * dt_min;
            state.CumulativeOut_gal += flowOut_gpm * dt_min;
            
            // IP-0016: Canonical RCS boundary accumulation is owned by HeatupSimEngine
            // where the same flow values are applied to the primary mass ledger.
            // Do not mutate CumulativeRCSChange_gal here to avoid double counting.
            
            // External flows cross the CVCS closed-loop boundary:
            //   In:  seal return (RCP seal leakage returned) + makeup (RMS/RWST)
            //   Out: divert (to BRS holdup tanks) + CBO (controlled bleedoff)
            // Letdown and charging are internal loop transfers (VCT↔RCS) and cancel.
            state.CumulativeExternalIn_gal  += (sealReturnFlow_gpm + state.MakeupFlow_gpm) * dt_min;
            state.CumulativeExternalOut_gal += (state.DivertFlow_gpm + cboLoss) * dt_min;
            
            UpdateBoron(ref state, dt_sec);
            UpdateAlarms(ref state);
        }
        
        private static void UpdateBoron(ref VCTState state, float dt_sec)
        {
            float dt_min = dt_sec / 60f;
            
            if (state.MakeupFlow_gpm > 0.1f)
            {
                // Makeup boron concentration depends on source:
                //   RWST suction:     BORON_RWST_PPM (2500 ppm)
                //   BRS distillate:   ≈ 0 ppm (evaporator condensate)
                //   RMS blending:     ≈ 0 ppm (simplified; real plant blends to target)
                float makeupBoron = state.RWSTSuctionActive ? BORON_RWST_PPM : 0f;
                float totalVolume = state.Volume_gal;
                float makeupVolume = state.MakeupFlow_gpm * dt_min;
                
                if (totalVolume > 1f)
                {
                    float oldMass = state.BoronConcentration_ppm * (totalVolume - makeupVolume);
                    float makeupMass = makeupBoron * makeupVolume;
                    state.BoronConcentration_ppm = (oldMass + makeupMass) / totalVolume;
                }
            }
            
            state.BoronMass_lb = state.BoronConcentration_ppm * state.Volume_gal * 8.34f / 1e6f;
        }
        
        private static void UpdateAlarms(ref VCTState state)
        {
            state.HighHighLevelAlarm = state.Level_percent >= LEVEL_HIGH_HIGH;
            state.HighLevelAlarm = state.Level_percent >= LEVEL_HIGH;
            state.LowLevelAlarm = state.Level_percent <= LEVEL_LOW;
            state.LowLowLevelAlarm = state.Level_percent <= LEVEL_LOW_LOW;
        }
        
        #endregion
        
        #region Flow Calculations
        
        public static float CalculateChargingForPZRLevel(float pzrLevelError, float letdownFlow_gpm, float sealReturnFlow_gpm)
        {
            float baseCharging = letdownFlow_gpm - sealReturnFlow_gpm + CBO_LOSS_GPM;
            float correction = pzrLevelError * 5f;
            float charging = baseCharging + correction;
            return Mathf.Clamp(charging, 0f, 150f);
        }
        
        /// <summary>
        /// Calculate charging flow rate that balances letdown for purification-only operation.
        /// In purification mode, charging = letdown - seal_return + CBO to maintain VCT level.
        /// Per NRC HRTD 4.1: During purification, letdown and charging are balanced so that
        /// net RCS inventory change is zero (only ion exchange/filtration occurs in letdown path).
        /// </summary>
        /// <param name="letdownFlow_gpm">Current letdown flow rate in gpm</param>
        /// <returns>Balanced charging flow rate in gpm</returns>
        public static float CalculateBalancedChargingForPurification(float letdownFlow_gpm)
        {
            // Balance equation: charging = letdown - seal_return + CBO
            // Seal return flows into VCT (adds inventory), CBO removes inventory
            // Net result: VCT level stable, RCS inventory stable
            return letdownFlow_gpm - SEAL_RETURN_NORMAL_GPM + CBO_LOSS_GPM;
        }
        
        public static float CalculateRCSInventoryChange(float chargingFlow_gpm, float letdownFlow_gpm, float dt_sec)
        {
            float netFlow_gpm = chargingFlow_gpm - letdownFlow_gpm;
            return netFlow_gpm * dt_sec / 60f;
        }
        
        /// <summary>
        /// Accumulate RCS inventory change in VCT state for mass conservation tracking.
        /// Call this each timestep with the per-step RCS change.
        /// </summary>
        public static void AccumulateRCSChange(ref VCTState state, float rcsChange_gal)
        {
            state.CumulativeRCSChange_gal += rcsChange_gal;
        }
        
        #endregion
        
        #region Verification
        
        /// <summary>
        /// Verify mass conservation across tracked CVCS/auxiliary buckets using
        /// the same plant-boundary external definition as Stage E inventory audit.
        /// Compares cumulative VCT change + cumulative RCS change against
        /// (plant external in - plant external out).
        /// Returns error in gallons (should be near zero if mass is conserved).
        /// </summary>
        public static float VerifyMassConservation(
            VCTState state,
            float rcsInventoryChange_gal,
            float plantExternalIn_gal,
            float plantExternalOut_gal)
        {
            // ================================================================
            // CROSS-SYSTEM MASS CONSERVATION CHECK
            // ================================================================
            // The CVCS forms a closed loop: VCT ↔ RCS via letdown/charging.
            // Plant-wide external flows (aligned with inventory audit):
            //   In:  makeup not sourced from BRS (RMS/RWST)
            //   Out: CBO bleedoff to outside tracked plant
            //
            // Conservation law:
            //   ΔV_vct + ΔV_rcs = Σ(external_in) - Σ(external_out)
            //
            // If charging > letdown → RCS gains, VCT loses (internal transfer).
            // External flows legitimately add/remove mass from the loop.
            // Error should be near zero if mass is conserved.
            // ================================================================
            
            float vctChange = state.Volume_gal - state.InitialVolume_gal;
            float rcsChange = state.CumulativeRCSChange_gal;
            float externalNet = plantExternalIn_gal - plantExternalOut_gal;
            
            // Conservation: vctChange + rcsChange - externalNet ≈ 0
            float error = Mathf.Abs(vctChange + rcsChange - externalNet);
            
            // v5.4.0 Stage 5: Diagnostic logging when error exceeds 100 gal
            // This helps identify which term is drifting and causing conservation failure.
            if (error > 100f)
            {
                Debug.Log($"[VCT_CONS_DIAG] ERROR={error:F2}gal | " +
                          $"vctChange={vctChange:F2}gal | rcsChange={rcsChange:F2}gal | " +
                          $"externalNet={externalNet:F2}gal");
                Debug.Log($"[VCT_CONS_DIAG] VCT: Vol={state.Volume_gal:F2}gal Init={state.InitialVolume_gal:F2}gal | " +
                          $"CumIn={state.CumulativeIn_gal:F2}gal CumOut={state.CumulativeOut_gal:F2}gal");
                Debug.Log($"[VCT_CONS_DIAG] External(PlantBoundary): In={plantExternalIn_gal:F2}gal " +
                          $"Out={plantExternalOut_gal:F2}gal | " +
                          $"Makeup={state.MakeupFlow_gpm:F2}gpm Divert={state.DivertFlow_gpm:F2}gpm");
                Debug.Log($"[VCT_CONS_DIAG] Equation: |{vctChange:F2} + {rcsChange:F2} - {externalNet:F2}| = {error:F2}");
                
                // Additional diagnostic: check if rcsChange is near zero (likely during solid ops)
                if (Mathf.Abs(rcsChange) < 10f && Mathf.Abs(vctChange) > 100f)
                {
                    Debug.LogWarning($"[VCT_CONS_DIAG] SUSPECT: rcsChange≈0 but vctChange={vctChange:F2}. " +
                                     $"Possibly in solid ops where RCS change not accumulated.");
                }
            }
            
            return error;
        }

        /// <summary>
        /// Backward-compatible overload. Uses legacy loop-scoped external accumulators.
        /// Prefer the plant-boundary overload for Stage E alignment.
        /// </summary>
        public static float VerifyMassConservation(VCTState state, float rcsInventoryChange_gal)
        {
            return VerifyMassConservation(
                state,
                rcsInventoryChange_gal,
                state.CumulativeExternalIn_gal,
                state.CumulativeExternalOut_gal);
        }
        
        public static float GetTurnoverTime(VCTState state)
        {
            float throughput_gpm = state.LetdownFlow_gpm + state.SealReturnFlow_gpm + state.MakeupFlow_gpm;
            if (throughput_gpm < 0.1f) return float.MaxValue;
            return state.Volume_gal / throughput_gpm;
        }
        
        #endregion
        
        #region Utilities
        
        public static string GetStatusString(VCTState state)
        {
            if (state.RWSTSuctionActive) return "RWST SUCTION";
            if (state.LowLowLevelAlarm) return "LO-LO LEVEL";
            if (state.LowLevelAlarm) return "LOW LEVEL";
            if (state.AutoMakeupActive) return "AUTO MAKEUP";
            if (state.DivertActive) return "DIVERTING";
            if (state.HighLevelAlarm) return "HIGH LEVEL";
            if (state.HighHighLevelAlarm) return "HI-HI LEVEL";
            return "NORMAL";
        }
        
        public static int GetAlarmSeverity(VCTState state)
        {
            if (state.LowLowLevelAlarm || state.HighHighLevelAlarm) return 3;
            if (state.LowLevelAlarm || state.HighLevelAlarm) return 2;
            if (state.AutoMakeupActive || state.DivertActive) return 1;
            return 0;
        }
        
        public static bool IsLevelNormal(VCTState state)
        {
            return state.Level_percent >= LEVEL_NORMAL_LOW && state.Level_percent <= LEVEL_NORMAL_HIGH;
        }
        
        #endregion
    }
}
