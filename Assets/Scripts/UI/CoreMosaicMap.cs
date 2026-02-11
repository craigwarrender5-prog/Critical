// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// CoreMosaicMap.cs - Interactive Core Mosaic Map Display
// ============================================================================
//
// PURPOSE:
//   Renders all 193 fuel assemblies as interactive colored cells in the
//   authentic Westinghouse cross-pattern. Provides real-time visualization
//   of core conditions with RCCA position overlays and bank-colored markers.
//
// VISUAL DESIGN:
//   Inspired by RBMK mosaic mimic boards but adapted for Westinghouse PWR:
//     - 15x15 grid with corner positions empty (octagonal cross-section)
//     - Each cell ~28x28 pixels with 2px gaps
//     - Color-coded by selected display mode (power, temp, etc.)
//     - RCCA locations show bank letter and insertion indicator
//     - Hover tooltip with assembly details
//     - Click to select and show detail panel
//
// DISPLAY MODES:
//   1. Relative Power - Blue (cold) to Red (hot) gradient
//   2. Fuel Temperature - Blue to Yellow to Red gradient
//   3. Coolant Temperature - Cyan to Orange gradient
//   4. Rod Bank Overlay - Bank colors with grey for fuel-only
//
// ARCHITECTURE:
//   - Reads data from ReactorController (via MosaicBoard reference)
//   - Pre-instantiates 193 Image GameObjects at initialization
//   - Updates colors at 2 Hz for performance
//   - Uses CoreMapData for layout and bank assignments
//
// SOURCES:
//   - ReactorOperatorGUI_Design_v1_0_0_0.md Section 2
//   - Unity_Implementation_Manual_v1_0_0_0.md Chapter 11-12
//
// GOLD STANDARD: Yes
// CHANGE: v2.0.3 — Replaced legacy Input.mousePosition with
//         Mouse.current.position.ReadValue() (New Input System)
// CHANGE: v4.1.0 — Cell backgrounds use cell_bg sprite for beveled appearance.
//         Bank labels and tooltip text upgraded from legacy Text to
//         TextMeshProUGUI for visual consistency with instrument font upgrade.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Critical.UI
{
    using Controllers;

    /// <summary>
    /// Display modes for core map color-coding.
    /// </summary>
    public enum CoreMapDisplayMode
    {
        RelativePower,      // Blue to Red based on assembly power fraction
        FuelTemperature,    // Blue to Yellow to Red based on fuel centerline temp
        CoolantTemperature, // Cyan to Orange based on outlet temperature
        RodBankOverlay      // Bank-specific colors, grey for fuel-only
    }

    /// <summary>
    /// Interactive 193-assembly core mosaic map display.
    /// Renders the Westinghouse 4-Loop PWR core in RBMK-style mosaic format.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CoreMosaicMap : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ====================================================================
        // INSPECTOR FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Display Settings")]
        [Tooltip("Current display mode for cell coloring")]
        public CoreMapDisplayMode DisplayMode = CoreMapDisplayMode.RelativePower;

        [Tooltip("Update rate for color changes (Hz)")]
        [Range(1f, 10f)]
        public float UpdateRate = 2f;

        [Tooltip("Show RCCA bank labels on cells")]
        public bool ShowBankLabels = true;

        [Tooltip("Show RCCA insertion indicators")]
        public bool ShowInsertionIndicators = true;

        [Header("Cell Appearance")]
        [Tooltip("Cell size in pixels")]
        public float CellSize = 28f;

        [Tooltip("Gap between cells in pixels")]
        public float CellGap = 2f;

        [Tooltip("Cell corner radius (0 = square)")]
        public float CellCornerRadius = 2f;

        [Header("Color Settings - Power Mode")]
        public Color PowerColdColor = new Color(0f, 0.27f, 0.67f);      // #0044AA
        public Color PowerHotColor = new Color(1f, 0.13f, 0f);          // #FF2200

        [Header("Color Settings - Fuel Temp Mode")]
        public Color FuelColdColor = new Color(0f, 0.27f, 0.67f);       // Blue
        public Color FuelMidColor = new Color(1f, 0.92f, 0f);           // Yellow
        public Color FuelHotColor = new Color(1f, 0.13f, 0f);           // Red

        [Header("Color Settings - Coolant Temp Mode")]
        public Color CoolantColdColor = new Color(0f, 0.8f, 1f);        // Cyan
        public Color CoolantHotColor = new Color(1f, 0.5f, 0f);         // Orange

        [Header("Color Settings - General")]
        public Color CellBorderColor = new Color(0.16f, 0.16f, 0.21f);  // #2A2A35
        public Color SelectedColor = new Color(0f, 0.8f, 1f);           // #00CCFF
        public Color HoveredColor = new Color(0.5f, 0.9f, 1f);          // Light cyan

        [Header("References")]
        [Tooltip("Parent MosaicBoard for data access")]
        public MosaicBoard Board;

        [Tooltip("Tooltip panel for hover info")]
        public GameObject TooltipPanel;

        [Tooltip("Tooltip text component (TMP)")]
        public TextMeshProUGUI TooltipText;

        [Tooltip("Detail panel for selected assembly")]
        public AssemblyDetailPanel DetailPanel;

        [Header("Bank Filter")]
        [Tooltip("Currently filtered bank (0 = show all, 1-8 = specific bank)")]
        public int FilteredBank = 0;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        // Cell GameObjects and components
        private GameObject[] _cellObjects;
        private Image[] _cellImages;
        private TextMeshProUGUI[] _cellLabels;
        private Image[] _insertionBars;
        private RectTransform[] _cellRects;

        // State tracking
        private int _hoveredAssembly = -1;
        private int _selectedAssembly = -1;
        private float _lastUpdateTime;
        private bool _initialized;

        // Cached data
        private float[] _assemblyPowers;
        private float[] _fuelTemps;
        private float[] _coolantTemps;
        private float[] _rodPositions;

        // Color gradient caches
        private Gradient _powerGradient;
        private Gradient _fuelTempGradient;
        private Gradient _coolantTempGradient;

        // Reactor data ranges (for normalization)
        private const float POWER_MIN = 0f;
        private const float POWER_MAX = 2f;           // Relative power (1.0 = average)
        private const float FUEL_TEMP_MIN = 500f;     // °F at cold
        private const float FUEL_TEMP_MAX = 4000f;    // °F at high power
        private const float COOLANT_TEMP_MIN = 530f;  // °F Tcold
        private const float COOLANT_TEMP_MAX = 620f;  // °F Thot
        private const float ROD_WITHDRAWN = 228f;     // Steps fully withdrawn
        private const float ROD_INSERTED = 0f;        // Steps fully inserted

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeGradients();
            InitializeDataArrays();
        }

        private void Start()
        {
            // Find MosaicBoard if not assigned
            if (Board == null)
            {
                Board = GetComponentInParent<MosaicBoard>();
                if (Board == null)
                {
                    Board = FindObjectOfType<MosaicBoard>();
                }
            }

            // Validate core data
            if (!CoreMapData.Validate())
            {
                Debug.LogError("[CoreMosaicMap] Core data validation failed!");
            }

            // Create cell GameObjects
            CreateCells();
            _initialized = true;

            // Initial update
            UpdateAllCells();
        }

        private void Update()
        {
            if (!_initialized) return;

            // Throttle updates for performance
            float updateInterval = 1f / UpdateRate;
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                _lastUpdateTime = Time.time;
                FetchReactorData();
                UpdateAllCells();
            }

            // Update tooltip position if visible
            UpdateTooltip();
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                UpdateAllCells();
            }
        }

        #endregion

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        #region Initialization

        private void InitializeGradients()
        {
            // Power gradient: Blue -> Red
            _powerGradient = new Gradient();
            _powerGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(PowerColdColor, 0f),
                    new GradientColorKey(PowerHotColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // Fuel temp gradient: Blue -> Yellow -> Red
            _fuelTempGradient = new Gradient();
            _fuelTempGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(FuelColdColor, 0f),
                    new GradientColorKey(FuelMidColor, 0.5f),
                    new GradientColorKey(FuelHotColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // Coolant temp gradient: Cyan -> Orange
            _coolantTempGradient = new Gradient();
            _coolantTempGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(CoolantColdColor, 0f),
                    new GradientColorKey(CoolantHotColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        private void InitializeDataArrays()
        {
            int count = CoreMapData.ASSEMBLY_COUNT;
            _assemblyPowers = new float[count];
            _fuelTemps = new float[count];
            _coolantTemps = new float[count];
            _rodPositions = new float[count];

            // Initialize with default values
            for (int i = 0; i < count; i++)
            {
                _assemblyPowers[i] = 1f;        // Average power
                _fuelTemps[i] = 1200f;          // Moderate fuel temp
                _coolantTemps[i] = 580f;        // Average coolant temp
                _rodPositions[i] = ROD_WITHDRAWN; // All rods withdrawn
            }
        }

        private void CreateCells()
        {
            int count = CoreMapData.ASSEMBLY_COUNT;
            _cellObjects = new GameObject[count];
            _cellImages = new Image[count];
            _cellLabels = new TextMeshProUGUI[count];
            _insertionBars = new Image[count];
            _cellRects = new RectTransform[count];

            // Calculate total grid size
            float gridSize = CoreMapData.GRID_SIZE * (CellSize + CellGap) - CellGap;
            
            // Get our RectTransform
            RectTransform myRect = GetComponent<RectTransform>();

            // Create container for cells
            GameObject cellContainer = new GameObject("CellContainer");
            cellContainer.transform.SetParent(transform, false);
            RectTransform containerRect = cellContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(gridSize, gridSize);
            containerRect.anchoredPosition = Vector2.zero;

            // Create each cell
            for (int i = 0; i < count; i++)
            {
                CreateCell(i, cellContainer.transform, gridSize);
            }

            Debug.Log($"[CoreMosaicMap] Created {count} assembly cells");
        }

        private void CreateCell(int assemblyIndex, Transform parent, float gridSize)
        {
            var (row, col) = CoreMapData.GetPosition(assemblyIndex);
            int bank = CoreMapData.GetBank(assemblyIndex);
            string bankName = CoreMapData.GetBankName(assemblyIndex);

            // Create cell GameObject
            string cellName = $"Cell_{row:D2}_{col:D2}";
            GameObject cellGO = new GameObject(cellName);
            cellGO.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rect = cellGO.AddComponent<RectTransform>();
            _cellRects[assemblyIndex] = rect;

            // Calculate position (centered grid, row 0 at top)
            float halfGrid = gridSize / 2f;
            float x = col * (CellSize + CellGap) + CellSize / 2f - halfGrid;
            float y = halfGrid - row * (CellSize + CellGap) - CellSize / 2f;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(CellSize, CellSize);
            rect.anchoredPosition = new Vector2(x, y);

            // v4.1.0: Add background image with beveled cell sprite
            Image cellImage = cellGO.AddComponent<Image>();
            Sprite cellSprite = Resources.Load<Sprite>("Sprites/cell_bg");
            if (cellSprite != null)
            {
                cellImage.sprite = cellSprite;
                cellImage.type = Image.Type.Sliced;
            }
            cellImage.color = PowerColdColor;
            cellImage.raycastTarget = true;
            _cellImages[assemblyIndex] = cellImage;

            _cellObjects[assemblyIndex] = cellGO;

            // Add bank label if RCCA
            if (bank > 0 && ShowBankLabels)
            {
                GameObject labelGO = new GameObject("BankLabel");
                labelGO.transform.SetParent(cellGO.transform, false);

                RectTransform labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0.6f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                // v4.1.0: TMP bank label with instrument font
                TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
                labelText.text = bankName;
                TMP_FontAsset cellFont = Resources.Load<TMP_FontAsset>(
                    "Fonts & Materials/Electronic Highway Sign SDF");
                if (cellFont != null) labelText.font = cellFont;
                labelText.fontSize = 7;
                labelText.fontStyle = FontStyles.Bold;
                labelText.alignment = TextAlignmentOptions.Top;
                labelText.color = Color.white;
                labelText.enableWordWrapping = false;
                labelText.overflowMode = TextOverflowModes.Overflow;
                labelText.raycastTarget = false;

                _cellLabels[assemblyIndex] = labelText;
            }

            // Add insertion indicator if RCCA
            if (bank > 0 && ShowInsertionIndicators)
            {
                GameObject barGO = new GameObject("InsertionBar");
                barGO.transform.SetParent(cellGO.transform, false);

                RectTransform barRect = barGO.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0.1f, 0.1f);
                barRect.anchorMax = new Vector2(0.3f, 0.55f);
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;

                Image barImage = barGO.AddComponent<Image>();
                barImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                barImage.type = Image.Type.Filled;
                barImage.fillMethod = Image.FillMethod.Vertical;
                barImage.fillOrigin = (int)Image.OriginVertical.Top;
                barImage.fillAmount = 0f; // 0 = withdrawn, 1 = inserted
                barImage.raycastTarget = false;

                _insertionBars[assemblyIndex] = barImage;
            }
        }

        #endregion

        // ====================================================================
        // DATA FETCHING
        // ====================================================================

        #region Data Fetching

        private void FetchReactorData()
        {
            if (Board == null || Board.Reactor == null) return;

            ReactorController reactor = Board.Reactor;

            // For now, use uniform values from reactor
            // TODO: When ReactorCore exposes per-assembly data, fetch it here
            float avgPower = reactor.ThermalPower / 3411f; // Normalize to fraction
            float avgFuelTemp = reactor.FuelCenterline;
            float avgCoolantTemp = reactor.Tavg;

            // Get rod positions from reactor if available
            // TODO: Per-bank rod positions

            for (int i = 0; i < CoreMapData.ASSEMBLY_COUNT; i++)
            {
                // Apply slight variation for visual interest (placeholder)
                // In production, this would come from actual core physics
                float variation = 1f + (Mathf.PerlinNoise(i * 0.1f, Time.time * 0.1f) - 0.5f) * 0.3f;
                
                _assemblyPowers[i] = avgPower * variation;
                _fuelTemps[i] = avgFuelTemp * variation;
                _coolantTemps[i] = avgCoolantTemp + (variation - 1f) * 20f;

                // Rod positions - check if RCCA
                int bank = CoreMapData.GetBank(i);
                if (bank > 0)
                {
                    // TODO: Get actual rod position from ControlRodBank
                    _rodPositions[i] = ROD_WITHDRAWN; // Placeholder
                }
            }
        }

        #endregion

        // ====================================================================
        // CELL UPDATE
        // ====================================================================

        #region Cell Update

        private void UpdateAllCells()
        {
            for (int i = 0; i < CoreMapData.ASSEMBLY_COUNT; i++)
            {
                UpdateCell(i);
            }
        }

        private void UpdateCell(int assemblyIndex)
        {
            if (_cellImages[assemblyIndex] == null) return;

            Image cellImage = _cellImages[assemblyIndex];
            int bank = CoreMapData.GetBank(assemblyIndex);

            // Check filter
            bool dimmed = (FilteredBank > 0 && bank != FilteredBank);

            // Get base color based on display mode
            Color baseColor = GetCellColor(assemblyIndex);

            // Apply selection/hover highlight
            if (assemblyIndex == _selectedAssembly)
            {
                baseColor = Color.Lerp(baseColor, SelectedColor, 0.5f);
            }
            else if (assemblyIndex == _hoveredAssembly)
            {
                baseColor = Color.Lerp(baseColor, HoveredColor, 0.3f);
            }

            // Apply filter dimming
            if (dimmed)
            {
                baseColor = Color.Lerp(baseColor, Color.black, 0.7f);
            }

            cellImage.color = baseColor;

            // Update insertion bar if present
            if (_insertionBars[assemblyIndex] != null)
            {
                float rodPos = _rodPositions[assemblyIndex];
                float fillAmount = 1f - (rodPos / ROD_WITHDRAWN); // 0 = out, 1 = in
                _insertionBars[assemblyIndex].fillAmount = Mathf.Clamp01(fillAmount);

                // Color based on insertion
                Color barColor = Color.Lerp(
                    new Color(0.2f, 0.8f, 0.2f, 0.9f),  // Green when withdrawn
                    new Color(0.8f, 0.2f, 0.2f, 0.9f),  // Red when inserted
                    fillAmount
                );
                _insertionBars[assemblyIndex].color = dimmed ? Color.Lerp(barColor, Color.black, 0.7f) : barColor;
            }

            // Update label color if present
            if (_cellLabels[assemblyIndex] != null)
            {
                _cellLabels[assemblyIndex].color = dimmed ? Color.gray : Color.white;
            }
        }

        private Color GetCellColor(int assemblyIndex)
        {
            switch (DisplayMode)
            {
                case CoreMapDisplayMode.RelativePower:
                    float powerNorm = Mathf.InverseLerp(POWER_MIN, POWER_MAX, _assemblyPowers[assemblyIndex]);
                    return _powerGradient.Evaluate(powerNorm);

                case CoreMapDisplayMode.FuelTemperature:
                    float fuelNorm = Mathf.InverseLerp(FUEL_TEMP_MIN, FUEL_TEMP_MAX, _fuelTemps[assemblyIndex]);
                    return _fuelTempGradient.Evaluate(fuelNorm);

                case CoreMapDisplayMode.CoolantTemperature:
                    float coolantNorm = Mathf.InverseLerp(COOLANT_TEMP_MIN, COOLANT_TEMP_MAX, _coolantTemps[assemblyIndex]);
                    return _coolantTempGradient.Evaluate(coolantNorm);

                case CoreMapDisplayMode.RodBankOverlay:
                    return CoreMapData.GetBankColor(assemblyIndex);

                default:
                    return Color.gray;
            }
        }

        #endregion

        // ====================================================================
        // POINTER EVENTS
        // ====================================================================

        #region Pointer Events

        public void OnPointerMove(PointerEventData eventData)
        {
            int assembly = GetAssemblyAtScreenPosition(eventData.position);
            
            if (assembly != _hoveredAssembly)
            {
                _hoveredAssembly = assembly;
                
                if (assembly >= 0)
                {
                    ShowTooltip(assembly);
                }
                else
                {
                    HideTooltip();
                }

                // Refresh affected cells
                UpdateAllCells();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoveredAssembly = -1;
            HideTooltip();
            UpdateAllCells();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int assembly = GetAssemblyAtScreenPosition(eventData.position);

            if (assembly >= 0)
            {
                // Toggle selection
                if (_selectedAssembly == assembly)
                {
                    _selectedAssembly = -1;
                    HideDetailPanel();
                }
                else
                {
                    _selectedAssembly = assembly;
                    ShowDetailPanel(assembly);
                }

                UpdateAllCells();
            }
        }

        private int GetAssemblyAtScreenPosition(Vector2 screenPosition)
        {
            // Raycast to find which cell was hit
            for (int i = 0; i < CoreMapData.ASSEMBLY_COUNT; i++)
            {
                if (_cellRects[i] == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(_cellRects[i], screenPosition, null))
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        // ====================================================================
        // TOOLTIP
        // ====================================================================

        #region Tooltip

        private void ShowTooltip(int assemblyIndex)
        {
            if (TooltipPanel == null || TooltipText == null) return;

            TooltipPanel.SetActive(true);
            TooltipText.text = GetAssemblyTooltipText(assemblyIndex);
        }

        private void HideTooltip()
        {
            if (TooltipPanel != null)
            {
                TooltipPanel.SetActive(false);
            }
        }

        private void UpdateTooltip()
        {
            if (TooltipPanel == null || !TooltipPanel.activeSelf) return;

            // Position tooltip near mouse (New Input System — legacy Input API is disabled)
            Vector3 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
            TooltipPanel.transform.position = mousePos + new Vector3(15f, -15f, 0f);

            // Keep on screen
            RectTransform tooltipRect = TooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                Vector3 pos = TooltipPanel.transform.position;
                pos.x = Mathf.Clamp(pos.x, 0f, Screen.width - tooltipRect.sizeDelta.x);
                pos.y = Mathf.Clamp(pos.y, tooltipRect.sizeDelta.y, Screen.height);
                TooltipPanel.transform.position = pos;
            }
        }

        private string GetAssemblyTooltipText(int assemblyIndex)
        {
            string coord = CoreMapData.GetCoordinateString(assemblyIndex);
            string type = CoreMapData.GetAssemblyType(assemblyIndex);
            
            float power = _assemblyPowers[assemblyIndex];
            float fuelTemp = _fuelTemps[assemblyIndex];
            float coolantTemp = _coolantTemps[assemblyIndex];

            string text = $"{coord} - Assembly #{assemblyIndex + 1}\n";
            text += $"{type}\n";
            text += $"Rel. Power: {power:F3}\n";
            text += $"Fuel Temp: {fuelTemp:F0}°F\n";
            text += $"Coolant: {coolantTemp:F1}°F";

            // Add rod info if RCCA
            if (CoreMapData.HasRCCA(assemblyIndex))
            {
                float rodPos = _rodPositions[assemblyIndex];
                text += $"\nRod Position: {rodPos:F0} steps";
            }

            return text;
        }

        #endregion

        // ====================================================================
        // DETAIL PANEL
        // ====================================================================

        #region Detail Panel

        private void ShowDetailPanel(int assemblyIndex)
        {
            if (DetailPanel != null)
            {
                DetailPanel.ShowAssembly(assemblyIndex, this);
            }
        }

        private void HideDetailPanel()
        {
            if (DetailPanel != null)
            {
                DetailPanel.Hide();
            }
        }

        #endregion

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================

        #region Public Methods

        /// <summary>
        /// Set display mode and refresh.
        /// </summary>
        public void SetDisplayMode(CoreMapDisplayMode mode)
        {
            DisplayMode = mode;
            UpdateAllCells();
        }

        /// <summary>
        /// Set bank filter (0 = show all, 1-8 = specific bank).
        /// </summary>
        public void SetBankFilter(int bankIndex)
        {
            FilteredBank = Mathf.Clamp(bankIndex, 0, CoreMapData.BANK_COUNT);
            UpdateAllCells();
        }

        /// <summary>
        /// Clear selection.
        /// </summary>
        public void ClearSelection()
        {
            _selectedAssembly = -1;
            HideDetailPanel();
            UpdateAllCells();
        }

        /// <summary>
        /// Get currently selected assembly index (-1 if none).
        /// </summary>
        public int GetSelectedAssembly()
        {
            return _selectedAssembly;
        }

        /// <summary>
        /// Get data for an assembly (for detail panel).
        /// </summary>
        public void GetAssemblyData(int assemblyIndex, out float power, out float fuelTemp, 
            out float coolantTemp, out float rodPosition)
        {
            if (assemblyIndex < 0 || assemblyIndex >= CoreMapData.ASSEMBLY_COUNT)
            {
                power = 0f;
                fuelTemp = 0f;
                coolantTemp = 0f;
                rodPosition = 0f;
                return;
            }

            power = _assemblyPowers[assemblyIndex];
            fuelTemp = _fuelTemps[assemblyIndex];
            coolantTemp = _coolantTemps[assemblyIndex];
            rodPosition = _rodPositions[assemblyIndex];
        }

        /// <summary>
        /// Force immediate refresh of all cells.
        /// </summary>
        public void Refresh()
        {
            FetchReactorData();
            UpdateAllCells();
        }

        #endregion

        // ====================================================================
        // DISPLAY MODE BUTTON HANDLERS
        // ====================================================================

        #region Button Handlers

        public void OnPowerModeClick() => SetDisplayMode(CoreMapDisplayMode.RelativePower);
        public void OnFuelTempModeClick() => SetDisplayMode(CoreMapDisplayMode.FuelTemperature);
        public void OnCoolantTempModeClick() => SetDisplayMode(CoreMapDisplayMode.CoolantTemperature);
        public void OnRodBankModeClick() => SetDisplayMode(CoreMapDisplayMode.RodBankOverlay);

        public void OnBankFilterAll() => SetBankFilter(0);
        public void OnBankFilterSA() => SetBankFilter(CoreMapData.BANK_SA);
        public void OnBankFilterSB() => SetBankFilter(CoreMapData.BANK_SB);
        public void OnBankFilterSC() => SetBankFilter(CoreMapData.BANK_SC);
        public void OnBankFilterSD() => SetBankFilter(CoreMapData.BANK_SD);
        public void OnBankFilterD() => SetBankFilter(CoreMapData.BANK_D);
        public void OnBankFilterC() => SetBankFilter(CoreMapData.BANK_C);
        public void OnBankFilterB() => SetBankFilter(CoreMapData.BANK_B);
        public void OnBankFilterA() => SetBankFilter(CoreMapData.BANK_A);

        #endregion
    }
}
