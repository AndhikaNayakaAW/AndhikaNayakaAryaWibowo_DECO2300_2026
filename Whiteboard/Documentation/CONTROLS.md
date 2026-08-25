# XR Study Whiteboard — Controls

## Meta Quest controllers

| Input | Action |
| --- | --- |
| Right trigger (hold) | Draw on the whiteboard; select a world-space UI control when pointing at it |
| Right grip | Grab the whiteboard marker from its tray |
| Left thumbstick | Move when the existing locomotion configuration enables continuous movement |
| Configured teleport action | Teleport to the classroom floor and move to any table |
| Right thumbstick | Snap turn using the reused XRI locomotion setup |

Marker mode starts with black selected. The four available colours are black, red, blue, and green. The eraser is selected from the UI and uses a larger brush.

## Student table paper

The table contains the paper only. Point at the `TOOLS` button beside the paper and press the right trigger to open its floating menu:

- `PENCIL` selects the fine paper-writing line.
- `ERASER` selects a wider paper eraser.
- `CLEAR PAPER` removes only that table's notes.

The selected paper tool is shared by the table menus, so the same right trigger can write on whichever paper is being pointed at.

## Hands

| Gesture | Action |
| --- | --- |
| Right index fingertip + thumb pinch | Start and continue a stroke when the hand is tracked |
| Release pinch | Stop the stroke |
| Point / poke / pinch select | Select colour, Marker, Eraser, and Clear Board controls through the existing hand interaction setup |

The experimental two-finger swipe from the paper prototype is not enabled. `HandToolSwitchGesture.cs` is the documented extension point; direct Marker/Eraser buttons are the reliable fallback.
