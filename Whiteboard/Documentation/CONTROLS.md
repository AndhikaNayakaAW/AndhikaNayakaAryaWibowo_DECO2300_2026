# XR Study Whiteboard — Controls

## Meta Quest controllers

| Input | Action |
| --- | --- |
| Right trigger (hold) | Draw on the whiteboard or paper; select a world-space UI control when pointing at it |
| Left thumbstick | Move through the classroom |
| Right thumbstick | Snap turn |
| X | Go directly to the whiteboard |
| Y | Go to the next student table |
| A | Return to the centre view |
| B | Stand up from a seated table view |
| Teleport ray | Aim at the classroom floor and use the configured XRI teleport action |

Marker mode starts with black selected. The four available colours are black, red, blue, and green. The eraser is selected from the UI and uses a larger brush.

## Student table paper

The nearest table presents its drawing controls beside the paper. Point with the right controller and press the right trigger:

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
