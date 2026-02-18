// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// RotarySwitchPOCController.cs — Test harness for 3-position rotary switches
// ============================================================================
//
// PURPOSE:
//   Demonstrates RotarySwitchPOC functionality with multiple switch instances
//   showing different configurations and label options.
//
// SETUP:
//   1. Add UIDocument to a GameObject
//   2. Assign Bootstrap.uxml as the Source Asset
//   3. Add this component to the same GameObject
//   4. Assign POC_PanelSettings to the UIDocument's Panel Settings
//
// VERSION: 2.0 — Enhanced industrial panel styling
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class RotarySwitchPOCController : MonoBehaviour
    {
        // ====================================================================
        // VISUAL CONSTANTS — Industrial Panel Styling
        // ====================================================================
        
        private static readonly Color PANEL_BG = new Color(0.16f, 0.17f, 0.18f, 1f);
        private static readonly Color PANEL_DARK = new Color(0.10f, 0.10f, 0.11f, 1f);
        private static readonly Color ACCENT_GREEN = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color TEXT_DIM = new Color(0.55f, 0.55f, 0.58f, 1f);
        private static readonly Color TEXT_BRIGHT = new Color(0.85f, 0.85f, 0.83f, 1f);
        private static readonly Color NAMEPLATE_BG = new Color(0.08f, 0.08f, 0.09f, 1f);
        private static readonly Color NAMEPLATE_BORDER = new Color(0.25f, 0.25f, 0.27f, 1f);
        
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================
        
        private UIDocument _uiDocument;
        
        private RotarySwitchPOC _switchHeater;
        private RotarySwitchPOC _switchPump;
        private RotarySwitchPOC _switchMode;
        private RotarySwitchPOC _switchValve;
        
        private Label _statusHeater;
        private Label _statusPump;
        private Label _statusMode;
        private Label _statusValve;
        
        // ====================================================================
        // LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BuildUI();
        }
        
        // ====================================================================
        // UI CONSTRUCTION
        // ====================================================================
        
        private void BuildUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            root.style.backgroundColor = PANEL_BG;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;
            root.style.paddingLeft = 30;
            root.style.paddingRight = 30;
            
            root.Add(CreateHeader());
            root.Add(CreateInstructionsBar());
            root.Add(CreateSwitchesPanel());
            root.Add(CreateStatusPanel());
            
            Debug.Log("[RotarySwitchPOC] UI built with 4 industrial rotary switches");
        }
        
        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 45;
            header.style.backgroundColor = PANEL_DARK;
            header.style.paddingLeft = 20;
            header.style.paddingRight = 20;
            header.style.borderBottomWidth = 3;
            header.style.borderBottomColor = ACCENT_GREEN;
            header.style.borderTopLeftRadius = 4;
            header.style.borderTopRightRadius = 4;
            
            var title = new Label("SELECTOR SWITCH MODULE");
            title.style.color = ACCENT_GREEN;
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 2;
            header.Add(title);
            
            var subtitle = new Label("3-POSITION ROTARY — ALLEN-BRADLEY 800T STYLE");
            subtitle.style.color = TEXT_DIM;
            subtitle.style.fontSize = 11;
            subtitle.style.letterSpacing = 1;
            header.Add(subtitle);
            
            return header;
        }
        
        private VisualElement CreateInstructionsBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.justifyContent = Justify.Center;
            bar.style.alignItems = Align.Center;
            bar.style.height = 35;
            bar.style.marginTop = 15;
            bar.style.marginBottom = 20;
            bar.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            bar.style.borderTopLeftRadius = 4;
            bar.style.borderTopRightRadius = 4;
            bar.style.borderBottomLeftRadius = 4;
            bar.style.borderBottomRightRadius = 4;
            bar.style.borderTopWidth = 1;
            bar.style.borderBottomWidth = 1;
            bar.style.borderLeftWidth = 1;
            bar.style.borderRightWidth = 1;
            bar.style.borderTopColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            bar.style.borderBottomColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            bar.style.borderLeftColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            bar.style.borderRightColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            
            var leftClick = new Label("LEFT CLICK");
            leftClick.style.color = new Color(0.5f, 0.7f, 1f, 1f);
            leftClick.style.fontSize = 11;
            leftClick.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.Add(leftClick);
            
            var arrow1 = new Label(" → CW    ");
            arrow1.style.color = TEXT_DIM;
            arrow1.style.fontSize = 11;
            bar.Add(arrow1);
            
            var divider = new Label("│");
            divider.style.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            divider.style.fontSize = 11;
            divider.style.marginLeft = 10;
            divider.style.marginRight = 10;
            bar.Add(divider);
            
            var rightClick = new Label("RIGHT CLICK");
            rightClick.style.color = new Color(1f, 0.7f, 0.5f, 1f);
            rightClick.style.fontSize = 11;
            rightClick.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.Add(rightClick);
            
            var arrow2 = new Label(" → CCW");
            arrow2.style.color = TEXT_DIM;
            arrow2.style.fontSize = 11;
            bar.Add(arrow2);
            
            return bar;
        }
        
        private VisualElement CreateSwitchesPanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            panel.style.justifyContent = Justify.SpaceAround;
            panel.style.alignItems = Align.FlexStart;
            panel.style.backgroundColor = new Color(0.13f, 0.13f, 0.14f, 1f);
            panel.style.paddingTop = 25;
            panel.style.paddingBottom = 30;
            panel.style.paddingLeft = 15;
            panel.style.paddingRight = 15;
            panel.style.borderTopLeftRadius = 6;
            panel.style.borderTopRightRadius = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius = 6;
            panel.style.marginBottom = 20;
            
            // Add texture effect (simulated panel surface)
            panel.style.borderTopWidth = 1;
            panel.style.borderTopColor = new Color(0.20f, 0.20f, 0.22f, 1f);
            panel.style.borderBottomWidth = 2;
            panel.style.borderBottomColor = new Color(0.08f, 0.08f, 0.09f, 1f);
            
            // Switch 1: Heater Control
            _switchHeater = CreateSwitch("HEATER CTL", "OFF", "AUTO", "ON", RotarySwitchPosition.Center);
            _switchHeater.PositionChanged += pos => UpdateStatus(_statusHeater, "HEATER", pos, _switchHeater);
            panel.Add(CreateSwitchAssembly(_switchHeater, "PZR HEATER CONTROL"));
            
            // Switch 2: Pump Select
            _switchPump = CreateSwitch("PUMP SEL", "A", "BOTH", "B", RotarySwitchPosition.Center);
            _switchPump.PositionChanged += pos => UpdateStatus(_statusPump, "PUMP", pos, _switchPump);
            panel.Add(CreateSwitchAssembly(_switchPump, "CHARGING PUMP SELECT"));
            
            // Switch 3: Mode
            _switchMode = CreateSwitch("MODE", "MAN", "AUTO", "RMT", RotarySwitchPosition.Left);
            _switchMode.PositionChanged += pos => UpdateStatus(_statusMode, "MODE", pos, _switchMode);
            panel.Add(CreateSwitchAssembly(_switchMode, "CONTROL MODE"));
            
            // Switch 4: Valve
            _switchValve = CreateSwitch("VALVE", "CLOSE", "STOP", "OPEN", RotarySwitchPosition.Center);
            _switchValve.PositionChanged += pos => UpdateStatus(_statusValve, "VALVE", pos, _switchValve);
            panel.Add(CreateSwitchAssembly(_switchValve, "LTDN ISOL VALVE"));
            
            return panel;
        }
        
        private RotarySwitchPOC CreateSwitch(string label, string left, string center, string right, RotarySwitchPosition initial)
        {
            var sw = new RotarySwitchPOC();
            sw.switchLabel = label;
            sw.labelLeft = left;
            sw.labelCenter = center;
            sw.labelRight = right;
            sw.SetPositionImmediate(initial);
            return sw;
        }
        
        private VisualElement CreateSwitchAssembly(RotarySwitchPOC sw, string description)
        {
            var assembly = new VisualElement();
            assembly.style.alignItems = Align.Center;
            assembly.style.width = 170;
            
            // Switch mounting plate
            var mountingPlate = new VisualElement();
            mountingPlate.style.width = 150;
            mountingPlate.style.backgroundColor = new Color(0.18f, 0.19f, 0.20f, 1f);
            mountingPlate.style.borderTopLeftRadius = 6;
            mountingPlate.style.borderTopRightRadius = 6;
            mountingPlate.style.borderBottomLeftRadius = 6;
            mountingPlate.style.borderBottomRightRadius = 6;
            mountingPlate.style.paddingTop = 12;
            mountingPlate.style.paddingBottom = 15;
            mountingPlate.style.paddingLeft = 10;
            mountingPlate.style.paddingRight = 10;
            mountingPlate.style.alignItems = Align.Center;
            
            // Plate edge effects
            mountingPlate.style.borderTopWidth = 1;
            mountingPlate.style.borderTopColor = new Color(0.28f, 0.29f, 0.30f, 1f);
            mountingPlate.style.borderBottomWidth = 2;
            mountingPlate.style.borderBottomColor = new Color(0.10f, 0.10f, 0.11f, 1f);
            mountingPlate.style.borderLeftWidth = 1;
            mountingPlate.style.borderLeftColor = new Color(0.22f, 0.23f, 0.24f, 1f);
            mountingPlate.style.borderRightWidth = 1;
            mountingPlate.style.borderRightColor = new Color(0.14f, 0.14f, 0.15f, 1f);
            
            // Position labels row (engraved style)
            var labelsRow = CreatePositionLabels(sw);
            mountingPlate.Add(labelsRow);
            
            // The switch itself
            sw.style.width = 110;
            sw.style.height = 110;
            sw.style.marginTop = 5;
            mountingPlate.Add(sw);
            
            // Nameplate below switch
            var nameplate = CreateNameplate(sw.switchLabel);
            mountingPlate.Add(nameplate);
            
            assembly.Add(mountingPlate);
            
            // Description label below mounting plate
            var descLabel = new Label(description);
            descLabel.style.fontSize = 10;
            descLabel.style.color = TEXT_DIM;
            descLabel.style.marginTop = 10;
            descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            descLabel.style.letterSpacing = 1;
            assembly.Add(descLabel);
            
            return assembly;
        }
        
        private VisualElement CreatePositionLabels(RotarySwitchPOC sw)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.width = 120;
            row.style.marginBottom = 3;
            
            var leftLabel = CreateEngravedLabel(sw.labelLeft);
            var centerLabel = CreateEngravedLabel(sw.labelCenter);
            var rightLabel = CreateEngravedLabel(sw.labelRight);
            
            row.Add(leftLabel);
            row.Add(centerLabel);
            row.Add(rightLabel);
            
            return row;
        }
        
        private Label CreateEngravedLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 9;
            label.style.color = new Color(0.65f, 0.65f, 0.63f, 1f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = 0.5f;
            // Simulate engraved text with subtle shadow
            label.style.textShadow = new TextShadow
            {
                offset = new Vector2(0, 1),
                blurRadius = 0,
                color = new Color(0, 0, 0, 0.5f)
            };
            return label;
        }
        
        private VisualElement CreateNameplate(string text)
        {
            var plate = new VisualElement();
            plate.style.marginTop = 8;
            plate.style.paddingTop = 4;
            plate.style.paddingBottom = 4;
            plate.style.paddingLeft = 12;
            plate.style.paddingRight = 12;
            plate.style.backgroundColor = NAMEPLATE_BG;
            plate.style.borderTopLeftRadius = 2;
            plate.style.borderTopRightRadius = 2;
            plate.style.borderBottomLeftRadius = 2;
            plate.style.borderBottomRightRadius = 2;
            plate.style.borderTopWidth = 1;
            plate.style.borderBottomWidth = 1;
            plate.style.borderLeftWidth = 1;
            plate.style.borderRightWidth = 1;
            plate.style.borderTopColor = NAMEPLATE_BORDER;
            plate.style.borderBottomColor = new Color(0.05f, 0.05f, 0.06f, 1f);
            plate.style.borderLeftColor = NAMEPLATE_BORDER;
            plate.style.borderRightColor = NAMEPLATE_BORDER;
            
            var label = new Label(text);
            label.style.fontSize = 10;
            label.style.color = TEXT_BRIGHT;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            plate.Add(label);
            
            return plate;
        }
        
        private VisualElement CreateStatusPanel()
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = PANEL_DARK;
            panel.style.paddingTop = 15;
            panel.style.paddingBottom = 15;
            panel.style.paddingLeft = 20;
            panel.style.paddingRight = 20;
            panel.style.borderTopLeftRadius = 6;
            panel.style.borderTopRightRadius = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius = 6;
            
            var title = new Label("SWITCH STATUS");
            title.style.color = ACCENT_GREEN;
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 2;
            title.style.marginBottom = 12;
            panel.Add(title);
            
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.justifyContent = Justify.SpaceAround;
            panel.Add(grid);
            
            _statusHeater = CreateStatusDisplay();
            _statusPump = CreateStatusDisplay();
            _statusMode = CreateStatusDisplay();
            _statusValve = CreateStatusDisplay();
            
            grid.Add(_statusHeater);
            grid.Add(_statusPump);
            grid.Add(_statusMode);
            grid.Add(_statusValve);
            
            // Initialize
            UpdateStatus(_statusHeater, "HEATER", _switchHeater.position, _switchHeater);
            UpdateStatus(_statusPump, "PUMP", _switchPump.position, _switchPump);
            UpdateStatus(_statusMode, "MODE", _switchMode.position, _switchMode);
            UpdateStatus(_statusValve, "VALVE", _switchValve.position, _switchValve);
            
            return panel;
        }
        
        private Label CreateStatusDisplay()
        {
            var label = new Label("---");
            label.style.fontSize = 12;
            label.style.color = TEXT_DIM;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.width = 140;
            label.style.paddingTop = 8;
            label.style.paddingBottom = 8;
            label.style.backgroundColor = new Color(0.06f, 0.06f, 0.07f, 1f);
            label.style.borderTopLeftRadius = 3;
            label.style.borderTopRightRadius = 3;
            label.style.borderBottomLeftRadius = 3;
            label.style.borderBottomRightRadius = 3;
            label.style.borderTopWidth = 1;
            label.style.borderTopColor = new Color(0.15f, 0.15f, 0.17f, 1f);
            return label;
        }
        
        private void UpdateStatus(Label statusLabel, string name, RotarySwitchPosition pos, RotarySwitchPOC sw)
        {
            string posName = pos switch
            {
                RotarySwitchPosition.Left => sw.labelLeft,
                RotarySwitchPosition.Center => sw.labelCenter,
                RotarySwitchPosition.Right => sw.labelRight,
                _ => "???"
            };
            
            statusLabel.text = $"{name}: {posName}";
            
            statusLabel.style.color = pos switch
            {
                RotarySwitchPosition.Left => new Color(0.4f, 0.75f, 1f, 1f),
                RotarySwitchPosition.Center => ACCENT_GREEN,
                RotarySwitchPosition.Right => new Color(1f, 0.75f, 0.35f, 1f),
                _ => TEXT_DIM
            };
        }
    }
}
