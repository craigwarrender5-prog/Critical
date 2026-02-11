# Screen Prefabs — Save Instructions

## Overview

All operator screens are built programmatically by `MultiScreenBuilder.cs` via the Unity Editor menu:
**Critical > Create All Operator Screens**

The builder creates the full screen hierarchy under a single Canvas. After building, you may optionally save screens as prefabs for faster iteration.

## How to Save Screen Prefabs

1. Run menu: **Critical > Create All Operator Screens**
2. In the Hierarchy, expand `OperatorCanvas`
3. Drag each screen GameObject into this folder (`Assets/Prefabs/Screens/`):
   - `ReactorOperatorScreen`
   - `RCSLoopScreen`
   - `PlantOverviewScreen`
   - `PressurizerScreen`
   - `CVCSScreen`
   - `SteamGeneratorScreen`
   - `TurbineGeneratorScreen`
   - `SecondarySystemsScreen`
   - `AuxiliarySystemsScreen`
4. Unity will create `.prefab` files automatically

## When to Rebuild vs. Use Prefabs

**Rebuild (menu):** After any code changes to MultiScreenBuilder.cs or screen classes.
**Use prefabs:** For quick scene setup when no builder changes have been made.

## Important Notes

- The builder always checks for existing screens and skips duplicates
- Delete old screen hierarchy before rebuilding if layout changes are expected
- `ScreenInputActions` asset is auto-wired to `ScreenManager` during build
- All SerializedObject field references are set during build — prefabs preserve these
