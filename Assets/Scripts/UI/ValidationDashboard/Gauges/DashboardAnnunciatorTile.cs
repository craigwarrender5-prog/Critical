// ============================================================================
// CRITICAL: Master the Atom - Dashboard Annunciator Tile
// DashboardAnnunciatorTile.cs - ISA-18.1 Compliant Alarm Tile
// ============================================================================
//
// PURPOSE:
//   Annunciator tile for the Validation Dashboard with full ISA-18.1
//   alarm sequence: INACTIVE → ALERTING → ACKNOWLEDGED → CLEARING → INACTIVE.
//   Visual style adopted from MosaicAlarmPanel.cs (proven, professional).
//
// VISUAL STANDARD:
//   - 4-edge 1px borders (top, bottom, left, right Image components)
//   - Instrument font ("Electronic Highway Sign SDF")
//   - Dark when inactive, illuminated when active
//   - Green = status, Amber = warning, Red = alarm
//   - Fast flash (3 Hz) for ALERTING, steady for ACKNOWLEDGED,
//     slow flash (0.7 Hz) for CLEARING
//
// REFERENCE:
//   NRC HRTD Section 4 — Annunciator Window Tile conventions
//   ANSI/ISA-18.1 — Annunciator Sequences and Specifications
//   MosaicAlarmPanel.cs — project visual standard
//
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    // ========================================================================
    // ANNUNCIATOR STATE ENUM (ISA-18.1)
    // ========================================================================

    /// <summary>
    /// ISA-18.1 annunciator state sequence.
    /// </summary>
    public enum AnnunciatorState
    {
        /// <summary>Dark, condition not active.</summary>
        Inactive,

        /// <summary>Fast flash (3 Hz), new alarm not yet acknowledged.</summary>
        Alerting,

        /// <summary>Steady on, alarm acknowledged but condition still active.</summary>
        Acknowledged,

        /// <summary>Slow flash (0.7 Hz), condition cleared but not yet reset.</summary>
        Clearing
    }

    // ========================================================================
    // TILE DESCRIPTOR
    // ========================================================================

    /// <summary>
    /// Describes a single annunciator tile definition.
    /// </summary>
    public struct AnnunciatorTileDescriptor
    {
        public string Label;
        public bool IsAlarm;     // true = red when active
        public bool IsWarning;   // true = amber when active (overrides IsAlarm=false)

        public AnnunciatorTileDescriptor(string label, bool isAlarm, bool isWarning = false)
        {
            Label = label;
            IsAlarm = isAlarm;
            IsWarning = isWarning;
        }

        /// <summary>True if this is a status indicator (green when lit, no state machine).</summary>
        public bool IsStatus => !IsAlarm && !IsWarning;
    }

    // ========================================================================
    // DASHBOARD ANNUNCIATOR TILE
    // ========================================================================

    /// <summary>
    /// Single annunciator tile with ISA-18.1 state machine and
    /// MosaicAlarmPanel visual style.
    /// </summary>
    public class DashboardAnnunciatorTile : MonoBehaviour
    {
        // ====================================================================
        // UI REFERENCES
        // ====================================================================

        private Image _background;
        private Image _borderTop;
        private Image _borderBottom;
        private Image _borderLeft;
        private Image _borderRight;
        private TextMeshProUGUI _label;

        // ====================================================================
        // TILE STATE
        // ====================================================================

        private AnnunciatorTileDescriptor _descriptor;
        private AnnunciatorState _state = AnnunciatorState.Inactive;
        private bool _conditionActive;
        private float _flashTimer;
        private float _clearingEnteredTime;

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        public AnnunciatorState State => _state;
        public bool ConditionActive => _conditionActive;
        public AnnunciatorTileDescriptor Descriptor => _descriptor;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Update()
        {
            // Status tiles don't flash — skip
            if (_descriptor.IsStatus) return;

            switch (_state)
            {
                case AnnunciatorState.Alerting:
                    _flashTimer += Time.unscaledDeltaTime;
                    UpdateFlashVisual(ValidationDashboardTheme.AnnunciatorAlertFlashHz);
                    break;

                case AnnunciatorState.Clearing:
                    _flashTimer += Time.unscaledDeltaTime;
                    UpdateFlashVisual(ValidationDashboardTheme.AnnunciatorClearFlashHz);

                    // Auto-reset after delay
                    if (Time.unscaledTime - _clearingEnteredTime >= ValidationDashboardTheme.AnnunciatorAutoResetDelay)
                    {
                        Reset();
                    }
                    break;
            }
        }

        // ====================================================================
        // ISA-18.1 STATE MACHINE
        // ====================================================================

        /// <summary>
        /// Update the condition (called each data refresh cycle).
        /// Drives ISA-18.1 state transitions.
        /// </summary>
        public void UpdateCondition(bool active)
        {
            bool wasActive = _conditionActive;
            _conditionActive = active;

            // Status tiles: simple on/off, no state machine
            if (_descriptor.IsStatus)
            {
                ApplyColors(active);
                return;
            }

            // ISA-18.1 transitions
            if (active && !wasActive)
            {
                // Condition onset
                if (_state == AnnunciatorState.Inactive || _state == AnnunciatorState.Clearing)
                {
                    _state = AnnunciatorState.Alerting;
                    _flashTimer = 0f;
                    ApplyLitColors();
                }
            }
            else if (!active && wasActive)
            {
                // Condition cleared
                switch (_state)
                {
                    case AnnunciatorState.Alerting:
                        // Self-cleared before ACK → go to clearing (slow flash)
                        _state = AnnunciatorState.Clearing;
                        _flashTimer = 0f;
                        _clearingEnteredTime = Time.unscaledTime;
                        break;

                    case AnnunciatorState.Acknowledged:
                        // Normal path: ACK'd then cleared → clearing
                        _state = AnnunciatorState.Clearing;
                        _flashTimer = 0f;
                        _clearingEnteredTime = Time.unscaledTime;
                        break;
                }
            }
        }

        /// <summary>
        /// Acknowledge this tile. ALERTING → ACKNOWLEDGED.
        /// </summary>
        public void Acknowledge()
        {
            if (_state == AnnunciatorState.Alerting)
            {
                _state = AnnunciatorState.Acknowledged;
                _flashTimer = 0f;
                ApplyLitColors(); // Steady on
            }
        }

        /// <summary>
        /// Reset this tile. CLEARING → INACTIVE.
        /// </summary>
        public void Reset()
        {
            if (_state == AnnunciatorState.Clearing)
            {
                _state = AnnunciatorState.Inactive;
                _flashTimer = 0f;
                ApplyColors(false);
            }
        }

        // ====================================================================
        // VISUAL UPDATES
        // ====================================================================

        private void UpdateFlashVisual(float flashHz)
        {
            float phase = (_flashTimer * flashHz) % 1f;
            bool lit = phase < 0.5f;

            if (lit)
                ApplyLitColors();
            else
                ApplyColors(false); // Momentary dark
        }

        private void ApplyColors(bool lit)
        {
            if (lit)
            {
                ApplyLitColors();
            }
            else
            {
                // Dark / inactive
                if (_background != null) _background.color = ValidationDashboardTheme.AnnunciatorOff;
                if (_label != null) _label.color = ValidationDashboardTheme.AnnunciatorTextDim;
                SetBorderColor(ValidationDashboardTheme.AnnunciatorBorderDim);
            }
        }

        private void ApplyLitColors()
        {
            Color bgColor, textColor;

            if (_descriptor.IsAlarm)
            {
                bgColor = ValidationDashboardTheme.AnnunciatorAlarm;
                textColor = ValidationDashboardTheme.AnnunciatorTextRed;
            }
            else if (_descriptor.IsWarning)
            {
                bgColor = ValidationDashboardTheme.AnnunciatorWarning;
                textColor = ValidationDashboardTheme.AnnunciatorTextAmber;
            }
            else
            {
                // Status (green)
                bgColor = ValidationDashboardTheme.AnnunciatorNormal;
                textColor = ValidationDashboardTheme.AnnunciatorTextGreen;
            }

            if (_background != null) _background.color = bgColor;
            if (_label != null) _label.color = textColor;
            SetBorderColor(textColor);
        }

        private void SetBorderColor(Color color)
        {
            if (_borderTop != null) _borderTop.color = color;
            if (_borderBottom != null) _borderBottom.color = color;
            if (_borderLeft != null) _borderLeft.color = color;
            if (_borderRight != null) _borderRight.color = color;
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a single annunciator tile following MosaicAlarmPanel pattern.
        /// </summary>
        public static DashboardAnnunciatorTile Create(Transform parent, AnnunciatorTileDescriptor descriptor)
        {
            string safeName = descriptor.Label.Replace("\n", "_").Replace(" ", "");
            GameObject root = new GameObject($"Ann_{safeName}");
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();

            // Background
            Image bg = root.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.AnnunciatorOff;
            bg.raycastTarget = false;

            // 4-edge borders
            float borderPx = 1f;
            Image bTop = CreateBorderEdge(root.transform, "BorderTop",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -borderPx), Vector2.zero);
            Image bBot = CreateBorderEdge(root.transform, "BorderBottom",
                new Vector2(0, 0), new Vector2(1, 0),
                Vector2.zero, new Vector2(0, borderPx));
            Image bLeft = CreateBorderEdge(root.transform, "BorderLeft",
                new Vector2(0, 0), new Vector2(0, 1),
                Vector2.zero, new Vector2(borderPx, 0));
            Image bRight = CreateBorderEdge(root.transform, "BorderRight",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-borderPx, 0), Vector2.zero);

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(root.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.05f, 0.05f);
            labelRT.anchorMax = new Vector2(0.95f, 0.95f);
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = descriptor.Label;
            labelTMP.fontSize = 9;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = ValidationDashboardTheme.AnnunciatorTextDim;
            labelTMP.enableWordWrapping = false;
            labelTMP.overflowMode = TextOverflowModes.Truncate;
            labelTMP.raycastTarget = false;

            // Try to load instrument font
            TMP_FontAsset instrumentFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");
            if (instrumentFont != null)
                labelTMP.font = instrumentFont;

            // Add component and wire references
            DashboardAnnunciatorTile tile = root.AddComponent<DashboardAnnunciatorTile>();
            tile._descriptor = descriptor;
            tile._background = bg;
            tile._borderTop = bTop;
            tile._borderBottom = bBot;
            tile._borderLeft = bLeft;
            tile._borderRight = bRight;
            tile._label = labelTMP;

            return tile;
        }

        /// <summary>
        /// Create a 1px border edge anchored to a tile edge.
        /// </summary>
        private static Image CreateBorderEdge(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject edgeGO = new GameObject(name);
            edgeGO.transform.SetParent(parent, false);

            RectTransform rt = edgeGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image img = edgeGO.AddComponent<Image>();
            img.color = ValidationDashboardTheme.AnnunciatorBorderDim;
            img.raycastTarget = false;

            return img;
        }
    }
}
