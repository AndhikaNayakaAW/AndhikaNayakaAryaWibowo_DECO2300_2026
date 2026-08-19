# XR Study Whiteboard — Controls

## Meta Quest controllers

| Input | Action |
| --- | --- |
| Right trigger (hold) | Draw on the whiteboard; select a world-space UI control when pointing at it |
| Right grip | Grab the physical marker |
| Left thumbstick | Move when the existing locomotion configuration enables continuous movement |
| Configured teleport action | Teleport to the classroom floor |
| Right thumbstick | Snap turn using the reused XRI locomotion setup |

Marker mode starts with black selected. The four available colours are black, red, blue, and green. The eraser is selected from the UI and uses a larger brush.

## Hands

| Gesture | Action |
| --- | --- |
| Right index fingertip + thumb pinch | Start and continue a stroke when the hand is tracked |
| Release pinch | Stop the stroke |
| Point / poke / pinch select | Select colour, Marker, Eraser, and Clear Board controls through the existing hand interaction setup |

The experimental two-finger swipe from the paper prototype is not enabled. `HandToolSwitchGesture.cs` is the documented extension point; direct Marker/Eraser buttons are the reliable fallback.
