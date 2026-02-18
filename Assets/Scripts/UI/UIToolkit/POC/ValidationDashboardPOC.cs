// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// ValidationDashboardPOC.cs — Complete Validation Dashboard Rebuild
// ============================================================================
//
// PURPOSE:
//   Comprehensive validation dashboard using UI Toolkit with all POC controls.
//   Addresses gaps identified in the UI review report:
//   - Bubble Formation State Machine visualization
//   - SG Multi-Node model display
//   - Steam Dump / HZP Systems
//   - Mass Conservation breakdown
//   - Energy Balance telemetry
//   - Startup Permissives chain
//
// TABS:
//   0. Overview    - Key parameters at a glance
//   1. Pressurizer - PZR vessel, bubble formation, heater/spray
//   2. RCS/Thermal - Loop temps, RHR, heat balance
//   3. CVCS        - Flows, VCT, mass conservation
//   4. Steam Gen   - SG multi-node, secondary pressure, draining
//   5. HZP/Startup - Steam dump, condenser, permissives
//   6. Alarms      - Full annunciator panel
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Critical.Validation;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class ValidationDashboardPOC : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR
        // ====================================================================
        
        [Header("Data Source")]
        [SerializeField] private bool useSimulatedData = true;
        
        [Header("Update Settings")]
        [SerializeField] private float updateRate = 5f;
        
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================
        
        private UIDocument _uiDocument;
        private HeatupSimEngine _engine;
        private float _nextUpdate;
        private float _simTime;
        private int _currentTab = 0;
        
        // Tab content containers
        private VisualElement[] _tabPanels;
        private Button[] _tabButtons;
        
        // Header elements
        private Label _modeLabel;
        private Label _phaseLabel;
        private Label _simTimeLabel;
        private Label _speedLabel;
        
        // Overview tab controls
        private ArcGaugePOC _ovPressureGauge;
        private ArcGaugePOC _ovTempGauge;
        private ArcGaugePOC _ovLevelGauge;
        private ArcGaugePOC _ovSubcoolGauge;
        private LinearGaugePOC _ovHeaterBar;
        private LinearGaugePOC _ovChargingBar;
        private LinearGaugePOC _ovLetdownBar;
        private Label _ovPressureValue;
        private Label _ovTempValue;
        private Label _ovLevelValue;
        private Label _ovSubcoolValue;
        private Label _ovBubblePhase;
        private Label _ovRcpStatus;
        private Label _ovRhrStatus;
        private StatusLEDPOC[] _ovRcpLEDs = new StatusLEDPOC[4];
        private StripChartPOC _ovTrendChart;
        
        // Pressurizer tab controls
        private PressurizerVesselPOC _pzrVessel;
        private Label _pzrPressure;
        private Label _pzrLevel;
        private Label _pzrTsat;
        private Label _pzrHeaterPower;
        private Label _pzrHeaterMode;
        private Label _pzrSprayFlow;
        private Label _pzrBubblePhase;
        private Label _pzrBubbleDuration;
        private StripChartPOC _pzrTrendChart;
        private VisualElement _bubbleStateIndicator;
        private Label[] _bubblePhaseLabels = new Label[7];
        
        // RCS tab controls
        private ArcGaugePOC _rcsThot;
        private ArcGaugePOC _rcsTcold;
        private ArcGaugePOC _rcsTavg;
        private BidirectionalGaugePOC _rcsHeatBalance;
        private Label _rcsRhrMode;
        private Label _rcsRhrHeat;
        private Label _rcsSgHeat;
        private Label _rcsNetHeat;
        private StripChartPOC _rcsTempChart;
        
        // CVCS tab controls
        private TankLevelPOC _cvcsVctTank;
        private LinearGaugePOC _cvcsChargingBar;
        private LinearGaugePOC _cvcsLetdownBar;
        private Label _cvcsMassLedger;
        private Label _cvcsMassDrift;
        private Label _cvcsMassStatus;
        private StatusLEDPOC _cvcsMassLED;
        private Label _cvcsOrificeLineup;
        
        // SG tab controls
        private TankLevelPOC _sgLevelTank;
        private ArcGaugePOC _sgSecondaryPressure;
        private Label _sgBoundaryState;
        private Label _sgThermoclineHeight;
        private Label _sgBoilingStatus;
        private Label _sgDrainingStatus;
        private StripChartPOC _sgPressureChart;
        
        // HZP tab controls
        private ArcGaugePOC _hzpCondenserVacuum;
        private LinearGaugePOC _hzpSteamDumpBar;
        private Label _hzpBridgeState;
        private Label _hzpP12Status;
        private Label _hzpC9Status;
        private Label _hzpPermissiveStatus;
        private StatusLEDPOC _hzpC9LED;
        private StatusLEDPOC _hzpP12LED;
        private StatusLEDPOC _hzpPermitLED;
        
        // Alarms tab controls
        private AnnunciatorTilePOC[] _alarmTiles;
        
        // Mini trends (always visible)
        private StripChartPOC _miniTrendChart;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_ACCENT = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color COLOR_TEXT = new Color(0.8f, 0.8f, 0.85f, 1f);
        private static readonly Color COLOR_DIM = new Color(0.5f, 0.5f, 0.6f, 1f);
        private static readonly Color COLOR_BG = new Color(0.04f, 0.04f, 0.06f, 1f);
        private static readonly Color COLOR_PANEL = new Color(0.07f, 0.07f, 0.1f, 1f);
        private static readonly Color COLOR_TAB_ACTIVE = new Color(0.1f, 0.4f, 0.3f, 1f);
        private static readonly Color COLOR_TAB_INACTIVE = new Color(0.08f, 0.08f, 0.12f, 1f);
        
        // ====================================================================
        // TAB NAMES
        // ====================================================================
        
        private static readonly string[] TAB_NAMES = {
            "OVERVIEW", "PRESSURIZER", "RCS/THERMAL", "CVCS", "STEAM GEN", "HZP/STARTUP", "ALARMS"
        };
        
        // ====================================================================
        // LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            
            if (!useSimulatedData)
            {
                _engine = FindObjectOfType<HeatupSimEngine>();
                if (_engine == null)
                {
                    Debug.LogWarning("[ValidationDashboardPOC] HeatupSimEngine not found, using simulated data");
                    useSimulatedData = true;
                }
            }
            
            BuildUI();
        }
        
        private void Update()
        {
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + 1f / updateRate;
            
            if (useSimulatedData)
            {
                UpdateSimulatedData();
            }
            else
            {
                UpdateFromEngine();
            }
        }
        
        // ====================================================================
        // UI CONSTRUCTION
        // ====================================================================
        
        private void BuildUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            root.style.backgroundColor = COLOR_BG;
            root.style.flexDirection = FlexDirection.Column;
            
            // Header bar
            root.Add(BuildHeader());
            
            // Tab bar
            root.Add(BuildTabBar());
            
            // Main content area (tabs + mini trends)
            var mainArea = new VisualElement();
            mainArea.style.flexDirection = FlexDirection.Row;
            mainArea.style.flexGrow = 1;
            mainArea.style.paddingLeft = 10;
            mainArea.style.paddingRight = 10;
            mainArea.style.paddingBottom = 10;
            root.Add(mainArea);
            
            // Tab content container
            var tabContainer = new VisualElement();
            tabContainer.style.flexGrow = 1;
            tabContainer.style.marginRight = 10;
            mainArea.Add(tabContainer);
            
            // Build all tab panels
            _tabPanels = new VisualElement[TAB_NAMES.Length];
            _tabPanels[0] = BuildOverviewTab();
            _tabPanels[1] = BuildPressurizerTab();
            _tabPanels[2] = BuildRCSTab();
            _tabPanels[3] = BuildCVCSTab();
            _tabPanels[4] = BuildSGTab();
            _tabPanels[5] = BuildHZPTab();
            _tabPanels[6] = BuildAlarmsTab();
            
            foreach (var panel in _tabPanels)
            {
                panel.style.flexGrow = 1;
                panel.style.display = DisplayStyle.None;
                tabContainer.Add(panel);
            }
            
            // Show first tab
            _tabPanels[0].style.display = DisplayStyle.Flex;
            
            // Mini trends panel (always visible)
            mainArea.Add(BuildMiniTrends());
        }
        
        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 40;
            header.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            header.style.paddingLeft = 15;
            header.style.paddingRight = 15;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = COLOR_ACCENT;
            
            // Title
            var title = new Label("HEATUP VALIDATION DASHBOARD");
            title.style.color = COLOR_ACCENT;
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            
            // Mode
            _modeLabel = new Label("MODE 5 - COLD SHUTDOWN");
            _modeLabel.style.color = new Color(0.3f, 0.8f, 1f);
            _modeLabel.style.fontSize = 12;
            header.Add(_modeLabel);
            
            // Phase
            _phaseLabel = new Label("SOLID PZR - HEATING");
            _phaseLabel.style.color = COLOR_TEXT;
            _phaseLabel.style.fontSize = 12;
            header.Add(_phaseLabel);
            
            // Time
            _simTimeLabel = new Label("T+ 00:00:00");
            _simTimeLabel.style.color = COLOR_ACCENT;
            _simTimeLabel.style.fontSize = 14;
            header.Add(_simTimeLabel);
            
            // Speed
            _speedLabel = new Label("1x");
            _speedLabel.style.color = COLOR_DIM;
            _speedLabel.style.fontSize = 12;
            header.Add(_speedLabel);
            
            return header;
        }
        
        private VisualElement BuildTabBar()
        {
            var tabBar = new VisualElement();
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.height = 32;
            tabBar.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f);
            tabBar.style.paddingLeft = 10;
            tabBar.style.paddingTop = 4;
            
            _tabButtons = new Button[TAB_NAMES.Length];
            
            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                int tabIndex = i;
                var btn = new Button(() => SelectTab(tabIndex));
                btn.text = TAB_NAMES[i];
                btn.style.backgroundColor = i == 0 ? COLOR_TAB_ACTIVE : COLOR_TAB_INACTIVE;
                btn.style.color = i == 0 ? COLOR_ACCENT : COLOR_DIM;
                btn.style.borderTopWidth = 0;
                btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0;
                btn.style.borderRightWidth = 0;
                btn.style.marginRight = 5;
                btn.style.paddingLeft = 12;
                btn.style.paddingRight = 12;
                btn.style.fontSize = 11;
                btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                btn.style.borderTopLeftRadius = 4;
                btn.style.borderTopRightRadius = 4;
                btn.style.borderBottomLeftRadius = 0;
                btn.style.borderBottomRightRadius = 0;
                tabBar.Add(btn);
                _tabButtons[i] = btn;
            }
            
            return tabBar;
        }
        
        private void SelectTab(int index)
        {
            for (int i = 0; i < _tabPanels.Length; i++)
            {
                _tabPanels[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
                _tabButtons[i].style.backgroundColor = i == index ? COLOR_TAB_ACTIVE : COLOR_TAB_INACTIVE;
                _tabButtons[i].style.color = i == index ? COLOR_ACCENT : COLOR_DIM;
            }
            _currentTab = index;
        }
        
        // ====================================================================
        // TAB BUILDERS
        // ====================================================================
        
        private VisualElement BuildOverviewTab()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            
            // Left column: Core parameters
            var leftCol = CreatePanel("CORE PARAMETERS");
            leftCol.style.width = Length.Percent(30);
            leftCol.style.marginRight = 10;
            
            // Gauges grid (2x2)
            var gaugeGrid = new VisualElement();
            gaugeGrid.style.flexDirection = FlexDirection.Row;
            gaugeGrid.style.flexWrap = Wrap.Wrap;
            gaugeGrid.style.marginTop = 10;
            
            gaugeGrid.Add(CreateGaugeCell("PRESSURE", 0, 2500, "psia", out _ovPressureGauge, out _ovPressureValue));
            gaugeGrid.Add(CreateGaugeCell("T_AVG", 50, 620, "°F", out _ovTempGauge, out _ovTempValue));
            gaugeGrid.Add(CreateGaugeCell("PZR LEVEL", 0, 100, "%", out _ovLevelGauge, out _ovLevelValue));
            gaugeGrid.Add(CreateGaugeCell("SUBCOOL", 0, 100, "°F", out _ovSubcoolGauge, out _ovSubcoolValue));
            
            leftCol.Add(gaugeGrid);
            panel.Add(leftCol);
            
            // Center column: Status
            var centerCol = CreatePanel("SYSTEM STATUS");
            centerCol.style.flexGrow = 1;
            centerCol.style.marginRight = 10;
            
            // Bubble phase
            var bubbleRow = new VisualElement();
            bubbleRow.style.flexDirection = FlexDirection.Row;
            bubbleRow.style.justifyContent = Justify.SpaceBetween;
            bubbleRow.style.marginTop = 10;
            
            var bubbleLbl = new Label("BUBBLE PHASE:");
            bubbleLbl.style.color = COLOR_DIM;
            bubbleLbl.style.fontSize = 11;
            bubbleRow.Add(bubbleLbl);
            
            _ovBubblePhase = new Label("NONE");
            _ovBubblePhase.style.color = COLOR_ACCENT;
            _ovBubblePhase.style.fontSize = 11;
            _ovBubblePhase.style.unityFontStyleAndWeight = FontStyle.Bold;
            bubbleRow.Add(_ovBubblePhase);
            
            centerCol.Add(bubbleRow);
            
            // RCP Status
            var rcpRow = new VisualElement();
            rcpRow.style.marginTop = 15;
            
            _ovRcpStatus = new Label("RCPs: 0/4");
            _ovRcpStatus.style.color = COLOR_TEXT;
            _ovRcpStatus.style.fontSize = 12;
            rcpRow.Add(_ovRcpStatus);
            
            var rcpLedRow = new VisualElement();
            rcpLedRow.style.flexDirection = FlexDirection.Row;
            rcpLedRow.style.marginTop = 5;
            
            for (int i = 0; i < 4; i++)
            {
                var ledHolder = new VisualElement();
                ledHolder.style.alignItems = Align.Center;
                ledHolder.style.marginRight = 10;
                
                _ovRcpLEDs[i] = new StatusLEDPOC();
                _ovRcpLEDs[i].style.width = 18;
                _ovRcpLEDs[i].style.height = 18;
                ledHolder.Add(_ovRcpLEDs[i]);
                
                var lbl = new Label($"RCP{i + 1}");
                lbl.style.fontSize = 9;
                lbl.style.color = COLOR_DIM;
                ledHolder.Add(lbl);
                
                rcpLedRow.Add(ledHolder);
            }
            rcpRow.Add(rcpLedRow);
            centerCol.Add(rcpRow);
            
            // RHR Status
            var rhrRow = new VisualElement();
            rhrRow.style.marginTop = 15;
            
            _ovRhrStatus = new Label("RHR: HEATUP MODE");
            _ovRhrStatus.style.color = COLOR_TEXT;
            _ovRhrStatus.style.fontSize = 12;
            rhrRow.Add(_ovRhrStatus);
            
            centerCol.Add(rhrRow);
            
            // CVCS bars
            var cvcsSection = new VisualElement();
            cvcsSection.style.marginTop = 20;
            
            var cvcsTitle = new Label("CVCS FLOWS");
            cvcsTitle.style.color = COLOR_ACCENT;
            cvcsTitle.style.fontSize = 10;
            cvcsTitle.style.marginBottom = 5;
            cvcsSection.Add(cvcsTitle);
            
            cvcsSection.Add(CreateFlowBar("HEATER", out _ovHeaterBar, 0, 100));
            cvcsSection.Add(CreateFlowBar("CHARGING", out _ovChargingBar, 0, 100));
            cvcsSection.Add(CreateFlowBar("LETDOWN", out _ovLetdownBar, 0, 120));
            
            centerCol.Add(cvcsSection);
            panel.Add(centerCol);
            
            // Right column: Trend
            var rightCol = CreatePanel("4-HOUR TREND");
            rightCol.style.width = Length.Percent(35);
            
            var chartHolder = new VisualElement();
            chartHolder.style.flexGrow = 1;
            chartHolder.style.marginTop = 10;
            chartHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            chartHolder.style.borderTopLeftRadius = 4;
            chartHolder.style.borderTopRightRadius = 4;
            chartHolder.style.borderBottomLeftRadius = 4;
            chartHolder.style.borderBottomRightRadius = 4;
            
            _ovTrendChart = new StripChartPOC();
            _ovTrendChart.style.flexGrow = 1;
            _ovTrendChart.AddTrace("Pressure", new Color(1f, 0.667f, 0f), 0, 100);
            _ovTrendChart.AddTrace("T_avg", COLOR_ACCENT, 0, 100);
            _ovTrendChart.AddTrace("Level", new Color(0.4f, 0.7f, 1f), 0, 100);
            chartHolder.Add(_ovTrendChart);
            rightCol.Add(chartHolder);
            
            panel.Add(rightCol);
            
            return panel;
        }
        
        private VisualElement BuildPressurizerTab()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            
            // Left: Vessel
            var leftCol = CreatePanel("PRESSURIZER VESSEL");
            leftCol.style.width = 220;
            leftCol.style.marginRight = 10;
            
            var vesselHolder = new VisualElement();
            vesselHolder.style.flexGrow = 1;
            vesselHolder.style.marginTop = 10;
            vesselHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            vesselHolder.style.borderTopLeftRadius = 4;
            vesselHolder.style.borderTopRightRadius = 4;
            vesselHolder.style.borderBottomLeftRadius = 4;
            vesselHolder.style.borderBottomRightRadius = 4;
            
            _pzrVessel = new PressurizerVesselPOC();
            _pzrVessel.style.flexGrow = 1;
            vesselHolder.Add(_pzrVessel);
            leftCol.Add(vesselHolder);
            panel.Add(leftCol);
            
            // Center: Parameters
            var centerCol = CreatePanel("PZR PARAMETERS");
            centerCol.style.width = 200;
            centerCol.style.marginRight = 10;
            
            var paramGrid = new VisualElement();
            paramGrid.style.marginTop = 10;
            
            paramGrid.Add(CreateParamRow("PRESSURE:", out _pzrPressure));
            paramGrid.Add(CreateParamRow("LEVEL:", out _pzrLevel));
            paramGrid.Add(CreateParamRow("T_SAT:", out _pzrTsat));
            paramGrid.Add(CreateParamRow("HEATER:", out _pzrHeaterPower));
            paramGrid.Add(CreateParamRow("HTR MODE:", out _pzrHeaterMode));
            paramGrid.Add(CreateParamRow("SPRAY:", out _pzrSprayFlow));
            
            centerCol.Add(paramGrid);
            
            // Bubble formation section
            var bubbleSection = CreatePanel("BUBBLE FORMATION");
            bubbleSection.style.marginTop = 15;
            
            bubbleSection.Add(CreateParamRow("PHASE:", out _pzrBubblePhase));
            bubbleSection.Add(CreateParamRow("DURATION:", out _pzrBubbleDuration));
            
            // Phase state machine visualization
            var phaseRow = new VisualElement();
            phaseRow.style.flexDirection = FlexDirection.Row;
            phaseRow.style.flexWrap = Wrap.Wrap;
            phaseRow.style.marginTop = 10;
            
            string[] phases = { "NONE", "DETECT", "VERIFY", "DRAIN", "STABIL", "PRESS", "DONE" };
            for (int i = 0; i < 7; i++)
            {
                var phaseLbl = new Label(phases[i]);
                phaseLbl.style.fontSize = 8;
                phaseLbl.style.color = COLOR_DIM;
                phaseLbl.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
                phaseLbl.style.paddingLeft = 4;
                phaseLbl.style.paddingRight = 4;
                phaseLbl.style.paddingTop = 2;
                phaseLbl.style.paddingBottom = 2;
                phaseLbl.style.marginRight = 3;
                phaseLbl.style.marginBottom = 3;
                phaseLbl.style.borderTopLeftRadius = 2;
                phaseLbl.style.borderTopRightRadius = 2;
                phaseLbl.style.borderBottomLeftRadius = 2;
                phaseLbl.style.borderBottomRightRadius = 2;
                _bubblePhaseLabels[i] = phaseLbl;
                phaseRow.Add(phaseLbl);
            }
            bubbleSection.Add(phaseRow);
            
            centerCol.Add(bubbleSection);
            panel.Add(centerCol);
            
            // Right: Trends
            var rightCol = CreatePanel("PZR TRENDS");
            rightCol.style.flexGrow = 1;
            
            var pzrChartHolder = new VisualElement();
            pzrChartHolder.style.flexGrow = 1;
            pzrChartHolder.style.marginTop = 10;
            pzrChartHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            
            _pzrTrendChart = new StripChartPOC();
            _pzrTrendChart.style.flexGrow = 1;
            _pzrTrendChart.AddTrace("Pressure", new Color(1f, 0.667f, 0f), 0, 100);
            _pzrTrendChart.AddTrace("Level", COLOR_ACCENT, 0, 100);
            _pzrTrendChart.AddTrace("Heater", new Color(1f, 0.4f, 0.2f), 0, 100);
            pzrChartHolder.Add(_pzrTrendChart);
            rightCol.Add(pzrChartHolder);
            
            panel.Add(rightCol);
            
            return panel;
        }
        
        private VisualElement BuildRCSTab()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            
            // Left: Temperature gauges
            var leftCol = CreatePanel("LOOP TEMPERATURES");
            leftCol.style.width = Length.Percent(40);
            leftCol.style.marginRight = 10;
            
            var tempGauges = new VisualElement();
            tempGauges.style.flexDirection = FlexDirection.Row;
            tempGauges.style.justifyContent = Justify.SpaceAround;
            tempGauges.style.marginTop = 10;
            
            Label unusedLabel;
            tempGauges.Add(CreateGaugeCell("T_HOT", 50, 650, "°F", out _rcsThot, out unusedLabel));
            tempGauges.Add(CreateGaugeCell("T_AVG", 50, 650, "°F", out _rcsTavg, out unusedLabel));
            tempGauges.Add(CreateGaugeCell("T_COLD", 50, 650, "°F", out _rcsTcold, out unusedLabel));
            
            leftCol.Add(tempGauges);
            
            // Heat balance
            var heatSection = CreatePanel("HEAT BALANCE");
            heatSection.style.marginTop = 15;
            
            var heatGaugeHolder = new VisualElement();
            heatGaugeHolder.style.height = 40;
            heatGaugeHolder.style.marginTop = 10;
            
            _rcsHeatBalance = new BidirectionalGaugePOC();
            _rcsHeatBalance.style.flexGrow = 1;
            _rcsHeatBalance.minValue = -10;
            _rcsHeatBalance.maxValue = 10;
            heatGaugeHolder.Add(_rcsHeatBalance);
            heatSection.Add(heatGaugeHolder);
            
            heatSection.Add(CreateParamRow("RHR MODE:", out _rcsRhrMode));
            heatSection.Add(CreateParamRow("RHR HEAT:", out _rcsRhrHeat));
            heatSection.Add(CreateParamRow("SG HEAT:", out _rcsSgHeat));
            heatSection.Add(CreateParamRow("NET HEAT:", out _rcsNetHeat));
            
            leftCol.Add(heatSection);
            panel.Add(leftCol);
            
            // Right: Temp trend
            var rightCol = CreatePanel("TEMPERATURE TREND");
            rightCol.style.flexGrow = 1;
            
            var rcsChartHolder = new VisualElement();
            rcsChartHolder.style.flexGrow = 1;
            rcsChartHolder.style.marginTop = 10;
            rcsChartHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            
            _rcsTempChart = new StripChartPOC();
            _rcsTempChart.style.flexGrow = 1;
            _rcsTempChart.AddTrace("T_hot", new Color(1f, 0.4f, 0.2f), 50, 650);
            _rcsTempChart.AddTrace("T_avg", COLOR_ACCENT, 50, 650);
            _rcsTempChart.AddTrace("T_cold", new Color(0.3f, 0.6f, 1f), 50, 650);
            rcsChartHolder.Add(_rcsTempChart);
            rightCol.Add(rcsChartHolder);
            
            panel.Add(rightCol);
            
            return panel;
        }
        
        private VisualElement BuildCVCSTab()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            
            // Left: VCT Tank
            var leftCol = CreatePanel("VCT INVENTORY");
            leftCol.style.width = 180;
            leftCol.style.marginRight = 10;
            
            var vctHolder = new VisualElement();
            vctHolder.style.height = 200;
            vctHolder.style.marginTop = 10;
            
            _cvcsVctTank = new TankLevelPOC();
            _cvcsVctTank.style.flexGrow = 1;
            vctHolder.Add(_cvcsVctTank);
            leftCol.Add(vctHolder);
            
            leftCol.Add(CreateParamRow("ORIFICE:", out _cvcsOrificeLineup));
            
            panel.Add(leftCol);
            
            // Center: Flows
            var centerCol = CreatePanel("CVCS FLOWS");
            centerCol.style.width = 200;
            centerCol.style.marginRight = 10;
            
            var flowSection = new VisualElement();
            flowSection.style.marginTop = 10;
            
            flowSection.Add(CreateFlowBar("CHARGING", out _cvcsChargingBar, 0, 100));
            flowSection.Add(CreateFlowBar("LETDOWN", out _cvcsLetdownBar, 0, 120));
            
            centerCol.Add(flowSection);
            
            // Mass conservation
            var massSection = CreatePanel("MASS CONSERVATION");
            massSection.style.marginTop = 15;
            
            var massRow = new VisualElement();
            massRow.style.flexDirection = FlexDirection.Row;
            massRow.style.alignItems = Align.Center;
            massRow.style.marginTop = 5;
            
            _cvcsMassLED = new StatusLEDPOC();
            _cvcsMassLED.style.width = 20;
            _cvcsMassLED.style.height = 20;
            _cvcsMassLED.style.marginRight = 10;
            massRow.Add(_cvcsMassLED);
            
            _cvcsMassStatus = new Label("OK");
            _cvcsMassStatus.style.color = COLOR_ACCENT;
            _cvcsMassStatus.style.fontSize = 14;
            _cvcsMassStatus.style.unityFontStyleAndWeight = FontStyle.Bold;
            massRow.Add(_cvcsMassStatus);
            
            massSection.Add(massRow);
            massSection.Add(CreateParamRow("LEDGER:", out _cvcsMassLedger));
            massSection.Add(CreateParamRow("DRIFT:", out _cvcsMassDrift));
            
            centerCol.Add(massSection);
            panel.Add(centerCol);
            
            // Right: Placeholder for detailed breakdown
            var rightCol = CreatePanel("MASS BREAKDOWN");
            rightCol.style.flexGrow = 1;
            
            var placeholder = new Label("Component mass breakdown\nwill be displayed here");
            placeholder.style.color = COLOR_DIM;
            placeholder.style.fontSize = 12;
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.flexGrow = 1;
            rightCol.Add(placeholder);
            
            panel.Add(rightCol);
            
            return panel;
        }
        
        private VisualElement BuildSGTab()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            
            // Left: SG Level
            var leftCol = CreatePanel("SG SECONDARY");
            leftCol.style.width = 180;
            leftCol.style.marginRight = 10;
            
            var sgLevelHolder = new VisualElement();
            sgLevelHolder.style.height = 180;
            sgLevelHolder.style.marginTop = 10;
            
            _sgLevelTank = new TankLevelPOC();
            _sgLevelTank.style.flexGrow = 1;
            sgLevelHolder.Add(_sgLevelTank);
            leftCol.Add(sgLevelHolder);
            
            leftCol.Add(CreateParamRow("DRAINING:", out _sgDrainingStatus));
            
            panel.Add(leftCol);
            
            // Center: SG Parameters
            var centerCol = CreatePanel("SG THERMAL STATE");
            centerCol.style.width = 220;
            centerCol.style.marginRight = 10;
            
            var sgPressHolder = new VisualElement();
            sgPressHolder.style.height = 100;
            sgPressHolder.style.marginTop = 10;
            sgPressHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            
            _sgSecondaryPressure = new ArcGaugePOC();
            _sgSecondaryPressure.style.flexGrow = 1;
            _sgSecondaryPressure.minValue = 0;
            _sgSecondaryPressure.maxValue = 1200;
            sgPressHolder.Add(_sgSecondaryPressure);
            centerCol.Add(sgPressHolder);
            
            var pressLabel = new Label("SECONDARY PRESSURE (psia)");
            pressLabel.style.fontSize = 9;
            pressLabel.style.color = COLOR_DIM;
            pressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerCol.Add(pressLabel);
            
            centerCol.Add(CreateParamRow("BOUNDARY:", out _sgBoundaryState));
            centerCol.Add(CreateParamRow("THERMOCLINE:", out _sgThermoclineHeight));
            centerCol.Add(CreateParamRow("BOILING:", out _sgBoilingStatus));
            
            panel.Add(centerCol);
            
            // Right: SG Pressure trend
            var rightCol = CreatePanel("SG PRESSURE TREND");
            rightCol.style.flexGrow = 1;
            
            var sgChartHolder = new VisualElement();
            sgChartHolder.style.flexGrow = 1;
            sgChartHolder.style.marginTop = 10;
            sgChartHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            
            _sgPressureChart = new StripChartPOC();
            _sgPressureChart.style.flexGrow = 1;
            _sgPressureChart.AddTrace("SG Press", new Color(0.8f, 0.5f, 0.2f), 0, 100);
            sgChartHolder.Add(_sgPressureChart);
            rightCol.Add(sgChartHolder);
            
            panel.Add(rightCol);
            
            return panel;
        }
        
        private VisualElement BuildHZPTab()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            
            // Left: Condenser
            var leftCol = CreatePanel("CONDENSER");
            leftCol.style.width = 200;
            leftCol.style.marginRight = 10;
            
            var vacuumHolder = new VisualElement();
            vacuumHolder.style.height = 100;
            vacuumHolder.style.marginTop = 10;
            vacuumHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            
            _hzpCondenserVacuum = new ArcGaugePOC();
            _hzpCondenserVacuum.style.flexGrow = 1;
            _hzpCondenserVacuum.minValue = 0;
            _hzpCondenserVacuum.maxValue = 30;
            vacuumHolder.Add(_hzpCondenserVacuum);
            leftCol.Add(vacuumHolder);
            
            var vacLabel = new Label("VACUUM (in. Hg)");
            vacLabel.style.fontSize = 9;
            vacLabel.style.color = COLOR_DIM;
            vacLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            leftCol.Add(vacLabel);
            
            // C-9 interlock
            var c9Row = new VisualElement();
            c9Row.style.flexDirection = FlexDirection.Row;
            c9Row.style.alignItems = Align.Center;
            c9Row.style.marginTop = 15;
            
            _hzpC9LED = new StatusLEDPOC();
            _hzpC9LED.style.width = 18;
            _hzpC9LED.style.height = 18;
            _hzpC9LED.style.marginRight = 8;
            c9Row.Add(_hzpC9LED);
            
            _hzpC9Status = new Label("C-9: NOT SATISFIED");
            _hzpC9Status.style.color = COLOR_TEXT;
            _hzpC9Status.style.fontSize = 11;
            c9Row.Add(_hzpC9Status);
            
            leftCol.Add(c9Row);
            panel.Add(leftCol);
            
            // Center: Steam dump
            var centerCol = CreatePanel("STEAM DUMP");
            centerCol.style.width = 200;
            centerCol.style.marginRight = 10;
            
            var dumpBarHolder = new VisualElement();
            dumpBarHolder.style.marginTop = 10;
            
            var dumpLabel = new Label("DUMP VALVE");
            dumpLabel.style.fontSize = 10;
            dumpLabel.style.color = COLOR_DIM;
            dumpBarHolder.Add(dumpLabel);
            
            _hzpSteamDumpBar = new LinearGaugePOC();
            _hzpSteamDumpBar.style.height = 20;
            _hzpSteamDumpBar.style.marginTop = 5;
            _hzpSteamDumpBar.minValue = 0;
            _hzpSteamDumpBar.maxValue = 100;
            dumpBarHolder.Add(_hzpSteamDumpBar);
            
            centerCol.Add(dumpBarHolder);
            
            centerCol.Add(CreateParamRow("BRIDGE:", out _hzpBridgeState));
            
            // P-12 status
            var p12Row = new VisualElement();
            p12Row.style.flexDirection = FlexDirection.Row;
            p12Row.style.alignItems = Align.Center;
            p12Row.style.marginTop = 10;
            
            _hzpP12LED = new StatusLEDPOC();
            _hzpP12LED.style.width = 18;
            _hzpP12LED.style.height = 18;
            _hzpP12LED.style.marginRight = 8;
            p12Row.Add(_hzpP12LED);
            
            _hzpP12Status = new Label("P-12: BLOCKED");
            _hzpP12Status.style.color = COLOR_TEXT;
            _hzpP12Status.style.fontSize = 11;
            p12Row.Add(_hzpP12Status);
            
            centerCol.Add(p12Row);
            
            // Final permissive
            var permitRow = new VisualElement();
            permitRow.style.flexDirection = FlexDirection.Row;
            permitRow.style.alignItems = Align.Center;
            permitRow.style.marginTop = 15;
            permitRow.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            permitRow.style.paddingTop = 8;
            permitRow.style.paddingBottom = 8;
            permitRow.style.paddingLeft = 8;
            permitRow.style.borderTopLeftRadius = 4;
            permitRow.style.borderTopRightRadius = 4;
            permitRow.style.borderBottomLeftRadius = 4;
            permitRow.style.borderBottomRightRadius = 4;
            
            _hzpPermitLED = new StatusLEDPOC();
            _hzpPermitLED.style.width = 22;
            _hzpPermitLED.style.height = 22;
            _hzpPermitLED.style.marginRight = 10;
            permitRow.Add(_hzpPermitLED);
            
            _hzpPermissiveStatus = new Label("STEAM DUMP: NOT PERMITTED");
            _hzpPermissiveStatus.style.color = COLOR_TEXT;
            _hzpPermissiveStatus.style.fontSize = 12;
            _hzpPermissiveStatus.style.unityFontStyleAndWeight = FontStyle.Bold;
            permitRow.Add(_hzpPermissiveStatus);
            
            centerCol.Add(permitRow);
            panel.Add(centerCol);
            
            // Right: HZP status
            var rightCol = CreatePanel("HZP STABILIZATION");
            rightCol.style.flexGrow = 1;
            
            var hzpPlaceholder = new Label("HZP stabilization status\nand handoff prerequisites\nwill be displayed here");
            hzpPlaceholder.style.color = COLOR_DIM;
            hzpPlaceholder.style.fontSize = 12;
            hzpPlaceholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            hzpPlaceholder.style.flexGrow = 1;
            rightCol.Add(hzpPlaceholder);
            
            panel.Add(rightCol);
            
            return panel;
        }
        
        private VisualElement BuildAlarmsTab()
        {
            var panel = CreatePanel("ANNUNCIATOR PANEL");
            
            // 6x5 grid of alarm tiles
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginTop = 10;
            grid.style.justifyContent = Justify.FlexStart;
            
            string[] alarmLabels = {
                "PZR HI PRESS", "PZR LO PRESS", "PZR HI LEVEL", "PZR LO LEVEL", "SUBCOOL LOW", "RCS FLOW LO",
                "SG HI PRESS", "VCT HI LEVEL", "VCT LO LEVEL", "LETDOWN ISO", "CHARGING HI", "RHR ISOLATED",
                "RCP 1 TRIP", "RCP 2 TRIP", "RCP 3 TRIP", "RCP 4 TRIP", "HEATER TRIP", "SPRAY ACTIVE",
                "MASS ALARM", "C-9 FAIL", "P-12 BLOCK", "COND VAC LO", "CST LO LEVEL", "FW PUMP TRIP",
                "RVLIS LOW", "BUBBLE FORM", "HZP READY", "STARTUP OK", "MODE CHANGE", "TRIP SIGNAL"
            };
            
            _alarmTiles = new AnnunciatorTilePOC[alarmLabels.Length];
            
            for (int i = 0; i < alarmLabels.Length; i++)
            {
                var tile = new AnnunciatorTilePOC();
                tile.style.width = 100;
                tile.style.height = 50;
                tile.style.marginRight = 8;
                tile.style.marginBottom = 8;
                tile.title = alarmLabels[i];
                _alarmTiles[i] = tile;
                grid.Add(tile);
            }
            
            panel.Add(grid);
            
            return panel;
        }
        
        private VisualElement BuildMiniTrends()
        {
            var panel = CreatePanel("TRENDS");
            panel.style.width = 180;
            
            var chartHolder = new VisualElement();
            chartHolder.style.flexGrow = 1;
            chartHolder.style.marginTop = 10;
            chartHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            
            _miniTrendChart = new StripChartPOC();
            _miniTrendChart.style.flexGrow = 1;
            _miniTrendChart.AddTrace("Press", new Color(1f, 0.667f, 0f), 0, 100);
            _miniTrendChart.AddTrace("Temp", COLOR_ACCENT, 0, 100);
            _miniTrendChart.AddTrace("Level", new Color(0.4f, 0.7f, 1f), 0, 100);
            chartHolder.Add(_miniTrendChart);
            panel.Add(chartHolder);
            
            return panel;
        }
        
        // ====================================================================
        // HELPER UI BUILDERS
        // ====================================================================
        
        private VisualElement CreatePanel(string title)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = COLOR_PANEL;
            panel.style.borderTopLeftRadius = 6;
            panel.style.borderTopRightRadius = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius = 6;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 11;
            titleLabel.style.color = COLOR_ACCENT;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(titleLabel);
            
            return panel;
        }
        
        private VisualElement CreateGaugeCell(string label, float min, float max, string unit, 
            out ArcGaugePOC gauge, out Label valueLabel)
        {
            var cell = new VisualElement();
            cell.style.width = Length.Percent(50);
            cell.style.alignItems = Align.Center;
            cell.style.paddingBottom = 10;
            
            var gaugeHolder = new VisualElement();
            gaugeHolder.style.width = 80;
            gaugeHolder.style.height = 65;
            gaugeHolder.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
            gaugeHolder.style.borderTopLeftRadius = 4;
            gaugeHolder.style.borderTopRightRadius = 4;
            gaugeHolder.style.borderBottomLeftRadius = 4;
            gaugeHolder.style.borderBottomRightRadius = 4;
            
            gauge = new ArcGaugePOC();
            gauge.style.flexGrow = 1;
            gauge.minValue = min;
            gauge.maxValue = max;
            gaugeHolder.Add(gauge);
            cell.Add(gaugeHolder);
            
            valueLabel = new Label($"--- {unit}");
            valueLabel.style.fontSize = 12;
            valueLabel.style.color = COLOR_ACCENT;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.marginTop = 3;
            cell.Add(valueLabel);
            
            var lbl = new Label(label);
            lbl.style.fontSize = 9;
            lbl.style.color = COLOR_DIM;
            cell.Add(lbl);
            
            return cell;
        }
        
        private VisualElement CreateParamRow(string label, out Label valueLabel)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 5;
            
            var lbl = new Label(label);
            lbl.style.fontSize = 10;
            lbl.style.color = COLOR_DIM;
            row.Add(lbl);
            
            valueLabel = new Label("---");
            valueLabel.style.fontSize = 10;
            valueLabel.style.color = COLOR_TEXT;
            row.Add(valueLabel);
            
            return row;
        }
        
        private VisualElement CreateFlowBar(string label, out LinearGaugePOC bar, float min, float max)
        {
            var row = new VisualElement();
            row.style.marginTop = 8;
            
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            
            var lbl = new Label(label);
            lbl.style.fontSize = 10;
            lbl.style.color = COLOR_DIM;
            headerRow.Add(lbl);
            row.Add(headerRow);
            
            bar = new LinearGaugePOC();
            bar.style.height = 14;
            bar.style.marginTop = 3;
            bar.minValue = min;
            bar.maxValue = max;
            row.Add(bar);
            
            return row;
        }
        
        // ====================================================================
        // DATA UPDATE
        // ====================================================================
        
        private void UpdateSimulatedData()
        {
            _simTime += 1f / updateRate;
            
            float sin1 = Mathf.Sin(_simTime * 0.3f);
            float sin2 = Mathf.Sin(_simTime * 0.5f);
            float progress = Mathf.Clamp01(_simTime / 180f);
            
            // Simulated values
            float pressure = 400f + progress * 1835f + sin1 * 20f;
            float tAvg = 100f + progress * 450f + sin1 * 5f;
            float tHot = tAvg + 15f + sin2 * 3f;
            float tCold = tAvg - 15f + sin2 * 3f;
            float level = progress < 0.3f ? 100f - progress * 200f : 60f + sin1 * 10f;
            float subcool = Mathf.Max(0, 80f - progress * 75f);
            float heaterPower = 50f + sin2 * 40f;
            float chargingFlow = 44f + sin1 * 20f;
            float letdownFlow = 75f + sin2 * 15f;
            
            int rcpCount = progress < 0.3f ? 0 : progress < 0.5f ? 2 : 4;
            bool rhrActive = pressure < 600f;
            
            string bubblePhase = progress < 0.1f ? "NONE" :
                                progress < 0.15f ? "DETECTION" :
                                progress < 0.2f ? "VERIFICATION" :
                                progress < 0.35f ? "DRAIN" :
                                progress < 0.4f ? "STABILIZE" :
                                progress < 0.5f ? "PRESSURIZE" : "COMPLETE";
            
            int mode = tAvg < 200f ? 5 : tAvg < 350f ? 4 : 3;
            
            // Update displays
            UpdateHeaderDisplays(mode, bubblePhase, _simTime, 1);
            UpdateOverviewTab(pressure, tAvg, level, subcool, heaterPower, chargingFlow, letdownFlow, 
                             bubblePhase, rcpCount, rhrActive);
            UpdatePressurizerTab(pressure, level, tAvg + 20f, heaterPower, 0f, bubblePhase, _simTime * 60f);
            UpdateRCSTab(tHot, tAvg, tCold, rhrActive, 1.5f, 0.5f, 1.0f);
            UpdateCVCSTab(chargingFlow, letdownFlow, 50f, 250000f, 0.1f, true, "1×75 gpm");
            UpdateSGTab(100f + progress * 800f, "OPEN_PREHEAT", 5f + progress * 20f, false, false);
            UpdateHZPTab(progress * 28f, false, false, false, "AWAIT_C9");
            UpdateAlarmStates(pressure, level, subcool, rhrActive);
            
            // Trends
            UpdateTrends(pressure / 25f, tAvg / 6.5f, level);
            
            // Animate vessel
            if (_pzrVessel != null && _currentTab == 1)
            {
                _pzrVessel.level = level;
                _pzrVessel.pressure = pressure;
                _pzrVessel.heaterPower = heaterPower;
                _pzrVessel.chargingFlow = chargingFlow;
                _pzrVessel.letdownFlow = letdownFlow;
                _pzrVessel.showBubbleZone = bubblePhase == "DRAIN" || bubblePhase == "DETECTION";
                _pzrVessel.UpdateFlowAnimation(1f / updateRate);
            }
            
            if (_simTime > 180f) _simTime = 0f;
        }
        
        private void UpdateFromEngine()
        {
            if (_engine == null) return;
            
            // Read from engine and update all displays
            // (Similar to UpdateSimulatedData but reading real values)
            
            float pressure = _engine.pressure;
            float tAvg = _engine.T_avg;
            float tHot = _engine.T_hot;
            float tCold = _engine.T_cold;
            float level = _engine.pzrLevel;
            float subcool = _engine.subcooling;
            float heaterPower = _engine.pzrHeaterPower * 100f / 1.8f; // Convert MW to %
            float chargingFlow = _engine.chargingFlow;
            float letdownFlow = _engine.letdownFlow;
            int rcpCount = _engine.rcpCount;
            bool rhrActive = _engine.rhrActive;
            string bubblePhase = _engine.bubblePhase.ToString();
            int mode = _engine.plantMode;
            float simTime = _engine.simTime;
            
            UpdateHeaderDisplays(mode, _engine.heatupPhaseDesc, simTime, _engine.currentSpeedIndex);
            UpdateOverviewTab(pressure, tAvg, level, subcool, heaterPower, chargingFlow, letdownFlow,
                             bubblePhase, rcpCount, rhrActive);
            
            // Update other tabs...
            UpdateTrends(pressure / 25f, tAvg / 6.5f, level);
        }
        
        private void UpdateHeaderDisplays(int mode, string phase, float simTime, int speedIndex)
        {
            string[] modeNames = { "", "", "", "MODE 3 - HOT STANDBY", "MODE 4 - HOT SHUTDOWN", "MODE 5 - COLD SHUTDOWN" };
            if (_modeLabel != null && mode >= 3 && mode <= 5) 
                _modeLabel.text = modeNames[mode];
            
            if (_phaseLabel != null) 
                _phaseLabel.text = phase;
            
            if (_simTimeLabel != null)
            {
                int hours = (int)simTime;
                int mins = (int)((simTime - hours) * 60);
                int secs = (int)((simTime * 3600) % 60);
                _simTimeLabel.text = $"T+ {hours:D2}:{mins:D2}:{secs:D2}";
            }
            
            if (_speedLabel != null)
            {
                string[] speeds = { "1x", "2x", "4x", "8x", "10x" };
                _speedLabel.text = speedIndex < speeds.Length ? speeds[speedIndex] : "1x";
            }
        }
        
        private void UpdateOverviewTab(float pressure, float tAvg, float level, float subcool,
                                       float heaterPower, float charging, float letdown,
                                       string bubblePhase, int rcpCount, bool rhrActive)
        {
            if (_ovPressureGauge != null) _ovPressureGauge.value = pressure;
            if (_ovTempGauge != null) _ovTempGauge.value = tAvg;
            if (_ovLevelGauge != null) _ovLevelGauge.value = level;
            if (_ovSubcoolGauge != null) _ovSubcoolGauge.value = subcool;
            
            if (_ovPressureValue != null) _ovPressureValue.text = $"{pressure:F0} psia";
            if (_ovTempValue != null) _ovTempValue.text = $"{tAvg:F1} °F";
            if (_ovLevelValue != null) _ovLevelValue.text = $"{level:F1} %";
            if (_ovSubcoolValue != null) _ovSubcoolValue.text = $"{subcool:F1} °F";
            
            if (_ovHeaterBar != null) _ovHeaterBar.value = heaterPower;
            if (_ovChargingBar != null) _ovChargingBar.value = charging;
            if (_ovLetdownBar != null) _ovLetdownBar.value = letdown;
            
            if (_ovBubblePhase != null) _ovBubblePhase.text = bubblePhase;
            if (_ovRcpStatus != null) _ovRcpStatus.text = $"RCPs: {rcpCount}/4";
            if (_ovRhrStatus != null) _ovRhrStatus.text = rhrActive ? "RHR: ACTIVE" : "RHR: ISOLATED";
            
            for (int i = 0; i < 4; i++)
            {
                if (_ovRcpLEDs[i] != null)
                    _ovRcpLEDs[i].state = i < rcpCount ? LEDState.Normal : LEDState.Off;
            }
        }
        
        private void UpdatePressurizerTab(float pressure, float level, float tSat, 
                                          float heaterPower, float sprayFlow,
                                          string bubblePhase, float phaseDuration)
        {
            if (_pzrPressure != null) _pzrPressure.text = $"{pressure:F0} psia";
            if (_pzrLevel != null) _pzrLevel.text = $"{level:F1} %";
            if (_pzrTsat != null) _pzrTsat.text = $"{tSat:F1} °F";
            if (_pzrHeaterPower != null) _pzrHeaterPower.text = $"{heaterPower:F0} %";
            if (_pzrHeaterMode != null) _pzrHeaterMode.text = heaterPower > 10 ? "AUTO" : "OFF";
            if (_pzrSprayFlow != null) _pzrSprayFlow.text = $"{sprayFlow:F0} gpm";
            if (_pzrBubblePhase != null) _pzrBubblePhase.text = bubblePhase;
            if (_pzrBubbleDuration != null) _pzrBubbleDuration.text = $"{phaseDuration:F0} min";
            
            // Update phase indicator
            int phaseIndex = System.Array.IndexOf(
                new[] { "NONE", "DETECTION", "VERIFICATION", "DRAIN", "STABILIZE", "PRESSURIZE", "COMPLETE" },
                bubblePhase);
            
            for (int i = 0; i < _bubblePhaseLabels.Length; i++)
            {
                if (_bubblePhaseLabels[i] != null)
                {
                    bool active = i == phaseIndex || (phaseIndex == 6 && i == 6);
                    bool past = i < phaseIndex;
                    _bubblePhaseLabels[i].style.backgroundColor = active ? COLOR_TAB_ACTIVE : 
                                                                  past ? new Color(0.15f, 0.2f, 0.15f) : 
                                                                  new Color(0.1f, 0.1f, 0.12f);
                    _bubblePhaseLabels[i].style.color = active ? COLOR_ACCENT : past ? COLOR_TEXT : COLOR_DIM;
                }
            }
        }
        
        private void UpdateRCSTab(float tHot, float tAvg, float tCold, bool rhrActive,
                                  float rhrHeat, float sgHeat, float netHeat)
        {
            if (_rcsThot != null) _rcsThot.value = tHot;
            if (_rcsTavg != null) _rcsTavg.value = tAvg;
            if (_rcsTcold != null) _rcsTcold.value = tCold;
            if (_rcsHeatBalance != null) _rcsHeatBalance.value = netHeat;
            
            if (_rcsRhrMode != null) _rcsRhrMode.text = rhrActive ? "HEATUP" : "ISOLATED";
            if (_rcsRhrHeat != null) _rcsRhrHeat.text = $"{rhrHeat:F2} MW";
            if (_rcsSgHeat != null) _rcsSgHeat.text = $"-{sgHeat:F2} MW";
            if (_rcsNetHeat != null) _rcsNetHeat.text = $"{netHeat:F2} MW";
        }
        
        private void UpdateCVCSTab(float charging, float letdown, float vctLevel,
                                   float massLedger, float massDrift, bool massOK, string orifice)
        {
            if (_cvcsChargingBar != null) _cvcsChargingBar.value = charging;
            if (_cvcsLetdownBar != null) _cvcsLetdownBar.value = letdown;
            if (_cvcsVctTank != null) _cvcsVctTank.value = vctLevel;
            
            if (_cvcsMassLedger != null) _cvcsMassLedger.text = $"{massLedger:F0} lbm";
            if (_cvcsMassDrift != null) _cvcsMassDrift.text = $"{massDrift:F2} %";
            if (_cvcsMassStatus != null) _cvcsMassStatus.text = massOK ? "OK" : "ALARM";
            if (_cvcsMassLED != null) _cvcsMassLED.state = massOK ? LEDState.Normal : LEDState.Alarm;
            if (_cvcsOrificeLineup != null) _cvcsOrificeLineup.text = orifice;
        }
        
        private void UpdateSGTab(float secondaryPressure, string boundaryState, float thermocline,
                                bool boiling, bool draining)
        {
            if (_sgSecondaryPressure != null) _sgSecondaryPressure.value = secondaryPressure;
            if (_sgLevelTank != null) _sgLevelTank.value = 50f; // Simplified
            if (_sgBoundaryState != null) _sgBoundaryState.text = boundaryState;
            if (_sgThermoclineHeight != null) _sgThermoclineHeight.text = $"{thermocline:F1} ft";
            if (_sgBoilingStatus != null) _sgBoilingStatus.text = boiling ? "ACTIVE" : "NONE";
            if (_sgDrainingStatus != null) _sgDrainingStatus.text = draining ? "ACTIVE" : "IDLE";
        }
        
        private void UpdateHZPTab(float vacuum, bool c9Satisfied, bool p12Bypass, bool permitted, string bridgeState)
        {
            if (_hzpCondenserVacuum != null) _hzpCondenserVacuum.value = vacuum;
            if (_hzpSteamDumpBar != null) _hzpSteamDumpBar.value = permitted ? 50f : 0f;
            if (_hzpBridgeState != null) _hzpBridgeState.text = bridgeState;
            
            if (_hzpC9LED != null) _hzpC9LED.state = c9Satisfied ? LEDState.Normal : LEDState.Off;
            if (_hzpC9Status != null) _hzpC9Status.text = c9Satisfied ? "C-9: SATISFIED" : "C-9: NOT SATISFIED";
            
            if (_hzpP12LED != null) _hzpP12LED.state = p12Bypass ? LEDState.Normal : LEDState.Off;
            if (_hzpP12Status != null) _hzpP12Status.text = p12Bypass ? "P-12: BYPASSED" : "P-12: BLOCKED";
            
            if (_hzpPermitLED != null) _hzpPermitLED.state = permitted ? LEDState.Normal : LEDState.Off;
            if (_hzpPermissiveStatus != null) 
                _hzpPermissiveStatus.text = permitted ? "STEAM DUMP: PERMITTED" : "STEAM DUMP: NOT PERMITTED";
        }
        
        private void UpdateAlarmStates(float pressure, float level, float subcool, bool rhrActive)
        {
            if (_alarmTiles == null) return;
            
            // Simple alarm logic for demo
            if (_alarmTiles.Length > 0) _alarmTiles[0].state = pressure > 2235 ? AnnunciatorState.Alerting : AnnunciatorState.Normal;
            if (_alarmTiles.Length > 1) _alarmTiles[1].state = pressure < 400 ? AnnunciatorState.Alerting : AnnunciatorState.Normal;
            if (_alarmTiles.Length > 3) _alarmTiles[3].state = level < 17 ? AnnunciatorState.Alerting : AnnunciatorState.Normal;
            if (_alarmTiles.Length > 4) _alarmTiles[4].state = subcool < 10 ? AnnunciatorState.Alerting : AnnunciatorState.Normal;
            if (_alarmTiles.Length > 11) _alarmTiles[11].state = !rhrActive ? AnnunciatorState.Acknowledged : AnnunciatorState.Normal;
        }
        
        private void UpdateTrends(float pressureNorm, float tempNorm, float level)
        {
            if (_ovTrendChart != null)
            {
                _ovTrendChart.AddValue(0, pressureNorm);
                _ovTrendChart.AddValue(1, tempNorm);
                _ovTrendChart.AddValue(2, level);
            }
            
            if (_pzrTrendChart != null && _currentTab == 1)
            {
                _pzrTrendChart.AddValue(0, pressureNorm);
                _pzrTrendChart.AddValue(1, level);
                _pzrTrendChart.AddValue(2, 50f); // Heater
            }
            
            if (_miniTrendChart != null)
            {
                _miniTrendChart.AddValue(0, pressureNorm);
                _miniTrendChart.AddValue(1, tempNorm);
                _miniTrendChart.AddValue(2, level);
            }
        }
    }
}
