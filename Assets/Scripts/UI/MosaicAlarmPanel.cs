// ============================================================================
// CRITICAL: Master the Atom - Annunciator Alarm Panel
// MosaicAlarmPanel.cs - Nuclear I&C Annunciator Tile Grid
// ============================================================================
//
// PURPOSE:
//   Displays reactor alarm and status conditions as illuminated annunciator
//   tiles in a grid layout, matching the visual standard established by the
//   Heatup Validation Visual annunciator panel. Replaces the previous
//   scrolling text-list alarm display with authentic control room tiles.
//
// VISUAL STANDARD:
//   Tiles follow nuclear I&C annunciator window conventions:
//     - Dark when inactive (condition not met)
//     - Illuminated with colored background when active:
//       * Green  — normal status indicators (CRITICAL, HTRS ON, etc.)
//       * Amber  — warning conditions (LVL HIGH, T-AVG deviation)
//       * Red    — alarm conditions (PRESS LOW, TRIP, SUBCOOL LOW)
//     - 1px border: active color when lit, dim when off
//     - Multi-line centered labels in instrument font
//
// REFERENCE:
//   Westinghouse 4-Loop PWR main control board annunciator panel
//   NRC HRTD Section 4 — Annunciator Window Tile conventions
//   HeatupValidationVisual.Annunciators.cs — project visual standard
//
// ARCHITECTURE:
//   Implements IMosaicComponent + IAlarmFlashReceiver for MosaicBoard lifecycle.
//   Tile conditions evaluated each UpdateData() cycle from ReactorController.
//   Tile GameObjects created dynamically on Initialize() if not pre-built
//   by OperatorScreenBuilder.
//
// CREATED: v4.2.2 — Replaced text-list alarm panel with annunciator tile grid
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    using Controllers;

    /// <summary>
    /// Annunciator tile alarm panel for Reactor Operator Screen.
    /// Displays alarm and status conditions as illuminated tiles in a grid.
    /// </summary>
    public class MosaicAlarmPanel : MonoBehaviour, IMosaicComponent, IAlarmFlashReceiver
    {
        // ====================================================================
        // ANNUNCIATOR TILE DESCRIPTOR
        // ====================================================================

        #region Tile Descriptor

        /// <summary>
        /// Defines a single annunciator tile: its label, how to evaluate
        /// its condition, and whether it represents an alarm or status.
        /// </summary>
        private struct TileDescriptor
        {
            public string Label;
            public bool IsAlarm;     // true = red when active; false = check IsNormalStatus
            public bool IsWarning;   // true = amber when active (overrides IsAlarm=false)

            public TileDescriptor(string label, bool isAlarm, bool isWarning = false)
            {
                Label = label;
                IsAlarm = isAlarm;
                IsWarning = isWarning;
            }
        }

        /// <summary>
        /// Runtime state for a single tile UI element.
        /// </summary>
        internal class TileUI
        {
            public GameObject Root;
            public Image Background;
            public Image BorderTop;
            public Image BorderBottom;
            public Image BorderLeft;
            public Image BorderRight;
            public TextMeshProUGUI Label;
            public bool Active;
            public bool Acknowledged;
        }

        #endregion

        // ====================================================================
        // ANNUNCIATOR COLOR PALETTE
        // Matches HeatupValidationVisual.Styles.cs annunciator colors
        // ====================================================================

        #region Colors

        // Tile background states
        private static readonly Color COLOR_ANN_OFF     = new Color(0.12f, 0.13f, 0.16f, 1f);
        private static readonly Color COLOR_ANN_NORMAL  = new Color(0.10f, 0.35f, 0.12f, 1f);
        private static readonly Color COLOR_ANN_WARNING = new Color(0.45f, 0.35f, 0.00f, 1f);
        private static readonly Color COLOR_ANN_ALARM   = new Color(0.50f, 0.08f, 0.08f, 1f);

        // Tile text/border active colors
        private static readonly Color COLOR_TEXT_GREEN   = new Color(0.18f, 0.82f, 0.25f, 1f);
        private static readonly Color COLOR_TEXT_AMBER   = new Color(1.00f, 0.78f, 0.00f, 1f);
        private static readonly Color COLOR_TEXT_RED     = new Color(1.00f, 0.18f, 0.18f, 1f);

        // Inactive text/border
        private static readonly Color COLOR_TEXT_DIM     = new Color(0.55f, 0.58f, 0.65f, 1f);
        private static readonly Color COLOR_BORDER_DIM   = new Color(0.20f, 0.22f, 0.26f, 1f);

        // Acknowledged (muted)
        private static readonly Color COLOR_ACK_BG       = new Color(0.18f, 0.18f, 0.20f, 1f);
        private static readonly Color COLOR_ACK_TEXT      = new Color(0.50f, 0.50f, 0.50f, 1f);

        #endregion

        // ====================================================================
        // TILE DEFINITIONS — 16 tiles covering key reactor alarms/status
        // ====================================================================

        #region Tile Definitions

        private const int TILE_COUNT = 16;

        /// <summary>
        /// Static tile descriptors. Order matches the evaluation index
        /// in EvaluateTileConditions().
        /// </summary>
        private static readonly TileDescriptor[] TILE_DEFS = new TileDescriptor[]
        {
            // Row 1: Critical alarms
            new TileDescriptor("REACTOR\nTRIPPED",    true),      // 0
            new TileDescriptor("NEUTRON\nPOWER HI",   true),      // 1
            new TileDescriptor("STARTUP\nRATE HI",    true),      // 2
            new TileDescriptor("ROD BOTTOM\nALARM",   true),      // 3

            // Row 2: Pressure/temperature alarms
            new TileDescriptor("PRESS\nLOW",          true),      // 4
            new TileDescriptor("PRESS\nHIGH",         true),      // 5
            new TileDescriptor("T-AVG\nLOW",          true),      // 6
            new TileDescriptor("T-AVG\nHIGH",         true),      // 7

            // Row 3: Level/margin alarms + warnings
            new TileDescriptor("SUBCOOL\nLOW",        true),      // 8
            new TileDescriptor("PZR LVL\nLOW",        true),      // 9
            new TileDescriptor("PZR LVL\nHIGH",       false, true), // 10 — warning (amber)
            new TileDescriptor("OVERPOWER\nΔT",       true),      // 11

            // Row 4: Status indicators (green when active)
            new TileDescriptor("REACTOR\nCRITICAL",   false),     // 12 — status (green)
            new TileDescriptor("AUTO ROD\nCONTROL",   false),     // 13 — status (green)
            new TileDescriptor("PZR HTRS\nON",        false),     // 14 — status (green)
            new TileDescriptor("LOW\nFLOW",           true),      // 15
        };

        #endregion

        // ====================================================================
        // INSPECTOR FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Display Configuration")]
        [Tooltip("Number of columns in the tile grid")]
        public int GridColumns = 8;

        [Tooltip("Tile spacing in pixels")]
        public float TileSpacing = 3f;

        [Tooltip("Border thickness in pixels")]
        public float BorderThickness = 1f;

        [Header("Visual References")]
        [Tooltip("Container for tile grid (auto-created if null)")]
        public RectTransform TileContainer;

        [Header("Buttons")]
        [Tooltip("Acknowledge button")]
        public Button AcknowledgeButton;

        [Tooltip("Silence button")]
        public Button SilenceButton;

        [Header("Audio")]
        [Tooltip("Alarm sound")]
        public AudioSource AlarmSound;

        [Tooltip("Acknowledge sound")]
        public AudioSource AckSound;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private MosaicBoard _board;
        private List<TileUI> _tiles = new List<TileUI>();
        private bool[] _tileStates = new bool[TILE_COUNT];
        private bool _alarmFlashing;
        private bool _isSilenced;
        private bool _tilesBuilt;
        private TMP_FontAsset _instrumentFont;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache instrument font
            _instrumentFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");

            // Wire buttons
            if (AcknowledgeButton != null)
                AcknowledgeButton.onClick.AddListener(OnAcknowledgeClick);

            if (SilenceButton != null)
                SilenceButton.onClick.AddListener(OnSilenceClick);
        }

        private void OnEnable()
        {
            if (_board == null && MosaicBoard.Instance != null)
            {
                _board = MosaicBoard.Instance;
                _board.RegisterComponent(this);
            }
        }

        private void OnDisable()
        {
            _board?.UnregisterComponent(this);
        }

        #endregion

        // ====================================================================
        // IMosaicComponent IMPLEMENTATION
        // ====================================================================

        #region IMosaicComponent

        public void Initialize(MosaicBoard board)
        {
            _board = board;
            _board.OnAlarmStateChanged += OnAlarmStateChanged;

            // Build tiles if not already created by OperatorScreenBuilder
            if (!_tilesBuilt)
            {
                BuildTileGrid();
            }
        }

        public void UpdateData()
        {
            if (_board?.Reactor == null) return;

            EvaluateTileConditions();
            UpdateTileVisuals();
        }

        #endregion

        // ====================================================================
        // IAlarmFlashReceiver IMPLEMENTATION
        // ====================================================================

        #region IAlarmFlashReceiver

        public void OnAlarmFlash(bool flashOn)
        {
            _alarmFlashing = flashOn;

            // Flash only unacknowledged alarm tiles
            for (int i = 0; i < _tiles.Count && i < TILE_COUNT; i++)
            {
                if (_tileStates[i] && TILE_DEFS[i].IsAlarm && !_tiles[i].Acknowledged)
                {
                    UpdateSingleTileVisual(i);
                }
            }
        }

        #endregion

        // ====================================================================
        // TILE GRID CONSTRUCTION
        // ====================================================================

        #region Tile Construction

        /// <summary>
        /// Dynamically builds the annunciator tile grid.
        /// </summary>
        private void BuildTileGrid()
        {
            // Create container if needed
            if (TileContainer == null)
            {
                GameObject containerGO = new GameObject("TileGrid");
                containerGO.transform.SetParent(transform, false);

                TileContainer = containerGO.AddComponent<RectTransform>();
                TileContainer.anchorMin = new Vector2(0.01f, 0.05f);
                TileContainer.anchorMax = new Vector2(0.99f, 0.85f);
                TileContainer.offsetMin = Vector2.zero;
                TileContainer.offsetMax = Vector2.zero;
            }

            // Add GridLayoutGroup for automatic tile arrangement
            GridLayoutGroup grid = TileContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = TileContainer.gameObject.AddComponent<GridLayoutGroup>();

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.spacing = new Vector2(TileSpacing, TileSpacing);
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.childAlignment = TextAnchor.UpperLeft;

            // Calculate cell size based on available space
            // Will be updated on first layout pass; set reasonable defaults
            grid.cellSize = new Vector2(100f, 36f);

            // Create tile GameObjects
            _tiles.Clear();
            for (int i = 0; i < TILE_COUNT; i++)
            {
                TileUI tile = CreateTile(TileContainer, TILE_DEFS[i], i);
                _tiles.Add(tile);
            }

            _tilesBuilt = true;

            // Defer cell size calculation to next frame when layout is resolved
            StartCoroutine(UpdateCellSizeDeferred(grid));
        }

        /// <summary>
        /// Recalculate cell size after layout pass so it fits the container.
        /// </summary>
        private System.Collections.IEnumerator UpdateCellSizeDeferred(GridLayoutGroup grid)
        {
            yield return null; // Wait one frame for RectTransform to resolve

            if (TileContainer == null) yield break;

            Rect containerRect = TileContainer.rect;
            if (containerRect.width <= 0f || containerRect.height <= 0f) yield break;

            int rows = Mathf.CeilToInt((float)TILE_COUNT / GridColumns);

            float availW = containerRect.width - grid.padding.left - grid.padding.right
                         - (GridColumns - 1) * TileSpacing;
            float availH = containerRect.height - grid.padding.top - grid.padding.bottom
                         - (rows - 1) * TileSpacing;

            float cellW = availW / GridColumns;
            float cellH = availH / rows;

            grid.cellSize = new Vector2(Mathf.Max(cellW, 40f), Mathf.Max(cellH, 24f));
        }

        /// <summary>
        /// Creates a single annunciator tile with background, border, and label.
        /// </summary>
        private TileUI CreateTile(RectTransform parent, TileDescriptor desc, int index)
        {
            TileUI tile = new TileUI();

            // Root object
            tile.Root = new GameObject($"Tile_{index:D2}_{desc.Label.Replace("\n", "_")}");
            tile.Root.transform.SetParent(parent, false);

            RectTransform rootRect = tile.Root.AddComponent<RectTransform>();

            // Background
            tile.Background = tile.Root.AddComponent<Image>();
            tile.Background.color = COLOR_ANN_OFF;
            tile.Background.raycastTarget = false;

            // Borders (4 edges)
            tile.BorderTop = CreateBorderEdge(tile.Root.transform, "BorderTop",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -BorderThickness), Vector2.zero);

            tile.BorderBottom = CreateBorderEdge(tile.Root.transform, "BorderBottom",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, BorderThickness));

            tile.BorderLeft = CreateBorderEdge(tile.Root.transform, "BorderLeft",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(BorderThickness, 0f));

            tile.BorderRight = CreateBorderEdge(tile.Root.transform, "BorderRight",
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-BorderThickness, 0f), Vector2.zero);

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(tile.Root.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0.05f);
            labelRect.anchorMax = new Vector2(0.95f, 0.95f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            tile.Label = labelGO.AddComponent<TextMeshProUGUI>();
            tile.Label.text = desc.Label;
            if (_instrumentFont != null)
                tile.Label.font = _instrumentFont;
            tile.Label.fontSize = 9;
            tile.Label.fontStyle = FontStyles.Bold;
            tile.Label.alignment = TextAlignmentOptions.Center;
            tile.Label.color = COLOR_TEXT_DIM;
            tile.Label.enableWordWrapping = false;
            tile.Label.overflowMode = TextOverflowModes.Truncate;
            tile.Label.raycastTarget = false;

            tile.Active = false;
            tile.Acknowledged = false;

            return tile;
        }

        /// <summary>
        /// Creates a 1px border edge image anchored to a tile edge.
        /// </summary>
        private Image CreateBorderEdge(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject edgeGO = new GameObject(name);
            edgeGO.transform.SetParent(parent, false);

            RectTransform rect = edgeGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image img = edgeGO.AddComponent<Image>();
            img.color = COLOR_BORDER_DIM;
            img.raycastTarget = false;

            return img;
        }

        #endregion

        // ====================================================================
        // TILE CONDITION EVALUATION
        // ====================================================================

        #region Condition Evaluation

        /// <summary>
        /// Evaluates all tile conditions from ReactorController state.
        /// Maps each tile index to its boolean active state.
        /// </summary>
        private void EvaluateTileConditions()
        {
            var reactor = _board.Reactor;
            if (reactor == null) return;

            // Row 1: Critical alarms
            _tileStates[0]  = reactor.IsTripped;                                          // REACTOR TRIPPED
            _tileStates[1]  = reactor.NeutronPower > 1.03f;                               // NEUTRON POWER HI
            _tileStates[2]  = reactor.StartupRate_DPM > 1.0f;                             // STARTUP RATE HI
            _tileStates[3]  = reactor.BankDPosition < 10f                                 // ROD BOTTOM ALARM
                              && reactor.NeutronPower > 0.05f;                             // (only meaningful above P-5)

            // Row 2: Pressure/temperature alarms
            // NOTE: RCS Pressure, Subcooling, and PZR Level are not yet exposed
            // on ReactorController (they live in the heatup simulation engine).
            // These tiles remain dark until the RCS thermal-hydraulic properties
            // are bridged to ReactorController in a future version.
            _tileStates[4]  = false;                                                       // PRESS LOW (awaiting RCS T-H bridge)
            _tileStates[5]  = false;                                                       // PRESS HIGH (awaiting RCS T-H bridge)
            _tileStates[6]  = reactor.Tavg < 547f && reactor.NeutronPower > 0.02f;        // T-AVG LOW (only at power)
            _tileStates[7]  = reactor.Tavg > 567f;                                        // T-AVG HIGH

            // Row 3: Level/margin alarms
            _tileStates[8]  = false;                                                       // SUBCOOL LOW (awaiting RCS T-H bridge)
            _tileStates[9]  = false;                                                       // PZR LVL LOW (awaiting RCS T-H bridge)
            _tileStates[10] = false;                                                       // PZR LVL HIGH (awaiting RCS T-H bridge)
            _tileStates[11] = reactor.IsTripped && reactor.NeutronPower > 1.09f;          // OVERPOWER ΔT (simplified)

            // Row 4: Status indicators + low flow
            _tileStates[12] = reactor.IsCritical;                                          // REACTOR CRITICAL
            _tileStates[13] = false;                                                       // AUTO ROD CONTROL (placeholder)
            _tileStates[14] = false;                                                       // PZR HTRS ON (awaiting RCS T-H bridge)
            _tileStates[15] = reactor.FlowFraction < 0.90f;                                // LOW FLOW
        }

        #endregion

        // ====================================================================
        // TILE VISUAL UPDATES
        // ====================================================================

        #region Visual Updates

        /// <summary>
        /// Updates all tile visuals based on current conditions.
        /// </summary>
        private void UpdateTileVisuals()
        {
            for (int i = 0; i < _tiles.Count && i < TILE_COUNT; i++)
            {
                bool wasActive = _tiles[i].Active;
                _tiles[i].Active = _tileStates[i];

                // Clear acknowledged state if alarm clears
                if (!_tileStates[i])
                    _tiles[i].Acknowledged = false;

                // New alarm onset — mark unacknowledged
                if (_tileStates[i] && !wasActive && TILE_DEFS[i].IsAlarm)
                    _tiles[i].Acknowledged = false;

                UpdateSingleTileVisual(i);
            }
        }

        /// <summary>
        /// Updates a single tile's colors based on its state.
        /// </summary>
        private void UpdateSingleTileVisual(int index)
        {
            TileUI tile = _tiles[index];
            TileDescriptor desc = TILE_DEFS[index];

            Color bgColor;
            Color textColor;
            Color borderColor;

            if (!tile.Active)
            {
                // Inactive — dark tile
                bgColor = COLOR_ANN_OFF;
                textColor = COLOR_TEXT_DIM;
                borderColor = COLOR_BORDER_DIM;
            }
            else if (tile.Acknowledged && desc.IsAlarm)
            {
                // Acknowledged alarm — muted
                bgColor = COLOR_ACK_BG;
                textColor = COLOR_ACK_TEXT;
                borderColor = COLOR_ACK_TEXT;
            }
            else if (desc.IsAlarm)
            {
                // Active alarm — flash support
                bool showLit = !_alarmFlashing || tile.Acknowledged;
                bgColor = showLit ? COLOR_ANN_ALARM : COLOR_ANN_OFF;
                textColor = showLit ? COLOR_TEXT_RED : COLOR_TEXT_DIM;
                borderColor = showLit ? COLOR_TEXT_RED : COLOR_BORDER_DIM;
            }
            else if (desc.IsWarning)
            {
                // Active warning — amber
                bgColor = COLOR_ANN_WARNING;
                textColor = COLOR_TEXT_AMBER;
                borderColor = COLOR_TEXT_AMBER;
            }
            else
            {
                // Active status — green
                bgColor = COLOR_ANN_NORMAL;
                textColor = COLOR_TEXT_GREEN;
                borderColor = COLOR_TEXT_GREEN;
            }

            // Apply colors
            if (tile.Background != null) tile.Background.color = bgColor;
            if (tile.Label != null)      tile.Label.color = textColor;
            if (tile.BorderTop != null)    tile.BorderTop.color = borderColor;
            if (tile.BorderBottom != null)  tile.BorderBottom.color = borderColor;
            if (tile.BorderLeft != null)    tile.BorderLeft.color = borderColor;
            if (tile.BorderRight != null)   tile.BorderRight.color = borderColor;
        }

        #endregion

        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================

        #region Event Handlers

        private void OnAlarmStateChanged(bool hasAlarms)
        {
            if (hasAlarms && !_isSilenced && AlarmSound != null && !AlarmSound.isPlaying)
            {
                AlarmSound.Play();
            }
        }

        private void OnAcknowledgeClick()
        {
            // Acknowledge all active alarm tiles
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i].Active && TILE_DEFS[i].IsAlarm)
                    _tiles[i].Acknowledged = true;
            }

            // Also acknowledge through MosaicBoard
            _board?.AcknowledgeAlarms();

            if (AlarmSound != null && AlarmSound.isPlaying)
                AlarmSound.Stop();

            if (AckSound != null)
                AckSound.Play();

            _isSilenced = false;
            UpdateTileVisuals();
        }

        private void OnSilenceClick()
        {
            _isSilenced = true;

            if (AlarmSound != null && AlarmSound.isPlaying)
                AlarmSound.Stop();
        }

        #endregion

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        #region Public API

        /// <summary>
        /// Manually set tile grid as pre-built (called by OperatorScreenBuilder
        /// if it constructs the tiles itself).
        /// </summary>
        internal void SetTilesPreBuilt(List<TileUI> preBuiltTiles)
        {
            _tiles = preBuiltTiles;
            _tilesBuilt = true;
        }

        /// <summary>
        /// Force rebuild the tile grid (e.g., after container resize).
        /// </summary>
        public void RebuildGrid()
        {
            // Destroy existing tiles
            foreach (var tile in _tiles)
            {
                if (tile.Root != null)
                    Destroy(tile.Root);
            }
            _tiles.Clear();
            _tilesBuilt = false;

            BuildTileGrid();
        }

        #endregion
    }
}
