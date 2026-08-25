# Editor Testing

## Open and run

1. Open the existing `Whiteboard` project in Unity `6000.3.21f1`.
2. Open `Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity`.
3. Enter Play Mode.
4. Use the installed XRI Device Simulator if it is enabled in the project. The existing simulator settings are under `Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset`.

The exact keyboard mapping can vary with the installed XRI simulator version. Open the simulator help overlay or the XRI Device Simulator component to see the active mapping rather than relying on a custom duplicate input system.

## Functional test

1. Confirm that the simulated right controller ray is visible.
2. Point at the board and hold the right trigger/select input.
3. Draw a complete circle across the board. The stroke should be continuous rather than disconnected dots and should not visibly trail the pointer.
4. Select Red, Blue, and Green one at a time and draw a short stroke for each.
5. Select Eraser and erase over a stroke.
6. Select Marker again and verify that the selected colour remains available.
7. Select Clear Board, confirm the overlay appears, choose Cancel, then repeat and choose Clear.
8. Teleport to a different point on the floor and test snap turning.
9. Teleport to each visible table using the `TABLE` desktop shortcuts or the floor teleport area. Confirm the view is aimed at that table's paper and the whiteboard remains visible.
10. Point at a table's `TOOLS` button and select `PENCIL`, draw a circle on the paper, select `ERASER`, erase part of it, then use `CLEAR PAPER` and confirm the paper is blank.
11. Grab and release the whiteboard marker from its tray.

## What was validated without a headset

Unity `6000.3.21f1` compiled the custom runtime and Editor scripts successfully, and the scene builder generated the classroom in Unity batch mode. No headset or Quest hardware was available for this validation.
