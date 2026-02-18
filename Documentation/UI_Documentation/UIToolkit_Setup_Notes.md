# Unity 6.3 UI Toolkit — Important Setup Notes

**Project:** Critical: Master the Atom  
**Date:** February 18, 2026

---

## UIDocument Requires a Source Asset

**Key Discovery:** `UIDocument.rootVisualElement` is not properly initialized until a Source Asset (UXML) is assigned. Without it:

- `rootVisualElement` may be null
- `generateVisualContent` callbacks don't fire
- Programmatic UI additions fail silently

### Solution

Always assign a UXML to Source Asset, even for fully programmatic UIs. Use `Bootstrap.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="root" style="flex-grow: 1;" />
</ui:UXML>
```

The controller script calls `root.Clear()` and builds UI from scratch — the UXML just bootstraps the visual tree.

---

## Panel Settings Asset

**Location:** `Assets > Create > UI Toolkit > Panel Settings Asset`

**Recommended Settings for Dashboard:**
- **Scale Mode:** Scale With Screen Size
- **Reference Resolution:** 1920 × 1080
- **Match:** 0.5

---

## UIDocument Setup Checklist

1. ✅ Create Panel Settings asset
2. ✅ Create Bootstrap.uxml (or any minimal UXML)
3. ✅ GameObject > UI Toolkit > UI Document
4. ✅ Assign Panel Settings to UIDocument
5. ✅ Assign Bootstrap.uxml to Source Asset
6. ✅ Add your controller script (e.g., ExtendedPOCController)
7. ✅ Enter Play Mode

---

## File Locations

```
Assets/
├── UI/
│   └── UIToolkit/
│       └── POC/
│           └── Bootstrap.uxml          <- Minimal bootstrap UXML
├── Scripts/
│   └── UI/
│       └── UIToolkit/
│           └── POC/
│               ├── ArcGaugePOC.cs       <- Arc gauge element
│               ├── StripChartPOC.cs     <- Strip chart element
│               ├── ArcGaugePOCController.cs    <- Single gauge test
│               └── ExtendedPOCController.cs    <- Full dashboard test
```

---

## POC Results (February 18, 2026)

| Test | Result |
|------|--------|
| Arc gauge renders correctly | ✅ PASS |
| Multiple arc gauges (4) | ✅ PASS |
| Strip chart line rendering | ✅ PASS |
| Multiple traces per chart | ✅ PASS |
| 5 Hz update rate | ✅ PASS |
| Performance acceptable | ✅ PASS |

**Conclusion:** UI Toolkit is viable for the Validation Dashboard rebuild.
