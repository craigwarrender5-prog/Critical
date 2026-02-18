// ============================================================================
// CRITICAL: Master the Atom — Edgewise Meter Element (UI Toolkit)
// EdgewiseMeterElement.cs
// ============================================================================
//
// PURPOSE:
//   Custom UI Toolkit VisualElement for linear edgewise panel meters.
//   Supports both horizontal and vertical orientations for deviation
//   indicators, level displays, and differential pressure readouts.
//
// USAGE:
//   <critical:EdgewiseMeterElement orientation="Horizontal" 
//       min-value="-20" max-value="20" value="5" center-zero="true"
//       unit="kPa x10" title="ΔP INDICATOR" />
//
// FEATURES:
//   - Horizontal and vertical orientations
//   - Center-zero option for deviation display
//   - Configurable tick marks and labels
//   - Optional setpoint marker
//   - Colored range bands (normal/warning/alarm)
//   - Industrial bezel styling
//
// REFERENCE:
//   Bendou YE.T-101 Edgewise Panel Meter
//   Yokogawa/Westinghouse linear indicator panels
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Critical.UI.Elements
{
    // ========================================================================
    // ENUMERATIONS
    // ========================================================================
    
    /// <summary>
    /// Meter orientation.
    /// </summary>
    public enum MeterOrientation
    {
        Horizontal,
        Vertical
    }
    
    // ========================================================================
    // EDGEWISE METER ELEMENT
    // ========================================================================
    
    /// <summary>
    /// Linear edgewise panel meter for deviation, level, or pressure display.
    /// </summary>
    public class EdgewiseMeterElement : VisualElement
    {
        // ====================================================================
        // UXML FACTORY
        // ====================================================================
        
        public new class UxmlFactory : UxmlFactory<EdgewiseMeterElement, UxmlTraits> { }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlEnumAttributeDescription<MeterOrientation> m_Orientation =
                new UxmlEnumAttributeDescription<MeterOrientation> { name = "orientation", defaultValue = MeterOrientation.Horizontal };
            
            private UxmlFloatAttributeDescription m_MinValue =
                new UxmlFloatAttributeDescription { name = "min-value", defaultValue = -20f };
            
            private UxmlFloatAttributeDescription m_MaxValue =
                new UxmlFloatAttributeDescription { name = "max-value", defaultValue = 20f };
            
            private UxmlFloatAttributeDescription m_Value =
                new UxmlFloatAttributeDescription { name = "value", defaultValue = 0f };
            
            private UxmlBoolAttributeDescription m_CenterZero =
                new UxmlBoolAttributeDescription { name = "center-zero", defaultValue = true };
            
            private UxmlFloatAttributeDescription m_MajorTickInterval =
                new UxmlFloatAttributeDescription { name = "major-tick-interval", defaultValue = 10f };
            
            private UxmlIntAttributeDescription m_MinorTicksPerMajor =
                new UxmlIntAttributeDescription { name = "minor-ticks-per-major", defaultValue = 5 };
            
            private UxmlStringAttributeDescription m_Unit =
                new UxmlStringAttributeDescription { name = "unit", defaultValue = "" };
            
            private UxmlStringAttributeDescription m_Title =
                new UxmlStringAttributeDescription { name = "title", defaultValue = "" };
            
            private UxmlFloatAttributeDescription m_Setpoint =
                new UxmlFloatAttributeDescription { name = "setpoint", defaultValue = float.NaN };
            
            private UxmlBoolAttributeDescription m_ShowSetpoint =
                new UxmlBoolAttributeDescription { name = "show-setpoint", defaultValue = false };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var meter = ve as EdgewiseMeterElement;
                
                meter.Orientation = m_Orientation.GetValueFromBag(bag, cc);
                meter.MinValue = m_MinValue.GetValueFromBag(bag, cc);
                meter.MaxValue = m_MaxValue.GetValueFromBag(bag, cc);
                meter.Value = m_Value.GetValueFromBag(bag, cc);
                meter.CenterZero = m_CenterZero.GetValueFromBag(bag, cc);
                meter.MajorTickInterval = m_MajorTickInterval.GetValueFromBag(bag, cc);
                meter.MinorTicksPerMajor = m_MinorTicksPerMajor.GetValueFromBag(bag, cc);
                meter.Unit = m_Unit.GetValueFromBag(bag, cc);
                meter.Title = m_Title.GetValueFromBag(bag, cc);
                meter.Setpoint = m_Setpoint.GetValueFromBag(bag, cc);
                meter.ShowSetpoint = m_ShowSetpoint.GetValueFromBag(bag, cc);
            }
        }
        
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const string USS_METER = "edgewise-meter";
        private const string USS_HORIZONTAL = "edgewise-meter--horizontal";
        private const string USS_VERTICAL = "edgewise-meter--vertical";
        private const string USS_FACE = "edgewise-meter__face";
        private const string USS_SCALE = "edgewise-meter__scale";
        private const string USS_POINTER = "edgewise-meter__pointer";
        private const string USS_SETPOINT = "edgewise-meter__setpoint";
        private const string USS_TICK = "edgewise-meter__tick";
        private const string USS_TICK_MAJOR = "edgewise-meter__tick--major";
        private const string USS_LABEL = "edgewise-meter__label";
        private const string USS_LABEL_ZERO = "edgewise-meter__label--zero";
        private const string USS_UNIT = "edgewise-meter__unit";
        private const string USS_TITLE = "edgewise-meter__title";
        
        // Scale positioning (percentage of face)
        private const float SCALE_START_PCT = 0.08f;
        private const float SCALE_END_PCT = 0.92f;
        
        // ====================================================================
        // BACKING FIELDS
        // ====================================================================
        
        private MeterOrientation m_Orientation = MeterOrientation.Horizontal;
        private float m_MinValue = -20f;
        private float m_MaxValue = 20f;
        private float m_Value = 0f;
        private bool m_CenterZero = true;
        private float m_MajorTickInterval = 10f;
        private int m_MinorTicksPerMajor = 5;
        private string m_Unit = "";
        private string m_Title = "";
        private float m_Setpoint = float.NaN;
        private bool m_ShowSetpoint = false;
        
        // Child elements
        private VisualElement m_Face;
        private VisualElement m_Scale;
        private VisualElement m_Pointer;
        private VisualElement m_SetpointMarker;
        private Label m_UnitLabel;
        private Label m_TitleLabel;
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        /// <summary>Meter orientation (Horizontal or Vertical).</summary>
        public MeterOrientation Orientation
        {
            get => m_Orientation;
            set
            {
                if (m_Orientation != value)
                {
                    RemoveFromClassList(m_Orientation == MeterOrientation.Horizontal ? USS_HORIZONTAL : USS_VERTICAL);
                    m_Orientation = value;
                    AddToClassList(m_Orientation == MeterOrientation.Horizontal ? USS_HORIZONTAL : USS_VERTICAL);
                    RegenerateScale();
                    UpdatePointerPosition();
                }
            }
        }
        
        /// <summary>Minimum value on the scale.</summary>
        public float MinValue
        {
            get => m_MinValue;
            set { m_MinValue = value; RegenerateScale(); UpdatePointerPosition(); }
        }
        
        /// <summary>Maximum value on the scale.</summary>
        public float MaxValue
        {
            get => m_MaxValue;
            set { m_MaxValue = value; RegenerateScale(); UpdatePointerPosition(); }
        }
        
        /// <summary>Current value displayed by the pointer.</summary>
        public float Value
        {
            get => m_Value;
            set { m_Value = Mathf.Clamp(value, m_MinValue, m_MaxValue); UpdatePointerPosition(); }
        }
        
        /// <summary>Whether zero is at the center of the scale.</summary>
        public bool CenterZero
        {
            get => m_CenterZero;
            set { m_CenterZero = value; RegenerateScale(); }
        }
        
        /// <summary>Interval between major tick marks.</summary>
        public float MajorTickInterval
        {
            get => m_MajorTickInterval;
            set { m_MajorTickInterval = value; RegenerateScale(); }
        }
        
        /// <summary>Number of minor ticks between each major tick.</summary>
        public int MinorTicksPerMajor
        {
            get => m_MinorTicksPerMajor;
            set { m_MinorTicksPerMajor = value; RegenerateScale(); }
        }
        
        /// <summary>Unit label (e.g., "kPa x10").</summary>
        public string Unit
        {
            get => m_Unit;
            set { m_Unit = value; if (m_UnitLabel != null) m_UnitLabel.text = value; }
        }
        
        /// <summary>Title label displayed below the meter.</summary>
        public string Title
        {
            get => m_Title;
            set { m_Title = value; if (m_TitleLabel != null) m_TitleLabel.text = value; }
        }
        
        /// <summary>Setpoint value for the marker.</summary>
        public float Setpoint
        {
            get => m_Setpoint;
            set { m_Setpoint = value; UpdateSetpointPosition(); }
        }
        
        /// <summary>Whether to show the setpoint marker.</summary>
        public bool ShowSetpoint
        {
            get => m_ShowSetpoint;
            set
            {
                m_ShowSetpoint = value;
                if (m_SetpointMarker != null)
                    m_SetpointMarker.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public EdgewiseMeterElement()
        {
            AddToClassList(USS_METER);
            AddToClassList(USS_HORIZONTAL);
            
            BuildVisualTree();
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        // ====================================================================
        // VISUAL TREE CONSTRUCTION
        // ====================================================================
        
        private void BuildVisualTree()
        {
            style.position = Position.Relative;
            style.flexDirection = FlexDirection.Column;
            style.width = 180f;
            style.height = 58f;
            style.minWidth = 140f;
            style.minHeight = 52f;

            // Face (display area)
            m_Face = new VisualElement();
            m_Face.AddToClassList(USS_FACE);
            m_Face.style.position = Position.Relative;
            m_Face.style.width = new Length(100f, LengthUnit.Percent);
            m_Face.style.height = 42f;
            m_Face.style.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);
            m_Face.style.borderTopWidth = 1f;
            m_Face.style.borderBottomWidth = 1f;
            m_Face.style.borderLeftWidth = 1f;
            m_Face.style.borderRightWidth = 1f;
            m_Face.style.borderTopColor = new Color(0.24f, 0.28f, 0.34f, 1f);
            m_Face.style.borderBottomColor = new Color(0.24f, 0.28f, 0.34f, 1f);
            m_Face.style.borderLeftColor = new Color(0.24f, 0.28f, 0.34f, 1f);
            m_Face.style.borderRightColor = new Color(0.24f, 0.28f, 0.34f, 1f);
            m_Face.style.borderTopLeftRadius = 3f;
            m_Face.style.borderTopRightRadius = 3f;
            m_Face.style.borderBottomLeftRadius = 3f;
            m_Face.style.borderBottomRightRadius = 3f;
            Add(m_Face);
            
            // Scale container
            m_Scale = new VisualElement();
            m_Scale.AddToClassList(USS_SCALE);
            m_Scale.style.position = Position.Absolute;
            m_Scale.style.left = 0f;
            m_Scale.style.top = 0f;
            m_Scale.style.width = new Length(100f, LengthUnit.Percent);
            m_Scale.style.height = new Length(100f, LengthUnit.Percent);
            m_Face.Add(m_Scale);
            
            // Unit label
            m_UnitLabel = new Label(m_Unit);
            m_UnitLabel.AddToClassList(USS_UNIT);
            m_UnitLabel.style.position = Position.Absolute;
            m_UnitLabel.style.right = 4f;
            m_UnitLabel.style.top = 2f;
            m_UnitLabel.style.fontSize = 8f;
            m_UnitLabel.style.color = new Color(0.70f, 0.77f, 0.88f, 1f);
            m_Face.Add(m_UnitLabel);
            
            // Setpoint marker
            m_SetpointMarker = new VisualElement();
            m_SetpointMarker.AddToClassList(USS_SETPOINT);
            m_SetpointMarker.style.position = Position.Absolute;
            m_SetpointMarker.style.width = 2f;
            m_SetpointMarker.style.height = new Length(70f, LengthUnit.Percent);
            m_SetpointMarker.style.top = new Length(15f, LengthUnit.Percent);
            m_SetpointMarker.style.backgroundColor = new Color(1f, 0.67f, 0f, 1f);
            m_SetpointMarker.style.display = DisplayStyle.None;
            m_Face.Add(m_SetpointMarker);
            
            // Pointer
            m_Pointer = new VisualElement();
            m_Pointer.AddToClassList(USS_POINTER);
            m_Pointer.style.position = Position.Absolute;
            m_Pointer.style.width = 3f;
            m_Pointer.style.height = new Length(72f, LengthUnit.Percent);
            m_Pointer.style.top = new Length(14f, LengthUnit.Percent);
            m_Pointer.style.backgroundColor = new Color(0.97f, 0.98f, 1f, 1f);
            m_Pointer.style.borderTopLeftRadius = 2f;
            m_Pointer.style.borderTopRightRadius = 2f;
            m_Pointer.style.borderBottomLeftRadius = 2f;
            m_Pointer.style.borderBottomRightRadius = 2f;
            m_Face.Add(m_Pointer);
            
            // Title label (below meter)
            m_TitleLabel = new Label(m_Title);
            m_TitleLabel.AddToClassList(USS_TITLE);
            m_TitleLabel.style.fontSize = 8f;
            m_TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_TitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_TitleLabel.style.color = new Color(0.62f, 0.70f, 0.82f, 1f);
            m_TitleLabel.style.marginTop = 2f;
            Add(m_TitleLabel);
        }
        
        // ====================================================================
        // SCALE GENERATION
        // ====================================================================
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            RegenerateScale();
            UpdatePointerPosition();
            UpdateSetpointPosition();
        }
        
        private void RegenerateScale()
        {
            if (m_Scale == null) return;
            
            m_Scale.Clear();
            
            float faceSize = m_Orientation == MeterOrientation.Horizontal 
                ? m_Face.resolvedStyle.width 
                : m_Face.resolvedStyle.height;
            
            if (float.IsNaN(faceSize) || faceSize <= 0) return;
            
            float range = m_MaxValue - m_MinValue;
            if (range <= 0 || m_MajorTickInterval <= 0) return;
            
            float scaleLength = faceSize * (SCALE_END_PCT - SCALE_START_PCT);
            
            // Generate major ticks and labels
            int majorTickCount = Mathf.FloorToInt(range / m_MajorTickInterval) + 1;
            
            for (int i = 0; i < majorTickCount; i++)
            {
                float tickValue = m_MinValue + (i * m_MajorTickInterval);
                if (tickValue > m_MaxValue + 0.001f) break;
                
                float normalizedPos = (tickValue - m_MinValue) / range;
                float position = SCALE_START_PCT + normalizedPos * (SCALE_END_PCT - SCALE_START_PCT);
                
                // Major tick
                CreateTick(position, true);
                
                // Label
                CreateLabel(position, tickValue);
                
                // Minor ticks
                if (i < majorTickCount - 1 && m_MinorTicksPerMajor > 0)
                {
                    float minorInterval = m_MajorTickInterval / (m_MinorTicksPerMajor + 1);
                    for (int j = 1; j <= m_MinorTicksPerMajor; j++)
                    {
                        float minorValue = tickValue + (j * minorInterval);
                        if (minorValue >= m_MaxValue) break;
                        
                        float minorNormalized = (minorValue - m_MinValue) / range;
                        float minorPos = SCALE_START_PCT + minorNormalized * (SCALE_END_PCT - SCALE_START_PCT);
                        
                        CreateTick(minorPos, false);
                    }
                }
            }
        }
        
        private void CreateTick(float positionPct, bool isMajor)
        {
            var tick = new VisualElement();
            tick.AddToClassList(USS_TICK);
            if (isMajor) tick.AddToClassList(USS_TICK_MAJOR);
            tick.style.position = Position.Absolute;
            tick.style.backgroundColor = isMajor ? new Color(0.80f, 0.84f, 0.92f, 0.85f) : new Color(0.54f, 0.59f, 0.68f, 0.75f);
            
            if (m_Orientation == MeterOrientation.Horizontal)
            {
                tick.style.width = isMajor ? 2f : 1f;
                tick.style.height = isMajor ? 10f : 6f;
                tick.style.bottom = 6f;
                tick.style.left = Length.Percent(positionPct * 100f);
                tick.style.translate = new Translate(Length.Percent(-50), 0f);
            }
            else
            {
                // Vertical: bottom is min, top is max
                tick.style.height = isMajor ? 2f : 1f;
                tick.style.width = isMajor ? 10f : 6f;
                tick.style.left = 6f;
                tick.style.bottom = Length.Percent(positionPct * 100f);
                tick.style.translate = new Translate(0f, Length.Percent(50));
            }
            
            m_Scale.Add(tick);
        }
        
        private void CreateLabel(float positionPct, float value)
        {
            var label = new Label();
            label.AddToClassList(USS_LABEL);
            label.style.position = Position.Absolute;
            label.style.fontSize = 7f;
            label.style.color = new Color(0.76f, 0.81f, 0.89f, 1f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Format value
            if (Mathf.Abs(value) < 0.001f)
            {
                label.text = "0";
                label.AddToClassList(USS_LABEL_ZERO);
            }
            else
            {
                // Show + sign for positive values if center-zero
                if (m_CenterZero && value > 0)
                    label.text = $"+{value:0}";
                else
                    label.text = $"{value:0}";
            }
            
            if (m_Orientation == MeterOrientation.Horizontal)
            {
                label.style.left = Length.Percent(positionPct * 100f);
                label.style.bottom = -2f;
                label.style.translate = new Translate(Length.Percent(-50), 0f);
            }
            else
            {
                label.style.bottom = Length.Percent(positionPct * 100f);
                label.style.left = 16f;
                label.style.translate = new Translate(0f, Length.Percent(50));
            }
            
            m_Scale.Add(label);
        }
        
        // ====================================================================
        // POINTER POSITION
        // ====================================================================
        
        private void UpdatePointerPosition()
        {
            if (m_Pointer == null) return;
            
            float range = m_MaxValue - m_MinValue;
            if (range <= 0) return;
            
            float normalizedPos = (m_Value - m_MinValue) / range;
            float positionPct = SCALE_START_PCT + normalizedPos * (SCALE_END_PCT - SCALE_START_PCT);
            
            if (m_Orientation == MeterOrientation.Horizontal)
            {
                m_Pointer.style.left = Length.Percent(positionPct * 100f);
                m_Pointer.style.translate = new Translate(Length.Percent(-50), 0f);
                m_Pointer.style.bottom = StyleKeyword.Auto;
            }
            else
            {
                m_Pointer.style.bottom = Length.Percent(positionPct * 100f);
                m_Pointer.style.left = 6f;
                m_Pointer.style.width = new Length(72f, LengthUnit.Percent);
                m_Pointer.style.height = 3f;
                m_Pointer.style.top = StyleKeyword.Auto;
                m_Pointer.style.translate = new Translate(0f, Length.Percent(50));
            }
        }
        
        private void UpdateSetpointPosition()
        {
            if (m_SetpointMarker == null || float.IsNaN(m_Setpoint)) return;
            
            float range = m_MaxValue - m_MinValue;
            if (range <= 0) return;
            
            float clampedSetpoint = Mathf.Clamp(m_Setpoint, m_MinValue, m_MaxValue);
            float normalizedPos = (clampedSetpoint - m_MinValue) / range;
            float positionPct = SCALE_START_PCT + normalizedPos * (SCALE_END_PCT - SCALE_START_PCT);
            
            if (m_Orientation == MeterOrientation.Horizontal)
            {
                m_SetpointMarker.style.left = Length.Percent(positionPct * 100f);
                m_SetpointMarker.style.translate = new Translate(Length.Percent(-50), 0f);
            }
            else
            {
                m_SetpointMarker.style.bottom = Length.Percent(positionPct * 100f);
                m_SetpointMarker.style.left = 6f;
                m_SetpointMarker.style.height = 2f;
                m_SetpointMarker.style.width = new Length(72f, LengthUnit.Percent);
                m_SetpointMarker.style.top = StyleKeyword.Auto;
                m_SetpointMarker.style.translate = new Translate(0f, Length.Percent(50));
            }
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Set the value with optional animation.
        /// </summary>
        public void SetValue(float newValue, bool animate = false)
        {
            // TODO: Implement smooth animation
            Value = newValue;
        }
        
        /// <summary>
        /// Get the deviation from setpoint (if setpoint is defined).
        /// </summary>
        public float GetDeviation()
        {
            if (float.IsNaN(m_Setpoint)) return 0f;
            return m_Value - m_Setpoint;
        }
    }
}
