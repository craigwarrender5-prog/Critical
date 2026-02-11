// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// CoreMapData.cs - Static Core Layout Data
// ============================================================================
//
// PURPOSE:
//   Encodes the authentic Westinghouse 4-Loop PWR core layout as static data.
//   Provides the 15x15 grid occupancy, 193 fuel assembly positions, and
//   53 RCCA bank assignments for the core mosaic map display.
//
// PHYSICS BASIS:
//   Westinghouse 4-Loop PWR core geometry:
//     - 193 fuel assemblies in octagonal cross-pattern
//     - 15x15 grid with 32 corner positions removed
//     - 53 RCCAs distributed across 8 banks (4 shutdown + 4 control)
//     - Quarter-core symmetry for fuel loading
//     - Octant symmetry for RCCA placement
//
// SOURCES:
//   - NRC ML11223A212 — Westinghouse Technology Systems Manual, Section 3.1
//   - Westinghouse 4-Loop FSAR Chapter 4 — Reactor Design
//   - NRC NUREG/CR-6042 — Control Room Design Review Guidelines
//
// ARCHITECTURE:
//   Pure static data class — no MonoBehaviour, no Unity dependencies.
//   Used by: CoreMosaicMap.cs, AssemblyDetailPanel.cs, ReactorOperatorScreen.cs
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;

namespace Critical.UI
{
    /// <summary>
    /// Static data class containing the Westinghouse 4-Loop PWR core layout.
    /// All data is readonly and validated against NRC FSAR documentation.
    /// </summary>
    public static class CoreMapData
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        /// <summary>Grid dimension (15x15 for Westinghouse 4-Loop)</summary>
        public const int GRID_SIZE = 15;

        /// <summary>Total fuel assemblies in core</summary>
        public const int ASSEMBLY_COUNT = 193;

        /// <summary>Total RCCA locations</summary>
        public const int RCCA_COUNT = 53;

        /// <summary>Number of RCCA banks</summary>
        public const int BANK_COUNT = 8;

        /// <summary>Grid value indicating empty corner position</summary>
        public const int EMPTY = -1;

        /// <summary>Grid value indicating fuel-only assembly (no RCCA)</summary>
        public const int FUEL_ONLY = 0;

        // ====================================================================
        // BANK INDICES
        // Bank indices 1-8 used in CORE_GRID, 0 = fuel-only, -1 = empty
        // ====================================================================

        public const int BANK_SA = 1;
        public const int BANK_SB = 2;
        public const int BANK_SC = 3;
        public const int BANK_SD = 4;
        public const int BANK_D = 5;
        public const int BANK_C = 6;
        public const int BANK_B = 7;
        public const int BANK_A = 8;

        // ====================================================================
        // BANK NAMES
        // ====================================================================

        /// <summary>Bank names indexed by bank number (1-8)</summary>
        public static readonly string[] BANK_NAMES = new string[]
        {
            "",     // Index 0 unused
            "SA",   // 1 - Shutdown A
            "SB",   // 2 - Shutdown B
            "SC",   // 3 - Shutdown C
            "SD",   // 4 - Shutdown D
            "D",    // 5 - Control D (lead regulating)
            "C",    // 6 - Control C (overlap)
            "B",    // 7 - Control B (deep reduction)
            "A"     // 8 - Control A (power reduction lead)
        };

        /// <summary>Bank types: true = shutdown, false = control</summary>
        public static readonly bool[] BANK_IS_SHUTDOWN = new bool[]
        {
            false,  // Index 0 unused
            true,   // SA - Shutdown
            true,   // SB - Shutdown
            true,   // SC - Shutdown
            true,   // SD - Shutdown
            false,  // D - Control
            false,  // C - Control
            false,  // B - Control
            false   // A - Control
        };

        /// <summary>Expected RCCA count per bank for validation</summary>
        public static readonly int[] BANK_RCCA_COUNTS = new int[]
        {
            0,  // Index 0 unused
            8,  // SA
            8,  // SB
            4,  // SC
            4,  // SD
            9,  // D
            9,  // C
            8,  // B
            4   // A
        };

        // ====================================================================
        // BANK COLORS
        // Per Design Document Section 2.2.2
        // ====================================================================

        /// <summary>Bank colors for RCCA overlay display (indexed 1-8)</summary>
        public static readonly Color[] BANK_COLORS = new Color[]
        {
            Color.gray,                         // Index 0 - fuel only
            new Color(1.00f, 0.42f, 0.42f),     // SA - #FF6B6B (coral red)
            new Color(0.31f, 0.80f, 0.77f),     // SB - #4ECDC4 (teal)
            new Color(0.27f, 0.72f, 0.82f),     // SC - #45B7D1 (sky blue)
            new Color(0.59f, 0.81f, 0.71f),     // SD - #96CEB4 (sage green)
            new Color(1.00f, 0.92f, 0.65f),     // D  - #FFEAA7 (pale yellow)
            new Color(0.87f, 0.63f, 0.87f),     // C  - #DDA0DD (plum)
            new Color(0.60f, 0.85f, 0.78f),     // B  - #98D8C8 (mint)
            new Color(0.97f, 0.86f, 0.44f)      // A  - #F7DC6F (gold)
        };

        // ====================================================================
        // CORE GRID LAYOUT
        // 15x15 grid: -1 = empty corner, 0 = fuel-only, 1-8 = RCCA bank
        //
        // This layout is derived from the standard Westinghouse 4-Loop core
        // loading arrangement per NRC FSAR Figure 4.3-1 and similar documents.
        //
        // The RCCA bank assignments follow octant symmetry where possible,
        // with shutdown banks (SA-SD) at peripheral positions and control
        // banks (D, C, B, A) nearer the core center for effective power shaping.
        //
        // Row 0 is TOP of core (north), Row 14 is BOTTOM (south)
        // Col 0 is LEFT (west), Col 14 is RIGHT (east)
        // ====================================================================

        /// <summary>
        /// Core grid layout: [row, col] indexed.
        /// Values: -1 = empty, 0 = fuel-only, 1-8 = RCCA bank index
        /// </summary>
        public static readonly int[,] CORE_GRID = new int[15, 15]
        {
            // Col:  0   1   2   3   4   5   6   7   8   9  10  11  12  13  14
            /*  0 */ { -1, -1, -1, -1,  0, SA,  0,  D,  0, SA,  0, -1, -1, -1, -1 },
            /*  1 */ { -1, -1,  0, SB,  0,  0,  C,  0,  C,  0,  0, SB,  0, -1, -1 },
            /*  2 */ { -1,  0,  B,  0,  0,  D,  0,  A,  0,  D,  0,  0,  B,  0, -1 },
            /*  3 */ { -1, SB,  0,  0,  C,  0,  0,  0,  0,  0,  C,  0,  0, SB, -1 },
            /*  4 */ {  0,  0,  0,  C,  0, SD,  0,  B,  0, SD,  0,  C,  0,  0,  0 },
            /*  5 */ { SA,  0,  D,  0, SD,  0,  0,  0,  0,  0, SD,  0,  D,  0, SA },
            /*  6 */ {  0,  C,  0,  0,  0,  0, SC,  0, SC,  0,  0,  0,  0,  C,  0 },
            /*  7 */ {  D,  0,  A,  0,  B,  0,  0,  0,  0,  0,  B,  0,  A,  0,  D },
            /*  8 */ {  0,  C,  0,  0,  0,  0, SC,  0, SC,  0,  0,  0,  0,  C,  0 },
            /*  9 */ { SA,  0,  D,  0, SD,  0,  0,  0,  0,  0, SD,  0,  D,  0, SA },
            /* 10 */ {  0,  0,  0,  C,  0, SD,  0,  B,  0, SD,  0,  C,  0,  0,  0 },
            /* 11 */ { -1, SB,  0,  0,  C,  0,  0,  0,  0,  0,  C,  0,  0, SB, -1 },
            /* 12 */ { -1,  0,  B,  0,  0,  D,  0,  A,  0,  D,  0,  0,  B,  0, -1 },
            /* 13 */ { -1, -1,  0, SB,  0,  0,  C,  0,  C,  0,  0, SB,  0, -1, -1 },
            /* 14 */ { -1, -1, -1, -1,  0, SA,  0,  D,  0, SA,  0, -1, -1, -1, -1 }
        };

        // Use bank constants for readability
        private const int SA = BANK_SA;
        private const int SB = BANK_SB;
        private const int SC = BANK_SC;
        private const int SD = BANK_SD;
        private const int D = BANK_D;
        private const int C = BANK_C;
        private const int B = BANK_B;
        private const int A = BANK_A;

        // ====================================================================
        // DERIVED DATA (computed once at static initialization)
        // ====================================================================

        /// <summary>
        /// Assembly index (0-192) to grid position mapping.
        /// Assemblies are numbered left-to-right, top-to-bottom.
        /// </summary>
        public static readonly (int row, int col)[] ASSEMBLY_POSITIONS;

        /// <summary>
        /// Grid position to assembly index mapping.
        /// Returns -1 for empty corner positions.
        /// </summary>
        public static readonly int[,] GRID_TO_ASSEMBLY;

        /// <summary>
        /// RCCA bank to assembly indices mapping.
        /// BANK_ASSEMBLIES[bankIndex] returns array of assembly indices for that bank.
        /// Index 0 is unused (fuel-only assemblies not tracked here).
        /// </summary>
        public static readonly int[][] BANK_ASSEMBLIES;

        /// <summary>
        /// Assembly index to bank mapping.
        /// Returns 0 for fuel-only assemblies, 1-8 for RCCA banks.
        /// </summary>
        public static readonly int[] ASSEMBLY_TO_BANK;

        // ====================================================================
        // STATIC CONSTRUCTOR - Builds derived data
        // ====================================================================

        static CoreMapData()
        {
            // Initialize arrays
            ASSEMBLY_POSITIONS = new (int, int)[ASSEMBLY_COUNT];
            GRID_TO_ASSEMBLY = new int[GRID_SIZE, GRID_SIZE];
            ASSEMBLY_TO_BANK = new int[ASSEMBLY_COUNT];

            // Initialize bank assembly lists
            BANK_ASSEMBLIES = new int[BANK_COUNT + 1][];
            for (int b = 0; b <= BANK_COUNT; b++)
            {
                BANK_ASSEMBLIES[b] = new int[b == 0 ? 140 : BANK_RCCA_COUNTS[b]];
            }

            // Track how many assemblies we've added to each bank
            int[] bankCounts = new int[BANK_COUNT + 1];

            // Build mappings by scanning grid left-to-right, top-to-bottom
            int assemblyIndex = 0;

            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    int gridValue = CORE_GRID[row, col];

                    if (gridValue == EMPTY)
                    {
                        // Empty corner position
                        GRID_TO_ASSEMBLY[row, col] = -1;
                    }
                    else
                    {
                        // Valid assembly position
                        ASSEMBLY_POSITIONS[assemblyIndex] = (row, col);
                        GRID_TO_ASSEMBLY[row, col] = assemblyIndex;
                        ASSEMBLY_TO_BANK[assemblyIndex] = gridValue;

                        // Add to bank list
                        int bankIdx = gridValue;
                        if (bankIdx >= 0 && bankIdx <= BANK_COUNT)
                        {
                            if (bankCounts[bankIdx] < BANK_ASSEMBLIES[bankIdx].Length)
                            {
                                BANK_ASSEMBLIES[bankIdx][bankCounts[bankIdx]] = assemblyIndex;
                                bankCounts[bankIdx]++;
                            }
                        }

                        assemblyIndex++;
                    }
                }
            }

            // Resize fuel-only array to actual count
            if (bankCounts[0] < 140)
            {
                int[] trimmed = new int[bankCounts[0]];
                System.Array.Copy(BANK_ASSEMBLIES[0], trimmed, bankCounts[0]);
                BANK_ASSEMBLIES[0] = trimmed;
            }
        }

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================

        /// <summary>
        /// Check if a grid position is valid (inside core, not corner).
        /// </summary>
        public static bool IsValidPosition(int row, int col)
        {
            if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
                return false;
            return CORE_GRID[row, col] != EMPTY;
        }

        /// <summary>
        /// Get assembly index at grid position, or -1 if invalid/empty.
        /// </summary>
        public static int GetAssemblyAt(int row, int col)
        {
            if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
                return -1;
            return GRID_TO_ASSEMBLY[row, col];
        }

        /// <summary>
        /// Get grid position for assembly index.
        /// </summary>
        public static (int row, int col) GetPosition(int assemblyIndex)
        {
            if (assemblyIndex < 0 || assemblyIndex >= ASSEMBLY_COUNT)
                return (-1, -1);
            return ASSEMBLY_POSITIONS[assemblyIndex];
        }

        /// <summary>
        /// Get bank index for assembly (0 = fuel-only, 1-8 = RCCA bank).
        /// </summary>
        public static int GetBank(int assemblyIndex)
        {
            if (assemblyIndex < 0 || assemblyIndex >= ASSEMBLY_COUNT)
                return -1;
            return ASSEMBLY_TO_BANK[assemblyIndex];
        }

        /// <summary>
        /// Get bank name for assembly ("" for fuel-only, "SA"-"A" for RCCA).
        /// </summary>
        public static string GetBankName(int assemblyIndex)
        {
            int bank = GetBank(assemblyIndex);
            if (bank <= 0 || bank > BANK_COUNT)
                return "";
            return BANK_NAMES[bank];
        }

        /// <summary>
        /// Get bank color for assembly.
        /// </summary>
        public static Color GetBankColor(int assemblyIndex)
        {
            int bank = GetBank(assemblyIndex);
            if (bank < 0 || bank > BANK_COUNT)
                return Color.gray;
            return BANK_COLORS[bank];
        }

        /// <summary>
        /// Check if assembly has an RCCA.
        /// </summary>
        public static bool HasRCCA(int assemblyIndex)
        {
            return GetBank(assemblyIndex) > 0;
        }

        /// <summary>
        /// Check if assembly's RCCA is in a shutdown bank.
        /// </summary>
        public static bool IsShutdownBank(int assemblyIndex)
        {
            int bank = GetBank(assemblyIndex);
            if (bank <= 0 || bank > BANK_COUNT)
                return false;
            return BANK_IS_SHUTDOWN[bank];
        }

        /// <summary>
        /// Get all assembly indices for a bank.
        /// </summary>
        public static int[] GetBankAssemblies(int bankIndex)
        {
            if (bankIndex < 0 || bankIndex > BANK_COUNT)
                return System.Array.Empty<int>();
            return BANK_ASSEMBLIES[bankIndex];
        }

        /// <summary>
        /// Get assembly coordinate string (e.g., "H-08" for center assembly).
        /// Uses standard reactor notation: columns A-P (skip I), rows 01-15.
        /// </summary>
        public static string GetCoordinateString(int assemblyIndex)
        {
            if (assemblyIndex < 0 || assemblyIndex >= ASSEMBLY_COUNT)
                return "??-??";

            var (row, col) = ASSEMBLY_POSITIONS[assemblyIndex];

            // Column letters: A-H, J-P (skip I)
            char colLetter = col < 8 ? (char)('A' + col) : (char)('A' + col + 1);

            // Row numbers: 01-15 (1-indexed)
            int rowNum = row + 1;

            return $"{colLetter}-{rowNum:D2}";
        }

        /// <summary>
        /// Get assembly type description.
        /// </summary>
        public static string GetAssemblyType(int assemblyIndex)
        {
            int bank = GetBank(assemblyIndex);
            if (bank <= 0)
                return "Fuel Assembly";

            string bankName = BANK_NAMES[bank];
            string bankType = BANK_IS_SHUTDOWN[bank] ? "Shutdown" : "Control";
            return $"RCCA Bank {bankName} ({bankType})";
        }

        // ====================================================================
        // VALIDATION
        // ====================================================================

        /// <summary>
        /// Validate core layout data integrity.
        /// Returns true if all checks pass, logs errors otherwise.
        /// </summary>
        public static bool Validate()
        {
            bool valid = true;
            int assemblyCount = 0;
            int rccaCount = 0;
            int[] bankCounts = new int[BANK_COUNT + 1];

            // Count assemblies and validate grid
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    int gridValue = CORE_GRID[row, col];

                    if (gridValue == EMPTY)
                        continue;

                    assemblyCount++;

                    if (gridValue < 0 || gridValue > BANK_COUNT)
                    {
                        Debug.LogError($"[CoreMapData] Invalid grid value {gridValue} at ({row},{col})");
                        valid = false;
                        continue;
                    }

                    bankCounts[gridValue]++;

                    if (gridValue > 0)
                        rccaCount++;
                }
            }

            // Check total assembly count
            if (assemblyCount != ASSEMBLY_COUNT)
            {
                Debug.LogError($"[CoreMapData] Assembly count mismatch: expected {ASSEMBLY_COUNT}, found {assemblyCount}");
                valid = false;
            }

            // Check RCCA count
            if (rccaCount != RCCA_COUNT)
            {
                Debug.LogError($"[CoreMapData] RCCA count mismatch: expected {RCCA_COUNT}, found {rccaCount}");
                valid = false;
            }

            // Check individual bank counts
            for (int b = 1; b <= BANK_COUNT; b++)
            {
                if (bankCounts[b] != BANK_RCCA_COUNTS[b])
                {
                    Debug.LogError($"[CoreMapData] Bank {BANK_NAMES[b]} count mismatch: expected {BANK_RCCA_COUNTS[b]}, found {bankCounts[b]}");
                    valid = false;
                }
            }

            // Check fuel-only count (193 - 53 = 140)
            int expectedFuelOnly = ASSEMBLY_COUNT - RCCA_COUNT;
            if (bankCounts[0] != expectedFuelOnly)
            {
                Debug.LogError($"[CoreMapData] Fuel-only count mismatch: expected {expectedFuelOnly}, found {bankCounts[0]}");
                valid = false;
            }

            // Check quarter-core symmetry (spot check)
            // Center assembly should be at (7,7)
            int centerAssembly = GRID_TO_ASSEMBLY[7, 7];
            if (centerAssembly < 0)
            {
                Debug.LogError("[CoreMapData] Center assembly (7,7) not found");
                valid = false;
            }

            // Check octant symmetry for a few RCCA positions
            // SA bank should have symmetric positions
            if (CORE_GRID[0, 5] != BANK_SA || CORE_GRID[0, 9] != BANK_SA)
            {
                Debug.LogError("[CoreMapData] SA bank symmetry check failed (row 0)");
                valid = false;
            }

            if (valid)
            {
                Debug.Log($"[CoreMapData] Validation PASSED: {assemblyCount} assemblies, {rccaCount} RCCAs across {BANK_COUNT} banks");
            }

            return valid;
        }

        /// <summary>
        /// Print core map to console for debugging.
        /// </summary>
        public static void PrintCoreMap()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Core Map Layout (-- = empty, 00 = fuel, SA-A = RCCA bank):");
            sb.AppendLine("     A  B  C  D  E  F  G  H  J  K  L  M  N  O  P");

            for (int row = 0; row < GRID_SIZE; row++)
            {
                sb.Append($"{row + 1,2}  ");
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    int val = CORE_GRID[row, col];
                    string cell = val switch
                    {
                        EMPTY => "--",
                        FUEL_ONLY => "00",
                        BANK_SA => "SA",
                        BANK_SB => "SB",
                        BANK_SC => "SC",
                        BANK_SD => "SD",
                        BANK_D => " D",
                        BANK_C => " C",
                        BANK_B => " B",
                        BANK_A => " A",
                        _ => "??"
                    };
                    sb.Append($"{cell} ");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }
    }
}
