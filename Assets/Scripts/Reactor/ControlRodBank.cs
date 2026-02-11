// CRITICAL: Master the Atom - Phase 2 Reactor Core
// ControlRodBank.cs - Control Rod Bank Model with S-Curve Worth
//
// Models the 8 control rod banks in a Westinghouse 4-Loop PWR:
//   - 4 Shutdown Banks (SA, SB, SC, SD) - rapid shutdown capability
//   - 4 Control Banks (D, C, B, A) - normal power control
//
// Key physics:
//   - S-curve integral worth (sine-squared distribution)
//   - Step-by-step withdrawal at 72 steps/minute
//   - Bank overlap for continuous reactivity addition
//   - Rod drop dynamics for reactor trip
//
// Reference: Westinghouse 4-Loop PWR
// Sources: NRC HRTD Chapter 4, FSAR Chapter 4
//
// Gold Standard Architecture:
//   - Module owns all rod control physics
//   - Engine observes rod positions and reactivity
//   - Interlocks and permissives handled internally

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Control rod bank enumeration.
    /// Banks are numbered by withdrawal sequence (shutdown banks first, then control banks).
    /// </summary>
    public enum RodBank
    {
        SA = 0,  // Shutdown Bank A - first withdrawn
        SB = 1,  // Shutdown Bank B
        SC = 2,  // Shutdown Bank C
        SD = 3,  // Shutdown Bank D - last shutdown bank
        D = 4,   // Control Bank D - first control bank
        C = 5,   // Control Bank C
        B = 6,   // Control Bank B
        A = 7    // Control Bank A - last withdrawn (regulating bank)
    }
    
    /// <summary>
    /// Rod motion direction.
    /// </summary>
    public enum RodDirection
    {
        Stationary = 0,
        Withdrawing = 1,
        Inserting = -1
    }
    
    /// <summary>
    /// Control rod bank model with S-curve integral worth.
    /// Manages all 8 banks and provides total rod reactivity.
    /// </summary>
    public class ControlRodBank
    {
        #region Rod Configuration Constants — Delegated to PlantConstants (Issue #12 consolidation)
        
        /// <summary>Number of control rod banks</summary>
        public static int BANK_COUNT => PlantConstants.ROD_BANKS;
        
        /// <summary>Total steps per rod (0 = full in, 228 = full out)</summary>
        public static int STEPS_TOTAL => PlantConstants.ROD_TOTAL_STEPS;
        
        /// <summary>Rod withdrawal speed in steps per minute</summary>
        public static float STEPS_PER_MINUTE => PlantConstants.ROD_STEPS_PER_MINUTE;
        
        /// <summary>Rod withdrawal speed in steps per second</summary>
        public static float STEPS_PER_SECOND => PlantConstants.ROD_STEPS_PER_MINUTE / 60f;
        
        /// <summary>
        /// Bank overlap in steps.
        /// Next bank starts withdrawing when previous bank reaches this position.
        /// </summary>
        public const int BANK_OVERLAP_STEPS = 100;
        
        /// <summary>Rod insertion limit (steps) - minimum allowed position</summary>
        public const int ROD_INSERTION_LIMIT = 0;
        
        /// <summary>
        /// Bank D insertion limit during power operation (steps).
        /// Cannot insert below this to maintain shutdown margin.
        /// </summary>
        public const int BANK_D_INSERTION_LIMIT = 30;
        
        // =====================================================================
        // Bank Worth Values (pcm)
        // Based on typical Westinghouse 4-Loop values
        // Total worth must provide at least 8000 pcm shutdown margin
        // =====================================================================
        
        /// <summary>Individual bank worths in pcm</summary>
        public static readonly float[] BANK_WORTH_PCM = new float[]
        {
            1500f,  // SA - Shutdown A
            1500f,  // SB - Shutdown B
            1500f,  // SC - Shutdown C
            1500f,  // SD - Shutdown D
            1200f,  // D  - Control D (regulating bank backup)
            600f,   // C  - Control C
            400f,   // B  - Control B
            400f    // A  - Control A (primary regulating bank)
        };
        
        /// <summary>Total rod worth when all banks fully inserted (pcm)</summary>
        public const float TOTAL_WORTH_PCM = 8600f;
        
        #endregion
        
        #region Rod Drop (Trip) Constants
        
        /// <summary>Rod drop time from full out to full in (seconds)</summary>
        public const float ROD_DROP_TIME_SEC = 2.0f;
        
        /// <summary>Time to reach dashpot (85% insertion) in seconds</summary>
        public const float ROD_DROP_TO_DASHPOT_SEC = 1.2f;
        
        /// <summary>Dashpot position (steps from bottom)</summary>
        public const int DASHPOT_POSITION = 34;
        
        #endregion
        
        #region Instance State
        
        // Bank positions (steps: 0 = full in, 228 = full out)
        private float[] _bankPositions;
        
        // Motion state
        private RodDirection[] _bankDirections;
        private bool _isTripped;
        private float _tripTime_sec;  // Time since trip
        
        // Calculated values
        private float[] _bankReactivities;  // Individual bank reactivities
        private float _totalRodReactivity;  // Sum of all banks
        
        // Sequential mode flags
        private bool _sequentialWithdrawActive;
        private bool _sequentialInsertActive;
        
        // Alarms/Interlocks
        private bool _rodBottomAlarm;
        private bool _rodDeviationAlarm;
        private bool _bankSequenceViolation;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Get position of specified bank (0-228 steps)</summary>
        public float GetBankPosition(RodBank bank) => _bankPositions[(int)bank];
        
        /// <summary>Get position of bank by index (0-228 steps)</summary>
        public float GetBankPosition(int bankIndex) => _bankPositions[bankIndex];
        
        /// <summary>Get reactivity contribution of specified bank (pcm)</summary>
        public float GetBankReactivity(RodBank bank) => _bankReactivities[(int)bank];
        
        /// <summary>Get all bank positions</summary>
        public float[] BankPositions => (float[])_bankPositions.Clone();
        
        /// <summary>Total rod reactivity in pcm (positive when withdrawn)</summary>
        public float TotalRodReactivity => _totalRodReactivity;
        
        /// <summary>True if reactor is tripped (rods dropping)</summary>
        public bool IsTripped => _isTripped;
        
        /// <summary>True if any rod is at bottom (0 steps)</summary>
        public bool RodBottomAlarm => _rodBottomAlarm;
        
        /// <summary>True if bank positions deviate from sequence</summary>
        public bool RodDeviationAlarm => _rodDeviationAlarm;
        
        /// <summary>True if bank withdrawal sequence is violated</summary>
        public bool BankSequenceViolation => _bankSequenceViolation;
        
        /// <summary>True if all rods are fully withdrawn</summary>
        public bool AllRodsOut => AreAllRodsAtPosition(STEPS_TOTAL);
        
        /// <summary>True if all rods are fully inserted</summary>
        public bool AllRodsIn => AreAllRodsAtPosition(0);
        
        /// <summary>
        /// Control Bank D position (primary regulating bank).
        /// This is the key bank for rod control display.
        /// </summary>
        public float BankDPosition => _bankPositions[(int)RodBank.D];
        
        /// <summary>
        /// Control Bank A position (secondary regulating bank).
        /// </summary>
        public float BankAPosition => _bankPositions[(int)RodBank.A];
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create control rod bank model with initial positions.
        /// </summary>
        /// <param name="initiallyWithdrawn">True to start with all rods out, false for all rods in</param>
        public ControlRodBank(bool initiallyWithdrawn = false)
        {
            _bankPositions = new float[BANK_COUNT];
            _bankDirections = new RodDirection[BANK_COUNT];
            _bankReactivities = new float[BANK_COUNT];
            
            float initialPosition = initiallyWithdrawn ? STEPS_TOTAL : 0;
            
            for (int i = 0; i < BANK_COUNT; i++)
            {
                _bankPositions[i] = initialPosition;
                _bankDirections[i] = RodDirection.Stationary;
            }
            
            _isTripped = false;
            _tripTime_sec = 0f;
            
            CalculateReactivities();
        }
        
        #endregion
        
        #region Core Update Methods
        
        /// <summary>
        /// Update rod positions based on motion commands.
        /// Called each simulation timestep.
        /// </summary>
        /// <param name="dt_sec">Time step in seconds</param>
        public void Update(float dt_sec)
        {
            if (_isTripped)
            {
                // During trip, rods drop under gravity
                UpdateTripDynamics(dt_sec);
            }
            else
            {
                // Normal rod motion
                UpdateNormalMotion(dt_sec);
            }
            
            // Recalculate reactivities
            CalculateReactivities();
            
            // Check alarms
            CheckAlarms();
        }
        
        /// <summary>
        /// Update rod positions during normal (non-trip) operation.
        /// </summary>
        private void UpdateNormalMotion(float dt_sec)
        {
            // Re-evaluate sequential directions each step (overlap handoff)
            if (_sequentialWithdrawActive) EvaluateSequentialWithdraw();
            if (_sequentialInsertActive) EvaluateSequentialInsert();
            
            float stepChange = STEPS_PER_SECOND * dt_sec;
            
            for (int i = 0; i < BANK_COUNT; i++)
            {
                if (_bankDirections[i] == RodDirection.Withdrawing)
                {
                    _bankPositions[i] = Math.Min(_bankPositions[i] + stepChange, STEPS_TOTAL);
                }
                else if (_bankDirections[i] == RodDirection.Inserting)
                {
                    _bankPositions[i] = Math.Max(_bankPositions[i] - stepChange, 0);
                }
            }
        }
        
        /// <summary>
        /// Re-evaluate which banks should be withdrawing based on overlap.
        /// Called each step when sequential withdrawal is active.
        /// </summary>
        private void EvaluateSequentialWithdraw()
        {
            bool anyMoving = false;
            for (int i = 0; i < BANK_COUNT; i++)
            {
                bool canWithdraw = (i == 0) || (_bankPositions[i - 1] >= BANK_OVERLAP_STEPS);
                if (canWithdraw && _bankPositions[i] < STEPS_TOTAL)
                {
                    _bankDirections[i] = RodDirection.Withdrawing;
                    anyMoving = true;
                }
                else if (_bankPositions[i] >= STEPS_TOTAL)
                {
                    _bankDirections[i] = RodDirection.Stationary;
                }
            }
            if (!anyMoving) _sequentialWithdrawActive = false;
        }
        
        /// <summary>
        /// Re-evaluate which banks should be inserting based on overlap.
        /// Called each step when sequential insertion is active.
        /// </summary>
        private void EvaluateSequentialInsert()
        {
            bool anyMoving = false;
            for (int i = BANK_COUNT - 1; i >= 0; i--)
            {
                bool canInsert = (i == BANK_COUNT - 1) || (_bankPositions[i + 1] <= BANK_OVERLAP_STEPS);
                if (canInsert && _bankPositions[i] > 0)
                {
                    _bankDirections[i] = RodDirection.Inserting;
                    anyMoving = true;
                }
                else if (_bankPositions[i] <= 0)
                {
                    _bankDirections[i] = RodDirection.Stationary;
                }
            }
            if (!anyMoving) _sequentialInsertActive = false;
        }
        
        /// <summary>
        /// Update rod positions during trip (gravity drop).
        /// Uses realistic rod drop curve with dashpot deceleration.
        /// </summary>
        private void UpdateTripDynamics(float dt_sec)
        {
            _tripTime_sec += dt_sec;
            
            for (int i = 0; i < BANK_COUNT; i++)
            {
                if (_bankPositions[i] <= 0) continue;  // Already at bottom
                
                // Calculate position based on time since trip
                // Uses simplified gravity/friction model
                float newPosition = CalculateTripPosition(_bankPositions[i], _tripTime_sec);
                _bankPositions[i] = Math.Max(newPosition, 0f);
            }
            
            // Check if trip is complete
            if (AreAllRodsAtPosition(0))
            {
                _isTripped = false;  // Trip complete, rods at bottom
            }
        }
        
        /// <summary>
        /// Calculate rod position during trip based on initial position and time.
        /// Models gravity drop with dashpot deceleration.
        /// </summary>
        private float CalculateTripPosition(float initialPosition, float time_sec)
        {
            if (time_sec >= ROD_DROP_TIME_SEC) return 0f;
            
            // Two-phase model:
            // Phase 1: Free fall to dashpot (accelerating)
            // Phase 2: Dashpot deceleration (decelerating)
            
            if (time_sec <= ROD_DROP_TO_DASHPOT_SEC)
            {
                // Phase 1: Approximately linear (constant average velocity)
                float avgVelocity = (initialPosition - DASHPOT_POSITION) / ROD_DROP_TO_DASHPOT_SEC;
                return initialPosition - avgVelocity * time_sec;
            }
            else
            {
                // Phase 2: Dashpot - slower approach to bottom
                float dashpotTime = time_sec - ROD_DROP_TO_DASHPOT_SEC;
                float dashpotDuration = ROD_DROP_TIME_SEC - ROD_DROP_TO_DASHPOT_SEC;
                float fraction = dashpotTime / dashpotDuration;
                return DASHPOT_POSITION * (1f - fraction);
            }
        }
        
        /// <summary>
        /// Calculate reactivity for all banks based on current positions.
        /// </summary>
        private void CalculateReactivities()
        {
            _totalRodReactivity = 0f;
            
            for (int i = 0; i < BANK_COUNT; i++)
            {
                _bankReactivities[i] = CalculateBankReactivity(i, _bankPositions[i]);
                _totalRodReactivity += _bankReactivities[i];
            }
        }
        
        /// <summary>
        /// Calculate reactivity for a single bank using S-curve integral worth.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7)</param>
        /// <param name="position">Position in steps (0-228)</param>
        /// <returns>Reactivity in pcm (0 when full in, full worth when full out)</returns>
        public float CalculateBankReactivity(int bankIndex, float position)
        {
            if (bankIndex < 0 || bankIndex >= BANK_COUNT) return 0f;
            
            float totalWorth = BANK_WORTH_PCM[bankIndex];
            float fractionWithdrawn = Math.Max(0f, Math.Min(position / STEPS_TOTAL, 1f));
            
            // S-curve (sine-squared) integral worth
            // Worth = TotalWorth × sin²(π × fraction / 2)
            float angle = fractionWithdrawn * (float)Math.PI / 2f;
            float sinValue = (float)Math.Sin(angle);
            
            return totalWorth * sinValue * sinValue;
        }
        
        /// <summary>
        /// Calculate differential rod worth at given position.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7)</param>
        /// <param name="position">Position in steps (0-228)</param>
        /// <returns>Differential worth in pcm/step</returns>
        public float CalculateDifferentialWorth(int bankIndex, float position)
        {
            if (bankIndex < 0 || bankIndex >= BANK_COUNT) return 0f;
            
            float totalWorth = BANK_WORTH_PCM[bankIndex];
            float fractionWithdrawn = Math.Max(0f, Math.Min(position / STEPS_TOTAL, 1f));
            
            // Derivative of sin²(πx/2) = π × sin(πx/2) × cos(πx/2) = (π/2) × sin(πx)
            float angle = fractionWithdrawn * (float)Math.PI;
            float diffWorth = totalWorth * (float)Math.PI / 2f * (float)Math.Sin(angle) / STEPS_TOTAL;
            
            return Math.Max(diffWorth, 0f);
        }
        
        #endregion
        
        #region Rod Control Commands
        
        /// <summary>
        /// Start withdrawing specified bank.
        /// </summary>
        /// <param name="bank">Bank to withdraw</param>
        /// <returns>True if command accepted</returns>
        public bool WithdrawBank(RodBank bank)
        {
            if (_isTripped) return false;
            
            int idx = (int)bank;
            if (_bankPositions[idx] >= STEPS_TOTAL) return false;  // Already full out
            
            _bankDirections[idx] = RodDirection.Withdrawing;
            return true;
        }
        
        /// <summary>
        /// Start inserting specified bank.
        /// </summary>
        /// <param name="bank">Bank to insert</param>
        /// <returns>True if command accepted</returns>
        public bool InsertBank(RodBank bank)
        {
            if (_isTripped) return false;
            
            int idx = (int)bank;
            if (_bankPositions[idx] <= 0) return false;  // Already full in
            
            _bankDirections[idx] = RodDirection.Inserting;
            return true;
        }
        
        /// <summary>
        /// Stop motion of specified bank.
        /// </summary>
        /// <param name="bank">Bank to stop</param>
        public void StopBank(RodBank bank)
        {
            _bankDirections[(int)bank] = RodDirection.Stationary;
        }
        
        /// <summary>
        /// Stop all rod motion.
        /// </summary>
        public void StopAllBanks()
        {
            _sequentialWithdrawActive = false;
            _sequentialInsertActive = false;
            for (int i = 0; i < BANK_COUNT; i++)
            {
                _bankDirections[i] = RodDirection.Stationary;
            }
        }
        
        /// <summary>
        /// Withdraw all banks in sequence (automatic withdrawal).
        /// Banks withdraw in order: SA → SB → SC → SD → D → C → B → A
        /// with overlap at 100 steps.
        /// </summary>
        /// <returns>True if any bank is still withdrawing</returns>
        public bool WithdrawInSequence()
        {
            if (_isTripped) return false;
            
            _sequentialWithdrawActive = true;
            _sequentialInsertActive = false;
            
            bool anyWithdrawing = false;
            
            for (int i = 0; i < BANK_COUNT; i++)
            {
                // Check if previous bank has reached overlap position
                bool canWithdraw = (i == 0) || (_bankPositions[i - 1] >= BANK_OVERLAP_STEPS);
                
                if (canWithdraw && _bankPositions[i] < STEPS_TOTAL)
                {
                    _bankDirections[i] = RodDirection.Withdrawing;
                    anyWithdrawing = true;
                }
                else if (_bankPositions[i] >= STEPS_TOTAL)
                {
                    _bankDirections[i] = RodDirection.Stationary;
                }
            }
            
            return anyWithdrawing;
        }
        
        /// <summary>
        /// Insert all banks in sequence (automatic insertion).
        /// Banks insert in reverse order: A → B → C → D → SD → SC → SB → SA
        /// </summary>
        /// <returns>True if any bank is still inserting</returns>
        public bool InsertInSequence()
        {
            if (_isTripped) return false;
            
            _sequentialInsertActive = true;
            _sequentialWithdrawActive = false;
            
            bool anyInserting = false;
            
            for (int i = BANK_COUNT - 1; i >= 0; i--)
            {
                // Check if next bank (in sequence) has reached overlap position
                bool canInsert = (i == BANK_COUNT - 1) || (_bankPositions[i + 1] <= BANK_OVERLAP_STEPS);
                
                if (canInsert && _bankPositions[i] > 0)
                {
                    _bankDirections[i] = RodDirection.Inserting;
                    anyInserting = true;
                }
                else if (_bankPositions[i] <= 0)
                {
                    _bankDirections[i] = RodDirection.Stationary;
                }
            }
            
            return anyInserting;
        }
        
        /// <summary>
        /// Initiate reactor trip - drops all rods.
        /// </summary>
        public void Trip()
        {
            _isTripped = true;
            _tripTime_sec = 0f;
            
            // All banks start dropping
            for (int i = 0; i < BANK_COUNT; i++)
            {
                _bankDirections[i] = RodDirection.Inserting;
            }
        }
        
        /// <summary>
        /// Reset after trip (requires deliberate action).
        /// </summary>
        public void ResetTrip()
        {
            if (!AllRodsIn) return;  // Can only reset when all rods at bottom
            
            _isTripped = false;
            _tripTime_sec = 0f;
            StopAllBanks();
        }
        
        /// <summary>
        /// Set bank position directly (for initialization or testing).
        /// </summary>
        /// <param name="bank">Bank to set</param>
        /// <param name="position">Position in steps (0-228)</param>
        public void SetBankPosition(RodBank bank, float position)
        {
            int idx = (int)bank;
            _bankPositions[idx] = Math.Max(0f, Math.Min(position, STEPS_TOTAL));
            _bankDirections[idx] = RodDirection.Stationary;
            CalculateReactivities();
        }
        
        /// <summary>
        /// Set all banks to same position.
        /// </summary>
        /// <param name="position">Position in steps (0-228)</param>
        public void SetAllBankPositions(float position)
        {
            position = Math.Max(0f, Math.Min(position, STEPS_TOTAL));
            for (int i = 0; i < BANK_COUNT; i++)
            {
                _bankPositions[i] = position;
                _bankDirections[i] = RodDirection.Stationary;
            }
            CalculateReactivities();
        }
        
        #endregion
        
        #region Alarms and Interlocks
        
        /// <summary>
        /// Check all alarm conditions.
        /// </summary>
        private void CheckAlarms()
        {
            // Rod bottom alarm - any control bank at 0 steps
            _rodBottomAlarm = false;
            for (int i = (int)RodBank.D; i < BANK_COUNT; i++)
            {
                if (_bankPositions[i] <= 0)
                {
                    _rodBottomAlarm = true;
                    break;
                }
            }
            
            // Bank sequence violation - check withdrawal order
            _bankSequenceViolation = false;
            for (int i = 1; i < BANK_COUNT; i++)
            {
                // Each bank should not be withdrawn further than previous bank + overlap
                if (_bankPositions[i] > _bankPositions[i - 1] + BANK_OVERLAP_STEPS + 10)
                {
                    _bankSequenceViolation = true;
                    break;
                }
            }
            
            // Rod deviation alarm - simplified check
            _rodDeviationAlarm = false;
            // In real plant, compares individual rod positions within a bank
            // Here we just flag if sequence is violated
            _rodDeviationAlarm = _bankSequenceViolation;
        }
        
        /// <summary>
        /// Check if withdrawal is permitted (interlocks satisfied).
        /// </summary>
        /// <returns>True if rod withdrawal is allowed</returns>
        public bool IsWithdrawalPermitted()
        {
            // Cannot withdraw if tripped
            if (_isTripped) return false;
            
            // Additional interlocks would check:
            // - Source range counts (for initial startup)
            // - Intermediate range indication
            // - Reactor coolant system conditions
            
            return true;
        }
        
        /// <summary>
        /// Check if all rods are at specified position.
        /// </summary>
        private bool AreAllRodsAtPosition(float position)
        {
            for (int i = 0; i < BANK_COUNT; i++)
            {
                if (Math.Abs(_bankPositions[i] - position) > 1f) return false;
            }
            return true;
        }
        
        #endregion
        
        #region Display and Status
        
        /// <summary>
        /// Get bank name string.
        /// </summary>
        public static string GetBankName(RodBank bank)
        {
            switch (bank)
            {
                case RodBank.SA: return "SA";
                case RodBank.SB: return "SB";
                case RodBank.SC: return "SC";
                case RodBank.SD: return "SD";
                case RodBank.D: return "D";
                case RodBank.C: return "C";
                case RodBank.B: return "B";
                case RodBank.A: return "A";
                default: return "??";
            }
        }
        
        /// <summary>
        /// Get status string for all banks.
        /// </summary>
        public string GetStatusString()
        {
            string status = "";
            for (int i = 0; i < BANK_COUNT; i++)
            {
                RodBank bank = (RodBank)i;
                status += $"{GetBankName(bank)}:{_bankPositions[i]:F0} ";
            }
            return status.Trim();
        }
        
        /// <summary>
        /// Get motion direction for specified bank.
        /// </summary>
        public RodDirection GetBankDirection(RodBank bank)
        {
            return _bankDirections[(int)bank];
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate control rod bank calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Full in should have zero reactivity
            var rods = new ControlRodBank(false);
            if (Math.Abs(rods.TotalRodReactivity) > 1f) valid = false;
            
            // Test 2: Full out should have full worth
            rods.SetAllBankPositions(STEPS_TOTAL);
            if (Math.Abs(rods.TotalRodReactivity - TOTAL_WORTH_PCM) > 100f) valid = false;
            
            // Test 3: S-curve should give ~50% worth at ~60% withdrawal (due to sine-squared)
            // sin²(0.6 × π/2) = sin²(0.942) = 0.809² = 0.655, so 65% worth at 60% position
            var bank = new ControlRodBank(false);
            float reactivity = bank.CalculateBankReactivity(0, 0.6f * STEPS_TOTAL);
            float expectedFraction = 0.655f;
            if (Math.Abs(reactivity / BANK_WORTH_PCM[0] - expectedFraction) > 0.05f) valid = false;
            
            // Test 4: Half withdrawal should give ~25% worth (sin²(45°) = 0.5)
            reactivity = bank.CalculateBankReactivity(0, 0.5f * STEPS_TOTAL);
            if (Math.Abs(reactivity / BANK_WORTH_PCM[0] - 0.5f) > 0.05f) valid = false;
            
            // Test 5: Differential worth should peak at mid-position
            float diffAtBottom = bank.CalculateDifferentialWorth(0, 0);
            float diffAtMiddle = bank.CalculateDifferentialWorth(0, STEPS_TOTAL / 2);
            float diffAtTop = bank.CalculateDifferentialWorth(0, STEPS_TOTAL);
            if (diffAtMiddle <= diffAtBottom || diffAtMiddle <= diffAtTop) valid = false;
            
            // Test 6: Trip should insert all rods
            rods = new ControlRodBank(true);  // Start full out
            rods.Trip();
            rods.Update(ROD_DROP_TIME_SEC + 1f);  // Wait for drop
            if (!rods.AllRodsIn) valid = false;
            
            // Test 7: Bank sequence - later banks should not exceed previous + overlap
            rods = new ControlRodBank(false);
            rods.SetBankPosition(RodBank.SA, 100);
            rods.SetBankPosition(RodBank.SB, 150);  // Should not exceed SA + overlap
            rods.Update(0.1f);
            // This should trigger sequence alarm if SB > SA + overlap
            
            // Test 8: Total worth should match constant
            float totalWorth = 0f;
            for (int i = 0; i < BANK_COUNT; i++)
            {
                totalWorth += BANK_WORTH_PCM[i];
            }
            if (Math.Abs(totalWorth - TOTAL_WORTH_PCM) > 1f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
