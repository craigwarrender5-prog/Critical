// ============================================================================
// CRITICAL: Master the Atom — Analog Gauge Visual Element
// AnalogGaugeElement.cs
// ============================================================================
//
// PURPOSE:
//   Custom UI Toolkit VisualElement that renders a realistic analog dial gauge.
//   Modeled after industrial bimetallic dial thermometers used for local
//   temperature indication on PWR process piping.
//
// USAGE:
//   <critical:AnalogGaugeElement min-value="0" max-value="250" value="125" 
//                                 unit="°F" title="TI-101" />
//
// FEATURES:
//   - Configurable min/max range
//   - Animated needle with smooth rotation
//   - Auto-generated scale markings
//   - Multiple size variants (small, standard, large)
//   - Supports °F, °C, PSIG, and custom units
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Critical.UI.Elements
{
    /// <summary>
    /// Analog dial gauge element for displaying temperature, pressure, or other values.
    /// Renders with realistic industrial gauge appearance including bezel, face, 
    /// scale markings, and rotating needle.
    /// </summary>
    public class AnalogGaugeElement : VisualElement
    {
        // ====================================================================
        // UXML FACTORY
        // ====================================================================
        
        public new class UxmlFactory : UxmlFactory<AnalogGaugeElement, UxmlTraits> { }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlFloatAttributeDescription m_MinValue = 
                new UxmlFloatAttributeDescription { name = "min-value", defaultValue = 0f };
            
            private UxmlFloatAttributeDescription m_MaxValue = 
                new UxmlFloatAttributeDescription { name = "max-value", defaultValue = 250f };
            
            private UxmlFloatAttributeDescription m_Value = 
                new UxmlFloatAttributeDescription { name = "value", defaultValue = 0f };
            
            private UxmlStringAttributeDescription m_Unit = 
                new UxmlStringAttributeDescription { name = "unit", defaultValue = "°F" };
            
            private UxmlStringAttributeDescription m_Title = 
                new UxmlStringAttributeDescription { name = "title", defaultValue = "" };
            
            private UxmlFloatAttributeDescription m_MajorTickInterval = 
                new UxmlFloatAttributeDescription { name = "major-tick-interval", defaultValue = 50f };
            
            private UxmlIntAttributeDescription m_MinorTicksPerMajor = 
                new UxmlIntAttributeDescription { name = "minor-ticks-per-major", defaultValue = 5 };
            
            private UxmlFloatAttributeDescription m_StartAngle = 
                new UxmlFloatAttributeDescription { name = "start-angle", defaultValue = -135f };
            
            private UxmlFloatAttributeDescription m_EndAngle = 
                new UxmlFloatAttributeDescription { name = "end-angle", defaultValue = 135f };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var gauge = ve as AnalogGaugeElement;
                
                gauge.MinValue = m_MinValue.GetValueFromBag(bag, cc);
                gauge.MaxValue = m_MaxValue.GetValueFromBag(bag, cc);
                gauge.Value = m_Value.GetValueFromBag(bag, cc);
                gauge.Unit = m_Unit.GetValueFromBag(bag, cc);
                gauge.Title = m_Title.GetValueFromBag(bag, cc);
                gauge.MajorTickInterval = m_MajorTickInterval.GetValueFromBag(bag, cc);
                gauge.MinorTicksPerMajor = m_MinorTicksPerMajor.GetValueFromBag(bag, cc);
                gauge.StartAngle = m_StartAngle.GetValueFromBag(bag, cc);
                gauge.EndAngle = m_EndAngle.GetValueFromBag(bag, cc);
            }
        }
        
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const string USS_CLASS = "analog-gauge";
        private const string USS_FACE = "analog-gauge__face";
        private const string USS_INNER_FACE = "analog-gauge__inner-face";
        private const string USS_NEEDLE_CONTAINER = "analog-gauge__needle-container";
        private const string USS_NEEDLE = "analog-gauge__needle";
        private const string USS_HUB = "analog-gauge__hub";
        private const string USS_UNIT_LABEL = "analog-gauge__unit-label";
        private const string USS_TITLE = "analog-gauge__title";
        private const string USS_SCALE_LABEL = "analog-gauge__scale-label";
        
        // ====================================================================
        // BACKING FIELDS
        // ====================================================================
        
        private float m_MinValue = 0f;
        private float m_MaxValue = 250f;
        private float m_Value = 0f;
        private string m_Unit = "°F";
        private string m_Title = "";
        private float m_MajorTickInterval = 50f;
        private int m_MinorTicksPerMajor = 5;
        private float m_StartAngle = -135f;  // 7 o'clock position
        private float m_EndAngle = 135f;     // 5 o'clock position
        
        // Child elements
        private VisualElement m_Face;
        private VisualElement m_InnerFace;
        private VisualElement m_NeedleContainer;
        private VisualElement m_Needle;
        private VisualElement m_Hub;
        private Label m_UnitLabel;
        private Label m_TitleLabel;
        private VisualElement m_ScaleContainer;
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        /// <summary>Minimum value on the gauge scale.</summary>
        public float MinValue
        {
            get => m_MinValue;
            set
            {
                m_MinValue = value;
                RegenerateScale();
                UpdateNeedleRotation();
            }
        }
        
        /// <summary>Maximum value on the gauge scale.</summary>
        public float MaxValue
        {
            get => m_MaxValue;
            set
            {
                m_MaxValue = value;
                RegenerateScale();
                UpdateNeedleRotation();
            }
        }
        
        /// <summary>Current value displayed by the needle.</summary>
        public float Value
        {
            get => m_Value;
            set
            {
                m_Value = Mathf.Clamp(value, m_MinValue, m_MaxValue);
                UpdateNeedleRotation();
            }
        }
        
        /// <summary>Unit label (e.g., °F, °C, PSIG).</summary>
        public string Unit
        {
            get => m_Unit;
            set
            {
                m_Unit = value;
                if (m_UnitLabel != null)
                    m_UnitLabel.text = value;
            }
        }
        
        /// <summary>Title/tag label (e.g., TI-101).</summary>
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                if (m_TitleLabel != null)
                    m_TitleLabel.text = value;
            }
        }
        
        /// <summary>Interval between major tick marks.</summary>
        public float MajorTickInterval
        {
            get => m_MajorTickInterval;
            set
            {
                m_MajorTickInterval = value;
                RegenerateScale();
            }
        }
        
        /// <summary>Number of minor ticks between each major tick.</summary>
        public int MinorTicksPerMajor
        {
            get => m_MinorTicksPerMajor;
            set
            {
                m_MinorTicksPerMajor = value;
                RegenerateScale();
            }
        }
        
        /// <summary>Starting angle in degrees (minimum value position).</summary>
        public float StartAngle
        {
            get => m_StartAngle;
            set
            {
                m_StartAngle = value;
                RegenerateScale();
                UpdateNeedleRotation();
            }
        }
        
        /// <summary>Ending angle in degrees (maximum value position).</summary>
        public float EndAngle
        {
            get => m_EndAngle;
            set
            {
                m_EndAngle = value;
                RegenerateScale();
                UpdateNeedleRotation();
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public AnalogGaugeElement()
        {
            AddToClassList(USS_CLASS);
            
            BuildVisualTree();
            
            // Defer scale generation until layout is complete
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        // ====================================================================
        // VISUAL TREE CONSTRUCTION
        // ====================================================================
        
        private void BuildVisualTree()
        {
            style.position = Position.Relative;
            style.overflow = Overflow.Hidden;
            style.width = 102f;
            style.height = 102f;

            // Outer face (metallic bezel)
            m_Face = new VisualElement();
            m_Face.AddToClassList(USS_FACE);
            m_Face.style.position = Position.Absolute;
            m_Face.style.left = 0f;
            m_Face.style.top = 0f;
            m_Face.style.width = new Length(100f, LengthUnit.Percent);
            m_Face.style.height = new Length(100f, LengthUnit.Percent);
            m_Face.style.borderTopLeftRadius = 999f;
            m_Face.style.borderTopRightRadius = 999f;
            m_Face.style.borderBottomLeftRadius = 999f;
            m_Face.style.borderBottomRightRadius = 999f;
            m_Face.style.backgroundColor = new Color(0.70f, 0.72f, 0.74f, 1f);
            m_Face.style.borderTopWidth = 3f;
            m_Face.style.borderBottomWidth = 3f;
            m_Face.style.borderLeftWidth = 3f;
            m_Face.style.borderRightWidth = 3f;
            m_Face.style.borderTopColor = new Color(0.34f, 0.36f, 0.39f, 1f);
            m_Face.style.borderBottomColor = new Color(0.34f, 0.36f, 0.39f, 1f);
            m_Face.style.borderLeftColor = new Color(0.34f, 0.36f, 0.39f, 1f);
            m_Face.style.borderRightColor = new Color(0.34f, 0.36f, 0.39f, 1f);
            Add(m_Face);
            
            // Inner face (white dial background)
            m_InnerFace = new VisualElement();
            m_InnerFace.AddToClassList(USS_INNER_FACE);
            m_InnerFace.style.position = Position.Absolute;
            m_InnerFace.style.left = new Length(5f, LengthUnit.Percent);
            m_InnerFace.style.top = new Length(5f, LengthUnit.Percent);
            m_InnerFace.style.width = new Length(90f, LengthUnit.Percent);
            m_InnerFace.style.height = new Length(90f, LengthUnit.Percent);
            m_InnerFace.style.borderTopLeftRadius = 999f;
            m_InnerFace.style.borderTopRightRadius = 999f;
            m_InnerFace.style.borderBottomLeftRadius = 999f;
            m_InnerFace.style.borderBottomRightRadius = 999f;
            m_InnerFace.style.backgroundColor = new Color(0.93f, 0.94f, 0.91f, 1f);
            m_InnerFace.style.borderTopWidth = 1f;
            m_InnerFace.style.borderBottomWidth = 1f;
            m_InnerFace.style.borderLeftWidth = 1f;
            m_InnerFace.style.borderRightWidth = 1f;
            m_InnerFace.style.borderTopColor = new Color(0.28f, 0.30f, 0.32f, 1f);
            m_InnerFace.style.borderBottomColor = new Color(0.28f, 0.30f, 0.32f, 1f);
            m_InnerFace.style.borderLeftColor = new Color(0.28f, 0.30f, 0.32f, 1f);
            m_InnerFace.style.borderRightColor = new Color(0.28f, 0.30f, 0.32f, 1f);
            Add(m_InnerFace);
            
            // Scale container (for tick marks and labels)
            m_ScaleContainer = new VisualElement();
            m_ScaleContainer.style.position = Position.Absolute;
            m_ScaleContainer.style.width = Length.Percent(100);
            m_ScaleContainer.style.height = Length.Percent(100);
            Add(m_ScaleContainer);
            
            // Title label
            m_TitleLabel = new Label(m_Title);
            m_TitleLabel.AddToClassList(USS_TITLE);
            m_TitleLabel.style.position = Position.Absolute;
            m_TitleLabel.style.top = new Length(29f, LengthUnit.Percent);
            m_TitleLabel.style.left = 0f;
            m_TitleLabel.style.right = 0f;
            m_TitleLabel.style.fontSize = 8f;
            m_TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_TitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_TitleLabel.style.color = new Color(0.20f, 0.22f, 0.24f, 1f);
            Add(m_TitleLabel);
            
            // Unit label
            m_UnitLabel = new Label(m_Unit);
            m_UnitLabel.AddToClassList(USS_UNIT_LABEL);
            m_UnitLabel.style.position = Position.Absolute;
            m_UnitLabel.style.bottom = new Length(24f, LengthUnit.Percent);
            m_UnitLabel.style.left = 0f;
            m_UnitLabel.style.right = 0f;
            m_UnitLabel.style.fontSize = 10f;
            m_UnitLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_UnitLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_UnitLabel.style.color = new Color(0.10f, 0.11f, 0.13f, 1f);
            Add(m_UnitLabel);
            
            // Needle container (for rotation)
            m_NeedleContainer = new VisualElement();
            m_NeedleContainer.AddToClassList(USS_NEEDLE_CONTAINER);
            m_NeedleContainer.style.position = Position.Absolute;
            m_NeedleContainer.style.width = new Length(100f, LengthUnit.Percent);
            m_NeedleContainer.style.height = new Length(100f, LengthUnit.Percent);
            m_NeedleContainer.style.alignItems = Align.Center;
            m_NeedleContainer.style.justifyContent = Justify.Center;
            Add(m_NeedleContainer);
            
            // Needle
            m_Needle = new VisualElement();
            m_Needle.AddToClassList(USS_NEEDLE);
            m_Needle.style.position = Position.Absolute;
            m_Needle.style.width = 4f;
            m_Needle.style.height = new Length(35f, LengthUnit.Percent);
            m_Needle.style.bottom = new Length(50f, LengthUnit.Percent);
            m_Needle.style.backgroundColor = new Color(0.78f, 0.16f, 0.14f, 1f);
            m_Needle.style.borderTopLeftRadius = 2f;
            m_Needle.style.borderTopRightRadius = 2f;
            m_Needle.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            m_NeedleContainer.Add(m_Needle);
            
            // Center hub (covers needle pivot)
            m_Hub = new VisualElement();
            m_Hub.AddToClassList(USS_HUB);
            m_Hub.style.position = Position.Absolute;
            m_Hub.style.width = 14f;
            m_Hub.style.height = 14f;
            m_Hub.style.left = new Length(50f, LengthUnit.Percent);
            m_Hub.style.top = new Length(50f, LengthUnit.Percent);
            m_Hub.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            m_Hub.style.borderTopLeftRadius = 999f;
            m_Hub.style.borderTopRightRadius = 999f;
            m_Hub.style.borderBottomLeftRadius = 999f;
            m_Hub.style.borderBottomRightRadius = 999f;
            m_Hub.style.backgroundColor = new Color(0.18f, 0.19f, 0.20f, 1f);
            m_Hub.style.borderTopWidth = 1f;
            m_Hub.style.borderBottomWidth = 1f;
            m_Hub.style.borderLeftWidth = 1f;
            m_Hub.style.borderRightWidth = 1f;
            m_Hub.style.borderTopColor = new Color(0.38f, 0.40f, 0.42f, 1f);
            m_Hub.style.borderBottomColor = new Color(0.38f, 0.40f, 0.42f, 1f);
            m_Hub.style.borderLeftColor = new Color(0.38f, 0.40f, 0.42f, 1f);
            m_Hub.style.borderRightColor = new Color(0.38f, 0.40f, 0.42f, 1f);
            Add(m_Hub);
        }
        
        // ====================================================================
        // SCALE GENERATION
        // ====================================================================
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            RegenerateScale();
            UpdateNeedleRotation();
        }
        
        private void RegenerateScale()
        {
            if (m_ScaleContainer == null) return;
            
            // Clear existing scale elements
            m_ScaleContainer.Clear();
            
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;
            
            if (float.IsNaN(width) || width <= 0) return;
            
            float centerX = width / 2f;
            float centerY = height / 2f;
            float radius = Mathf.Min(width, height) / 2f * 0.75f; // Scale at 75% of radius
            float labelRadius = radius * 0.72f; // Labels slightly inside ticks
            
            float range = m_MaxValue - m_MinValue;
            float angleRange = m_EndAngle - m_StartAngle;
            
            if (range <= 0 || m_MajorTickInterval <= 0) return;
            
            // Generate major ticks and labels
            int majorTickCount = Mathf.FloorToInt(range / m_MajorTickInterval) + 1;
            
            for (int i = 0; i < majorTickCount; i++)
            {
                float tickValue = m_MinValue + (i * m_MajorTickInterval);
                if (tickValue > m_MaxValue) break;
                
                float normalizedValue = (tickValue - m_MinValue) / range;
                float angle = m_StartAngle + (normalizedValue * angleRange);
                float radians = angle * Mathf.Deg2Rad;
                
                // Create major tick using generateTickMark helper
                CreateTickMark(centerX, centerY, radius, radians, true);
                
                // Create label
                CreateScaleLabel(centerX, centerY, labelRadius, radians, tickValue);
                
                // Generate minor ticks between this and next major tick
                if (i < majorTickCount - 1 && m_MinorTicksPerMajor > 0)
                {
                    float minorInterval = m_MajorTickInterval / (m_MinorTicksPerMajor + 1);
                    for (int j = 1; j <= m_MinorTicksPerMajor; j++)
                    {
                        float minorValue = tickValue + (j * minorInterval);
                        if (minorValue >= m_MaxValue) break;
                        
                        float minorNormalized = (minorValue - m_MinValue) / range;
                        float minorAngle = m_StartAngle + (minorNormalized * angleRange);
                        float minorRadians = minorAngle * Mathf.Deg2Rad;
                        
                        CreateTickMark(centerX, centerY, radius, minorRadians, false);
                    }
                }
            }
        }
        
        private void CreateTickMark(float centerX, float centerY, float radius, float radians, bool isMajor)
        {
            var tick = new VisualElement();
            
            float tickLength = isMajor ? 12f : 8f;
            float tickWidth = isMajor ? 2f : 1f;
            
            // Calculate tick position
            float outerX = centerX + Mathf.Sin(radians) * radius;
            float outerY = centerY - Mathf.Cos(radians) * radius;
            
            tick.style.position = Position.Absolute;
            tick.style.width = tickWidth;
            tick.style.height = tickLength;
            tick.style.backgroundColor = isMajor ? new Color(0.08f, 0.08f, 0.08f) : new Color(0.24f, 0.24f, 0.24f);
            tick.style.left = outerX - tickWidth / 2f;
            tick.style.top = outerY;
            tick.style.transformOrigin = new TransformOrigin(Length.Percent(50), 0);
            tick.style.rotate = new Rotate(Angle.Degrees(radians * Mathf.Rad2Deg));
            
            m_ScaleContainer.Add(tick);
        }
        
        private void CreateScaleLabel(float centerX, float centerY, float radius, float radians, float value)
        {
            var label = new Label();
            label.AddToClassList(USS_SCALE_LABEL);
            
            // Format value (no decimals for clean appearance)
            label.text = Mathf.RoundToInt(value).ToString();
            
            // Position label
            float labelX = centerX + Mathf.Sin(radians) * radius;
            float labelY = centerY - Mathf.Cos(radians) * radius;
            
            label.style.position = Position.Absolute;
            label.style.left = labelX;
            label.style.top = labelY;
            label.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            
            m_ScaleContainer.Add(label);
        }
        
        // ====================================================================
        // NEEDLE ROTATION
        // ====================================================================
        
        private void UpdateNeedleRotation()
        {
            if (m_Needle == null) return;
            
            float range = m_MaxValue - m_MinValue;
            if (range <= 0) return;
            
            float normalizedValue = (m_Value - m_MinValue) / range;
            float angleRange = m_EndAngle - m_StartAngle;
            float needleAngle = m_StartAngle + (normalizedValue * angleRange);
            
            m_Needle.style.rotate = new Rotate(Angle.Degrees(needleAngle));
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Sets the value with optional animation.
        /// </summary>
        /// <param name="newValue">Target value</param>
        /// <param name="animate">Whether to animate the transition (future feature)</param>
        public void SetValue(float newValue, bool animate = false)
        {
            // TODO: Implement smooth animation for needle movement
            Value = newValue;
        }
    }
}
