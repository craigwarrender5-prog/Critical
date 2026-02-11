// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// AssemblyDetailPanel.cs - Assembly Detail Display Panel
// ============================================================================
//
// PURPOSE:
//   Displays detailed information about a selected fuel assembly when the
//   operator clicks on a cell in the core mosaic map. Shows assembly
//   identification, physics parameters, and RCCA data if applicable.
//
// DISPLAY CONTENT:
//   - Assembly identification: index, grid coordinates, type
//   - Relative power fraction
//   - Fuel centerline temperature
//   - Coolant outlet temperature
//   - RCCA data (if present): bank name, rod position, insertion %
//   - Close button / click-away dismiss
//
// ARCHITECTURE:
//   - Activated by CoreMosaicMap.ShowDetailPanel()
//   - Reads data via CoreMosaicMap.GetAssemblyData()
//   - Updates at same rate as core map (2 Hz)
//   - Hidden by default, shown on assembly selection
//
// SOURCES:
//   - ReactorOperatorGUI_Design_v1_0_0_0.md Section 5.2
//   - Unity_Implementation_Manual_v1_0_0_0.md Chapter 12
//
// GOLD STANDARD: Yes
// CHANGE: v4.1.0 — All Text fields changed from legacy Text to
//         TextMeshProUGUI. CreateText() helper updated to use TMP.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    /// <summary>
    /// Floating detail panel showing selected assembly information.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AssemblyDetailPanel : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Panel Settings")]
        [Tooltip("Update rate for live data (Hz)")]
        [Range(1f, 10f)]
        public float UpdateRate = 2f;

        [Header("Color Settings")]
        public Color PanelBackgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);  // #1E1E28
        public Color HeaderColor = new Color(0.78f, 0.82f, 0.85f);                   // #C8D0D8
        public Color LabelColor = new Color(0.50f, 0.56f, 0.63f);                    // #8090A0
        public Color ValueColor = new Color(0f, 1f, 0.53f);                          // #00FF88 green
        public Color WarningColor = new Color(1f, 0.72f, 0.19f);                     // #FFB830 amber
        public Color AlarmColor = new Color(1f, 0.20f, 0.27f);                       // #FF3344 red
        public Color RCCAColor = new Color(0f, 0.80f, 1f);                           // #00CCFF cyan

        [Header("Text References - Header")]
        public TextMeshProUGUI HeaderText;
        public TextMeshProUGUI CoordinateText;
        public TextMeshProUGUI TypeText;

        [Header("Text References - Power")]
        public TextMeshProUGUI PowerLabel;
        public TextMeshProUGUI PowerValue;
        public Image PowerBar;

        [Header("Text References - Fuel Temperature")]
        public TextMeshProUGUI FuelTempLabel;
        public TextMeshProUGUI FuelTempValue;
        public Image FuelTempBar;

        [Header("Text References - Coolant Temperature")]
        public TextMeshProUGUI CoolantTempLabel;
        public TextMeshProUGUI CoolantTempValue;

        [Header("Text References - RCCA (optional)")]
        public GameObject RCCASection;
        public TextMeshProUGUI RCCABankLabel;
        public TextMeshProUGUI RCCABankValue;
        public TextMeshProUGUI RCCAPositionLabel;
        public TextMeshProUGUI RCCAPositionValue;
        public Image RCCAPositionBar;
        public TextMeshProUGUI RCCAInsertionLabel;
        public TextMeshProUGUI RCCAInsertionValue;

        [Header("Controls")]
        public Button CloseButton;

        [Header("References")]
        public Image BackgroundImage;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private int _currentAssembly = -1;
        private CoreMosaicMap _coreMap;
        private float _lastUpdateTime;
        private bool _isVisible;

        // Data ranges for bar displays
        private const float POWER_MIN = 0f;
        private const float POWER_MAX = 2f;
        private const float FUEL_TEMP_MIN = 500f;
        private const float FUEL_TEMP_MAX = 4000f;
        private const float FUEL_TEMP_WARNING = 3000f;
        private const float FUEL_TEMP_ALARM = 3500f;
        private const float ROD_STEPS_MAX = 228f;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            // Set background color
            if (BackgroundImage != null)
            {
                BackgroundImage.color = PanelBackgroundColor;
            }

            // Wire close button
            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(OnCloseClicked);
            }

            // Start hidden
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isVisible || _currentAssembly < 0 || _coreMap == null) return;

            // Throttle updates
            float updateInterval = 1f / UpdateRate;
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                _lastUpdateTime = Time.time;
                UpdateDisplay();
            }
        }

        private void OnDestroy()
        {
            if (CloseButton != null)
            {
                CloseButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        #endregion

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================

        #region Public Methods

        /// <summary>
        /// Show detail panel for specified assembly.
        /// </summary>
        public void ShowAssembly(int assemblyIndex, CoreMosaicMap coreMap)
        {
            if (assemblyIndex < 0 || assemblyIndex >= CoreMapData.ASSEMBLY_COUNT)
            {
                Hide();
                return;
            }

            _currentAssembly = assemblyIndex;
            _coreMap = coreMap;
            _isVisible = true;

            // Show panel
            gameObject.SetActive(true);

            // Update static info
            UpdateStaticInfo();

            // Update live data
            UpdateDisplay();

            Debug.Log($"[AssemblyDetailPanel] Showing assembly {assemblyIndex}");
        }

        /// <summary>
        /// Hide the detail panel.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            _currentAssembly = -1;
            _coreMap = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Check if panel is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Get currently displayed assembly index.
        /// </summary>
        public int CurrentAssembly => _currentAssembly;

        #endregion

        // ====================================================================
        // DISPLAY UPDATE
        // ====================================================================

        #region Display Update

        private void UpdateStaticInfo()
        {
            if (_currentAssembly < 0) return;

            // Header - Assembly number
            if (HeaderText != null)
            {
                HeaderText.text = $"ASSEMBLY #{_currentAssembly + 1}";
                HeaderText.color = HeaderColor;
            }

            // Coordinates
            if (CoordinateText != null)
            {
                string coord = CoreMapData.GetCoordinateString(_currentAssembly);
                var (row, col) = CoreMapData.GetPosition(_currentAssembly);
                CoordinateText.text = $"{coord}  (Row {row + 1}, Col {col + 1})";
                CoordinateText.color = LabelColor;
            }

            // Type
            if (TypeText != null)
            {
                TypeText.text = CoreMapData.GetAssemblyType(_currentAssembly);
                TypeText.color = CoreMapData.HasRCCA(_currentAssembly) ? RCCAColor : LabelColor;
            }

            // Show/hide RCCA section
            if (RCCASection != null)
            {
                bool hasRCCA = CoreMapData.HasRCCA(_currentAssembly);
                RCCASection.SetActive(hasRCCA);

                if (hasRCCA)
                {
                    UpdateRCCAStaticInfo();
                }
            }

            // Set label colors
            SetLabelColors();
        }

        private void UpdateRCCAStaticInfo()
        {
            int bank = CoreMapData.GetBank(_currentAssembly);
            string bankName = CoreMapData.GetBankName(_currentAssembly);
            bool isShutdown = CoreMapData.IsShutdownBank(_currentAssembly);
            Color bankColor = CoreMapData.GetBankColor(_currentAssembly);

            if (RCCABankLabel != null)
            {
                RCCABankLabel.text = "BANK:";
                RCCABankLabel.color = LabelColor;
            }

            if (RCCABankValue != null)
            {
                string bankType = isShutdown ? "Shutdown" : "Control";
                RCCABankValue.text = $"{bankName} ({bankType})";
                RCCABankValue.color = bankColor;
            }

            if (RCCAPositionLabel != null)
            {
                RCCAPositionLabel.text = "POSITION:";
                RCCAPositionLabel.color = LabelColor;
            }

            if (RCCAInsertionLabel != null)
            {
                RCCAInsertionLabel.text = "INSERTION:";
                RCCAInsertionLabel.color = LabelColor;
            }
        }

        private void SetLabelColors()
        {
            if (PowerLabel != null) PowerLabel.color = LabelColor;
            if (FuelTempLabel != null) FuelTempLabel.color = LabelColor;
            if (CoolantTempLabel != null) CoolantTempLabel.color = LabelColor;
        }

        private void UpdateDisplay()
        {
            if (_currentAssembly < 0 || _coreMap == null) return;

            // Get current data from core map
            _coreMap.GetAssemblyData(_currentAssembly, 
                out float power, 
                out float fuelTemp, 
                out float coolantTemp, 
                out float rodPosition);

            // Update power display
            UpdatePowerDisplay(power);

            // Update fuel temperature display
            UpdateFuelTempDisplay(fuelTemp);

            // Update coolant temperature display
            UpdateCoolantTempDisplay(coolantTemp);

            // Update RCCA display if applicable
            if (CoreMapData.HasRCCA(_currentAssembly))
            {
                UpdateRCCADisplay(rodPosition);
            }
        }

        private void UpdatePowerDisplay(float power)
        {
            if (PowerValue != null)
            {
                PowerValue.text = $"{power:F3}";
                
                // Color based on power level
                if (power > 1.5f)
                    PowerValue.color = AlarmColor;
                else if (power > 1.2f)
                    PowerValue.color = WarningColor;
                else
                    PowerValue.color = ValueColor;
            }

            if (PowerBar != null)
            {
                float normalized = Mathf.InverseLerp(POWER_MIN, POWER_MAX, power);
                PowerBar.fillAmount = Mathf.Clamp01(normalized);

                // Color gradient
                if (power > 1.5f)
                    PowerBar.color = AlarmColor;
                else if (power > 1.2f)
                    PowerBar.color = WarningColor;
                else
                    PowerBar.color = ValueColor;
            }
        }

        private void UpdateFuelTempDisplay(float fuelTemp)
        {
            if (FuelTempValue != null)
            {
                FuelTempValue.text = $"{fuelTemp:F0} °F";

                // Color based on temperature
                if (fuelTemp > FUEL_TEMP_ALARM)
                    FuelTempValue.color = AlarmColor;
                else if (fuelTemp > FUEL_TEMP_WARNING)
                    FuelTempValue.color = WarningColor;
                else
                    FuelTempValue.color = ValueColor;
            }

            if (FuelTempBar != null)
            {
                float normalized = Mathf.InverseLerp(FUEL_TEMP_MIN, FUEL_TEMP_MAX, fuelTemp);
                FuelTempBar.fillAmount = Mathf.Clamp01(normalized);

                // Color gradient
                if (fuelTemp > FUEL_TEMP_ALARM)
                    FuelTempBar.color = AlarmColor;
                else if (fuelTemp > FUEL_TEMP_WARNING)
                    FuelTempBar.color = WarningColor;
                else
                    FuelTempBar.color = ValueColor;
            }
        }

        private void UpdateCoolantTempDisplay(float coolantTemp)
        {
            if (CoolantTempValue != null)
            {
                CoolantTempValue.text = $"{coolantTemp:F1} °F";
                CoolantTempValue.color = ValueColor;
            }
        }

        private void UpdateRCCADisplay(float rodPosition)
        {
            // Position in steps
            if (RCCAPositionValue != null)
            {
                RCCAPositionValue.text = $"{rodPosition:F0} / {ROD_STEPS_MAX:F0} steps";
                RCCAPositionValue.color = ValueColor;
            }

            // Position bar (0 = inserted, 228 = withdrawn)
            if (RCCAPositionBar != null)
            {
                float normalized = rodPosition / ROD_STEPS_MAX;
                RCCAPositionBar.fillAmount = Mathf.Clamp01(normalized);

                // Green when withdrawn, red when inserted
                RCCAPositionBar.color = Color.Lerp(AlarmColor, ValueColor, normalized);
            }

            // Insertion percentage
            if (RCCAInsertionValue != null)
            {
                float insertionPercent = (1f - (rodPosition / ROD_STEPS_MAX)) * 100f;
                RCCAInsertionValue.text = $"{insertionPercent:F1}%";

                // Red when significantly inserted
                if (insertionPercent > 50f)
                    RCCAInsertionValue.color = AlarmColor;
                else if (insertionPercent > 20f)
                    RCCAInsertionValue.color = WarningColor;
                else
                    RCCAInsertionValue.color = ValueColor;
            }
        }

        #endregion

        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================

        #region Event Handlers

        private void OnCloseClicked()
        {
            // Tell the core map to clear selection
            if (_coreMap != null)
            {
                _coreMap.ClearSelection();
            }
            else
            {
                Hide();
            }
        }

        #endregion

        // ====================================================================
        // STATIC CREATION HELPER
        // ====================================================================

        #region Static Creation

        /// <summary>
        /// Create a complete detail panel with all UI elements.
        /// Used by OperatorScreenBuilder.
        /// </summary>
        public static AssemblyDetailPanel CreateDetailPanel(Transform parent, string name = "AssemblyDetailPanel")
        {
            // Create panel GameObject
            GameObject panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.80f, 0.26f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);

            // Add background image
            Image bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

            // Add the component
            AssemblyDetailPanel panel = panelGO.AddComponent<AssemblyDetailPanel>();
            panel.BackgroundImage = bg;

            // Create header section
            CreateHeaderSection(panelGO.transform, panel);

            // Create power section
            CreatePowerSection(panelGO.transform, panel);

            // Create fuel temp section
            CreateFuelTempSection(panelGO.transform, panel);

            // Create coolant temp section
            CreateCoolantTempSection(panelGO.transform, panel);

            // Create RCCA section
            CreateRCCASection(panelGO.transform, panel);

            // Create close button
            CreateCloseButton(panelGO.transform, panel);

            // Start inactive
            panelGO.SetActive(false);

            return panel;
        }

        private static void CreateHeaderSection(Transform parent, AssemblyDetailPanel panel)
        {
            float yPos = -10f;

            // Header text (Assembly #)
            panel.HeaderText = CreateText(parent, "HeaderText", "ASSEMBLY #1", 16, FontStyle.Bold,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 24f), TextAnchor.MiddleLeft);
            yPos -= 28f;

            // Coordinate text
            panel.CoordinateText = CreateText(parent, "CoordinateText", "H-08 (Row 8, Col 8)", 11, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 18f), TextAnchor.MiddleLeft);
            yPos -= 22f;

            // Type text
            panel.TypeText = CreateText(parent, "TypeText", "Fuel Assembly", 11, FontStyle.Italic,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 18f), TextAnchor.MiddleLeft);

            // Divider line
            CreateDivider(parent, -75f);
        }

        private static void CreatePowerSection(Transform parent, AssemblyDetailPanel panel)
        {
            float yPos = -85f;

            // Label
            panel.PowerLabel = CreateText(parent, "PowerLabel", "RELATIVE POWER", 10, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
            yPos -= 20f;

            // Value
            panel.PowerValue = CreateText(parent, "PowerValue", "1.000", 14, FontStyle.Bold,
                new Vector2(10f, yPos), new Vector2(80f, yPos + 20f), TextAnchor.MiddleLeft);

            // Bar
            panel.PowerBar = CreateBar(parent, "PowerBar",
                new Vector2(90f, yPos + 4f), new Vector2(-10f, yPos + 16f));
        }

        private static void CreateFuelTempSection(Transform parent, AssemblyDetailPanel panel)
        {
            float yPos = -135f;

            // Label
            panel.FuelTempLabel = CreateText(parent, "FuelTempLabel", "FUEL CENTERLINE", 10, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
            yPos -= 20f;

            // Value
            panel.FuelTempValue = CreateText(parent, "FuelTempValue", "1200 °F", 14, FontStyle.Bold,
                new Vector2(10f, yPos), new Vector2(100f, yPos + 20f), TextAnchor.MiddleLeft);

            // Bar
            panel.FuelTempBar = CreateBar(parent, "FuelTempBar",
                new Vector2(110f, yPos + 4f), new Vector2(-10f, yPos + 16f));
        }

        private static void CreateCoolantTempSection(Transform parent, AssemblyDetailPanel panel)
        {
            float yPos = -185f;

            // Label
            panel.CoolantTempLabel = CreateText(parent, "CoolantTempLabel", "COOLANT OUTLET", 10, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
            yPos -= 20f;

            // Value
            panel.CoolantTempValue = CreateText(parent, "CoolantTempValue", "580.0 °F", 14, FontStyle.Bold,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 20f), TextAnchor.MiddleLeft);

            // Divider
            CreateDivider(parent, -230f);
        }

        private static void CreateRCCASection(Transform parent, AssemblyDetailPanel panel)
        {
            // Container for RCCA info (can be hidden)
            GameObject rccarSection = new GameObject("RCCASection");
            rccarSection.transform.SetParent(parent, false);

            RectTransform sectionRect = rccarSection.AddComponent<RectTransform>();
            sectionRect.anchorMin = Vector2.zero;
            sectionRect.anchorMax = Vector2.one;
            sectionRect.offsetMin = Vector2.zero;
            sectionRect.offsetMax = Vector2.zero;

            panel.RCCASection = rccarSection;

            float yPos = -240f;

            // Section header
            CreateText(rccarSection.transform, "RCCASectionHeader", "ROD CLUSTER CONTROL", 10, FontStyle.Bold,
                new Vector2(10f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
            yPos -= 24f;

            // Bank
            panel.RCCABankLabel = CreateText(rccarSection.transform, "RCCABankLabel", "BANK:", 10, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(70f, yPos + 16f), TextAnchor.MiddleLeft);
            panel.RCCABankValue = CreateText(rccarSection.transform, "RCCABankValue", "D (Control)", 11, FontStyle.Bold,
                new Vector2(75f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
            yPos -= 22f;

            // Position
            panel.RCCAPositionLabel = CreateText(rccarSection.transform, "RCCAPositionLabel", "POSITION:", 10, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(70f, yPos + 16f), TextAnchor.MiddleLeft);
            panel.RCCAPositionValue = CreateText(rccarSection.transform, "RCCAPositionValue", "228 / 228 steps", 11, FontStyle.Bold,
                new Vector2(75f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
            yPos -= 22f;

            // Position bar
            panel.RCCAPositionBar = CreateBar(rccarSection.transform, "RCCAPositionBar",
                new Vector2(10f, yPos + 4f), new Vector2(-10f, yPos + 14f));
            yPos -= 22f;

            // Insertion percentage
            panel.RCCAInsertionLabel = CreateText(rccarSection.transform, "RCCAInsertionLabel", "INSERTION:", 10, FontStyle.Normal,
                new Vector2(10f, yPos), new Vector2(80f, yPos + 16f), TextAnchor.MiddleLeft);
            panel.RCCAInsertionValue = CreateText(rccarSection.transform, "RCCAInsertionValue", "0.0%", 11, FontStyle.Bold,
                new Vector2(85f, yPos), new Vector2(-10f, yPos + 16f), TextAnchor.MiddleLeft);
        }

        private static void CreateCloseButton(Transform parent, AssemblyDetailPanel panel)
        {
            GameObject btnGO = new GameObject("CloseButton");
            btnGO.transform.SetParent(parent, false);

            RectTransform btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 1f);
            btnRect.anchorMax = new Vector2(1f, 1f);
            btnRect.pivot = new Vector2(1f, 1f);
            btnRect.sizeDelta = new Vector2(30f, 30f);
            btnRect.anchoredPosition = new Vector2(-5f, -5f);

            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.3f, 0.35f);

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            // X label
            GameObject xLabel = new GameObject("XLabel");
            xLabel.transform.SetParent(btnGO.transform, false);

            RectTransform xRect = xLabel.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            TextMeshProUGUI xText = xLabel.AddComponent<TextMeshProUGUI>();
            xText.text = "✕";
            TMP_FontAsset closeFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            if (closeFont != null) xText.font = closeFont;
            xText.fontSize = 16;
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = Color.white;
            xText.raycastTarget = false;

            panel.CloseButton = btn;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string defaultText, int fontSize, 
            FontStyle style, Vector2 offsetMin, Vector2 offsetMax, TextAnchor alignment)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(offsetMin.x, 0f);
            rect.offsetMax = new Vector2(offsetMax.x, 0f);
            rect.anchoredPosition = new Vector2(0f, offsetMin.y);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, offsetMax.y - offsetMin.y);

            // v4.1.0: TMP text with appropriate font
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            
            // Use instrument font for values (Bold), label font for labels/headers
            bool isValue = (style == FontStyle.Bold || style == FontStyle.BoldAndItalic);
            TMP_FontAsset font = isValue
                ? Resources.Load<TMP_FontAsset>("Fonts & Materials/Electronic Highway Sign SDF")
                : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) text.font = font;
            
            text.fontSize = fontSize;
            text.fontStyle = style switch
            {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
            
            // Convert legacy TextAnchor to TMP alignment
            text.alignment = alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Left
            };
            
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.raycastTarget = false;

            return text;
        }

        private static Image CreateBar(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
        {
            // Background
            GameObject bgGO = new GameObject(name + "Bg");
            bgGO.transform.SetParent(parent, false);

            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.offsetMin = new Vector2(offsetMin.x, 0f);
            bgRect.offsetMax = new Vector2(offsetMax.x, 0f);
            bgRect.anchoredPosition = new Vector2(0f, offsetMin.y);
            bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, offsetMax.y - offsetMin.y);

            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.12f);

            // Fill bar
            GameObject fillGO = new GameObject(name);
            fillGO.transform.SetParent(bgGO.transform, false);

            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);

            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0f, 1f, 0.53f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0.5f;

            return fillImage;
        }

        private static void CreateDivider(Transform parent, float yPos)
        {
            GameObject divGO = new GameObject("Divider");
            divGO.transform.SetParent(parent, false);

            RectTransform rect = divGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, yPos);
            rect.sizeDelta = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);

            Image divImage = divGO.AddComponent<Image>();
            divImage.color = new Color(0.2f, 0.2f, 0.25f);
        }

        #endregion
    }
}
