# Implementation Plan v5.1.0 â€” Rotary Selector Switch (2D UI)

**CRITICAL: Master the Atom**  
Westinghouse 4-Loop PWR Simulator  
Pressurizer Heater Mode Control

**Document Version:** 2.0  
**Target Version:** 5.1.0  
**Date:** 2026-02-18  
**Classification:** UI / 2D Canvas Component

---

## Table of Contents

1. [Problem Summary](#1-problem-summary)
2. [Expectations](#2-expectations)
3. [Technical Design](#3-technical-design)
4. [Implementation Stages](#4-implementation-stages)
5. [Unaddressed Issues](#5-unaddressed-issues)
6. [Files Created/Modified](#6-files-createdmodified)
7. [Validation Criteria](#7-validation-criteria)

---

## 1. Problem Summary

The Pressurizer screen currently lacks a dedicated rotary mode control for PZR heaters. Users need an intuitive, realistic way to select heater operating mode from the Pressurizer Operator Screen (Key 3).

**Current State:**
- No dedicated UI rotary mode control for PZR heater operation
- Heater behavior follows existing simulation logic without an explicit OFF/AUTO selector surface
- Missing authentic control room selector aesthetic for heater mode management

**Desired State:**
- 3-position industrial rotary selector switch (OFF â†’ AUTO â†’ MANUAL)
- 2D sprite-based implementation matching existing Canvas UI
- Animated rotation on click
- Placed on the Pressurizer Screen where heatup procedures initiate
- Scalable via RectTransform
- MANUAL position disabled/greyed for future expansion

---

## 2. Expectations

### 2.1 Visual Appearance

The switch replicates an authentic industrial 3-position selector switch rendered as 2D sprites:

| Element | Description |
|---------|-------------|
| **Base plate** | Dark grey circular plate with engraved position labels |
| **Position labels** | "OFF" at 7 o'clock, "AUTO" at 12 o'clock, "MANUAL" at 5 o'clock |
| **Knob** | Black circular knob with white/chrome indicator line |
| **Indicator line** | Points to current position, rotates with knob |
| **Bezel** | Subtle chrome ring around edge (optional, baked into base sprite) |

**Colour Palette (matching existing UI):**
| Element | Hex | RGB |
|---------|-----|-----|
| Base plate | #2A2A35 | 42, 42, 53 |
| Knob body | #1A1A1F | 26, 26, 31 |
| Indicator line | #C8D0D8 | 200, 208, 216 |
| Label (active) | #C8D0D8 | 200, 208, 216 |
| Label (disabled) | #505060 | 80, 80, 96 |
| Bezel highlight | #3A3A45 | 58, 58, 69 |

### 2.2 Positions

| Position | Knob Rotation | Label Location | Function |
|----------|---------------|----------------|----------|
| OFF | -45° | 7 o'clock | PZR heaters forced off (simulation continues) |
| AUTO | 0° | 12 o'clock | PZR heaters operate under existing automatic simulation logic |
| MANUAL | +45Â° | 5 o'clock | Reserved (disabled, grey label) |

### 2.3 Interaction

- **Left Click:** Rotate clockwise (OFF â†’ AUTO â†’ MANUAL)
- **Right Click:** Rotate counter-clockwise (MANUAL â†’ AUTO â†’ OFF)
- **Disabled position handling:** When MANUAL is disabled, left-click from AUTO does nothing; right-click from OFF does nothing
- **Audio feedback:** Optional click sound on position change (future)
- **Visual feedback:** Smooth animated rotation (~0.15 seconds)

**Interaction Diagram:**
```
            AUTO (12 o'clock)
              â–²
    L-Click â”Œâ”€â”´â”€â” R-Click
            â”‚   â”‚
            â–¼   â–¼
OFF â—„â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º MANUAL
(7 o'clock)    (5 o'clock)
     R-Click â—„â”€â”€â”€ L-Click
```

### 2.4 Scalability

- RectTransform-based sizing
- Works at any reasonable UI size (50px to 200px diameter)
- Crisp rendering via vector-like sprite or procedural generation

---

## 3. Technical Design

### 3.1 Architecture Overview

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                     UNITY CANVAS UI                             â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ RotarySelectorSwitch (GameObject)                        â”‚   â”‚
â”‚  â”‚  â”œâ”€ RectTransform (size: configurable)                  â”‚   â”‚
â”‚  â”‚  â”œâ”€ RotarySelectorSwitch.cs (main script)               â”‚   â”‚
â”‚  â”‚  â”‚                                                       â”‚   â”‚
â”‚  â”‚  â””â”€ Children:                                            â”‚   â”‚
â”‚  â”‚      â”œâ”€ BasePlate (Image - static)                      â”‚   â”‚
â”‚  â”‚      â”‚   â””â”€ Labels rendered via TMP or baked in sprite  â”‚   â”‚
â”‚  â”‚      â”‚                                                   â”‚   â”‚
â”‚  â”‚      â””â”€ Knob (Image - rotates)                          â”‚   â”‚
â”‚  â”‚          â””â”€ RectTransform.localRotation animated        â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                            â”‚                                    â”‚
â”‚                            â–¼                                    â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ PressurizerScreen.cs Integration                        â”‚   â”‚
â”‚  â”‚  â””â”€ Subscribe to OnPositionChanged → Heater mode path
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 3.2 Sprite Approach Options

**Option A: Procedural Generation (Recommended)**
- Generate sprites at runtime using `Texture2D` and `Sprite.Create()`
- No external assets required
- Matches existing procedural UI patterns in the project
- Crisp at any resolution

**Option B: Pre-made PNG Assets**
- Create sprites in image editor
- Place in `Assets/Sprites/` or `Assets/Resources/Sprites/`
- Simpler code but requires asset management

**Recommendation:** Option A (procedural) for the base plate and knob, matching how other UI elements are created in the project.

### 3.3 Component Structure

```csharp
// ============================================================================
// RotarySelectorSwitch.cs - 3-Position Industrial Rotary Switch
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
using System.Collections;

namespace Critical.UI
{
    public enum SwitchPosition { Off, Auto, Manual }

    public class RotarySelectorSwitch : MonoBehaviour, IPointerClickHandler
    {
        #region Configuration
        [Header("State")]
        [SerializeField] private SwitchPosition _currentPosition = SwitchPosition.Off;
        [SerializeField] private bool _manualEnabled = false;

        [Header("Angles (degrees, Z-axis)")]
        [SerializeField] private float _angleOff = -45f;
        [SerializeField] private float _angleAuto = 0f;
        [SerializeField] private float _angleManual = 45f;

        [Header("Animation")]
        [SerializeField] private float _rotationDuration = 0.15f;

        [Header("References")]
        [SerializeField] private RectTransform _knobTransform;
        [SerializeField] private TextMeshProUGUI _labelOff;
        [SerializeField] private TextMeshProUGUI _labelAuto;
        [SerializeField] private TextMeshProUGUI _labelManual;

        [Header("Colors")]
        [SerializeField] private Color _labelActiveColor = new Color(0.784f, 0.816f, 0.847f);
        [SerializeField] private Color _labelDisabledColor = new Color(0.314f, 0.314f, 0.376f);
        #endregion

        #region Events
        [Header("Events")]
        public UnityEvent<SwitchPosition> OnPositionChanged;
        #endregion

        #region Properties
        public SwitchPosition CurrentPosition => _currentPosition;
        public bool ManualEnabled
        {
            get => _manualEnabled;
            set
            {
                _manualEnabled = value;
                UpdateLabelColors();
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            SetPositionImmediate(_currentPosition);
            UpdateLabelColors();
        }
        #endregion

        #region IPointerClickHandler
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                RotateClockwise();
            else if (eventData.button == PointerEventData.InputButton.Right)
                RotateCounterClockwise();
        }
        #endregion

        #region Rotation Logic
        private void RotateClockwise()
        {
            switch (_currentPosition)
            {
                case SwitchPosition.Off:
                    SetPosition(SwitchPosition.Auto);
                    break;
                case SwitchPosition.Auto:
                    if (_manualEnabled)
                        SetPosition(SwitchPosition.Manual);
                    break;
                case SwitchPosition.Manual:
                    // Already at max clockwise
                    break;
            }
        }

        private void RotateCounterClockwise()
        {
            switch (_currentPosition)
            {
                case SwitchPosition.Manual:
                    SetPosition(SwitchPosition.Auto);
                    break;
                case SwitchPosition.Auto:
                    SetPosition(SwitchPosition.Off);
                    break;
                case SwitchPosition.Off:
                    // Already at max counter-clockwise
                    break;
            }
        }

        public void SetPosition(SwitchPosition newPosition)
        {
            if (_currentPosition == newPosition) return;

            _currentPosition = newPosition;
            float targetAngle = GetAngleForPosition(newPosition);
            
            StopAllCoroutines();
            StartCoroutine(AnimateRotation(targetAngle));
            
            OnPositionChanged?.Invoke(_currentPosition);
        }

        private void SetPositionImmediate(SwitchPosition position)
        {
            _currentPosition = position;
            float angle = GetAngleForPosition(position);
            if (_knobTransform != null)
                _knobTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private float GetAngleForPosition(SwitchPosition position)
        {
            return position switch
            {
                SwitchPosition.Off => _angleOff,
                SwitchPosition.Auto => _angleAuto,
                SwitchPosition.Manual => _angleManual,
                _ => 0f
            };
        }
        #endregion

        #region Animation
        private IEnumerator AnimateRotation(float targetAngle)
        {
            if (_knobTransform == null) yield break;

            float startAngle = _knobTransform.localRotation.eulerAngles.z;
            
            // Normalize angles for shortest path
            if (startAngle > 180f) startAngle -= 360f;
            
            float elapsed = 0f;
            while (elapsed < _rotationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _rotationDuration);
                float angle = Mathf.Lerp(startAngle, targetAngle, t);
                _knobTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            
            _knobTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
        }
        #endregion

        #region Visual Updates
        private void UpdateLabelColors()
        {
            if (_labelOff != null)
                _labelOff.color = _labelActiveColor;
            if (_labelAuto != null)
                _labelAuto.color = _labelActiveColor;
            if (_labelManual != null)
                _labelManual.color = _manualEnabled ? _labelActiveColor : _labelDisabledColor;
        }
        #endregion
    }
}
```

### 3.4 Procedural Sprite Generation

```csharp
// ============================================================================
// RotarySwitchBuilder.cs - Editor Tool to Create Switch UI
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Critical.UI
{
    public static class RotarySwitchBuilder
    {
        private static readonly Color COLOR_BASE = new Color(0.165f, 0.165f, 0.208f);      // #2A2A35
        private static readonly Color COLOR_KNOB = new Color(0.102f, 0.102f, 0.122f);      // #1A1A1F
        private static readonly Color COLOR_INDICATOR = new Color(0.784f, 0.816f, 0.847f); // #C8D0D8
        private static readonly Color COLOR_BEZEL = new Color(0.227f, 0.227f, 0.271f);     // #3A3A45
        private static readonly Color COLOR_LABEL = new Color(0.784f, 0.816f, 0.847f);     // #C8D0D8
        private static readonly Color COLOR_LABEL_DIM = new Color(0.314f, 0.314f, 0.376f); // #505060

        [MenuItem("Critical/Create Rotary Selector Switch")]
        public static void CreateSwitch()
        {
            // Find or create canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[RotarySwitchBuilder] No Canvas found in scene.");
                return;
            }

            // Create root object
            GameObject root = new GameObject("RotarySelectorSwitch");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(100f, 100f);

            // Add main script
            RotarySelectorSwitch switchScript = root.AddComponent<RotarySelectorSwitch>();

            // Create base plate
            GameObject basePlate = CreateBasePlate(root.transform);

            // Create labels
            TextMeshProUGUI labelOff = CreateLabel(root.transform, "OFF", -45f, 42f);
            TextMeshProUGUI labelAuto = CreateLabel(root.transform, "AUTO", 0f, 42f);
            TextMeshProUGUI labelManual = CreateLabel(root.transform, "MANUAL", 45f, 42f);
            labelManual.color = COLOR_LABEL_DIM;

            // Create knob
            GameObject knob = CreateKnob(root.transform);

            // Wire references via SerializedObject
            SerializedObject so = new SerializedObject(switchScript);
            so.FindProperty("_knobTransform").objectReferenceValue = knob.GetComponent<RectTransform>();
            so.FindProperty("_labelOff").objectReferenceValue = labelOff;
            so.FindProperty("_labelAuto").objectReferenceValue = labelAuto;
            so.FindProperty("_labelManual").objectReferenceValue = labelManual;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = root;
            Debug.Log("[RotarySwitchBuilder] Rotary Selector Switch created.");
        }

        private static GameObject CreateBasePlate(Transform parent)
        {
            GameObject obj = new GameObject("BasePlate");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.sprite = CreateCircleSprite(128, COLOR_BASE, COLOR_BEZEL, 4);
            img.type = Image.Type.Simple;
            img.raycastTarget = true;

            return obj;
        }

        private static GameObject CreateKnob(Transform parent)
        {
            GameObject obj = new GameObject("Knob");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(50f, 50f);
            rect.anchoredPosition = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.sprite = CreateKnobSprite(64, COLOR_KNOB, COLOR_INDICATOR);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            return obj;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text, float angleDeg, float radius)
        {
            GameObject obj = new GameObject($"Label_{text}");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(40f, 15f);

            // Position based on angle (0Â° = top, clockwise positive)
            float angleRad = (90f - angleDeg) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;
            rect.anchoredPosition = new Vector2(x, y);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = COLOR_LABEL;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static Sprite CreateCircleSprite(int size, Color fillColor, Color borderColor, int borderWidth)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float outerRadius = size / 2f - 1f;
            float innerRadius = outerRadius - borderWidth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    
                    if (dist > outerRadius)
                        tex.SetPixel(x, y, Color.clear);
                    else if (dist > innerRadius)
                        tex.SetPixel(x, y, borderColor);
                    else
                        tex.SetPixel(x, y, fillColor);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateKnobSprite(int size, Color knobColor, Color indicatorColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        // Check if pixel is on the indicator line (pointing up)
                        bool isIndicator = (x >= center - 2 && x <= center + 2 && y >= center && y <= size - 4);
                        tex.SetPixel(x, y, isIndicator ? indicatorColor : knobColor);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
#endif
```

---

## 4. Implementation Stages

### Stage 1: Core Script Creation
**Deliverables:**
- `RotarySelectorSwitch.cs` â€” Main switch component with state machine and animation

**Tasks:**
1. Create script file in `Assets/Scripts/UI/`
2. Implement `SwitchPosition` enum
3. Implement `IPointerClickHandler` for left/right click detection
4. Implement rotation animation coroutine
5. Implement `OnPositionChanged` UnityEvent

**Validation:**
- Script compiles without errors
- Can be added to any GameObject

---

### Stage 2: Builder Script & Prefab Creation
**Deliverables:**
- `RotarySwitchBuilder.cs` â€” Editor menu tool to create switch hierarchy
- `RotarySelectorSwitch.prefab` â€” Ready-to-use prefab

**Tasks:**
1. Create builder script in `Assets/Scripts/UI/Editor/`
2. Implement procedural sprite generation for base plate and knob
3. Create menu item `Critical > Create Rotary Selector Switch`
4. Generate complete hierarchy with all references wired
5. Save as prefab in `Assets/Prefabs/UI/`

**Validation:**
- Menu item appears under Critical menu
- Executing creates complete switch hierarchy
- All references auto-wired

---

### Stage 3: Pressurizer Screen Integration
**Deliverables:**
- Modified `PressurizerScreen.cs` with switch reference
- Switch positioned on Pressurizer operator screen
- PZR heater mode mapping wired to switch state

**Tasks:**
1. Add switch prefab to PressurizerScreen hierarchy
2. Position in bottom panel (near Time Control or standalone)
3. Add reference field to `PressurizerScreen.cs`
4. Subscribe to `OnPositionChanged` event
5. Wire to heater mode authority: OFF = heaters forced off, AUTO = existing automatic heater behavior, MANUAL = reserved (disabled)

**Validation:**
- Switch visible on Pressurizer Screen (Key 3)
- Clicking changes heater mode state

---

### Stage 4: Documentation & Changelog
**Deliverables:**
- Changelog v5.1.0
- Any code comments/documentation updates

**Tasks:**
1. Create changelog entry
2. Update any relevant documentation
3. Final testing pass

---

## 5. Unaddressed Issues

| Issue | Reason | Future Version |
|-------|--------|----------------|
| Click sound effect | Out of scope for v5.1.0 | v5.2.0 |
| MANUAL position functionality | Requires explicit manual heater control authority surface | v6.0.0+ |
| Drag-to-rotate interaction | Click-to-advance sufficient for now | v5.3.0 |
| Visual "detent" feedback | Polish feature | v5.2.0 |
| Tooltip on disabled click | Nice-to-have | v5.2.0 |

---

## 6. Files Created/Modified

### New Files

| File | Location | Purpose |
|------|----------|---------|
| `RotarySelectorSwitch.cs` | `Assets/Scripts/UI/` | Main switch component |
| `RotarySwitchBuilder.cs` | `Assets/Scripts/UI/Editor/` | Editor creation tool |
| `RotarySelectorSwitch.prefab` | `Assets/Prefabs/UI/` | Ready-to-use prefab |

### Modified Files

| File | Changes |
|------|---------|
| `PressurizerScreen.cs` | Add switch reference, wire to heater mode authority |
| `PressurizerScreen.prefab` | Add switch to hierarchy |

---

## 7. Validation Criteria

### 7.1 Visual Validation
- [ ] Switch renders as dark circular base plate with visible bezel
- [ ] Knob is centered with visible indicator line
- [ ] Labels "OFF", "AUTO", "MANUAL" positioned at correct angles
- [ ] "MANUAL" label is greyed out (disabled appearance)
- [ ] Switch scales correctly when RectTransform resized

### 7.2 Functional Validation
- [ ] Left-click from OFF rotates knob to AUTO position
- [ ] Left-click from AUTO does nothing (MANUAL disabled)
- [ ] Right-click from AUTO rotates knob to OFF position
- [ ] Right-click from OFF does nothing (already at limit)
- [ ] Knob rotation animates smoothly (~0.15 seconds)
- [ ] `OnPositionChanged` event fires on each valid position change
- [ ] Setting `ManualEnabled = true` enables left-click from AUTO to MANUAL
- [ ] Setting `ManualEnabled = true` changes MANUAL label to active color

### 7.3 Integration Validation
- [ ] Switch visible on Pressurizer Screen (Key 3)
- [ ] Switch positioned appropriately in bottom panel
- [ ] OFF position: PZR heaters forced off while simulation continues running
- [ ] AUTO position: PZR heaters follow current automatic simulation behavior
- [ ] MANUAL position remains disabled in v5.1.0 scope
- [ ] No errors or warnings in Console during operation
- [ ] Switch state persists correctly during gameplay

---

**End of Implementation Plan v5.1.0 (Revised for 2D UI)**


