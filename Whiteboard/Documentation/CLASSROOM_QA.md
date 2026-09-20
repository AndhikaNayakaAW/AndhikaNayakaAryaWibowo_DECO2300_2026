# Classroom navigation, drawing and session QA

Verified in Unity 6000.3.21f1, macOS Game view with the XR Device Simulator. The existing Android/OpenXR setup and classroom were retained. No Quest headset was attached; physical tracking, reach and haptics still need a headset check.

## Changes and causes

- Table navigation previously placed the rig 1.4 m behind the paper and ignored the tracked camera offset. Named SeatAnchors now describe head poses derived from the actual desk bounds. Navigation rotates around the camera in yaw and translates the rig by the remaining camera-to-anchor delta, preserving tracked local pose and rig scale.
- The desktop seated view uses a 25-degree downward view and wider field of view so the whole paper and teacher's board are visible together. Headset pitch and field of view remain headset controlled.
- WASD previously moved both the rig and the simulator's controller offsets. Runtime input ownership prevents duplicate keyboard/thumbstick motion. Space or left Shift explicitly selects controller posing; ordinary mouse drawing does not pose controllers. Official sample action assets are unchanged.
- T selects the left simulated controller and Y selects the right. In either mode the mouse aims that controller, left click is its trigger, G is grab, and Escape returns to normal cursor drawing. Repeating T or Y keeps the requested controller active instead of toggling it off.
- Navigation had two click handlers. The manual handler consumed MouseUp before GUI.Button released its mouse capture, swallowing the first subsequent drawing drag. The duplicate handler was removed. First strokes immediately after table and whiteboard navigation were retested.
- Paper contact mapped local +Z to increasing V, but its displayed Quad mapped V toward -Z. Correcting the Quad orientation aligns the visible texture with contact coordinates. Whiteboard Cube-face UVs were verified separately; they did not need the paper's axis correction.
- Strokes end on release, surface exit and tool changes. Trusted desktop endpoints are completed while tracked discontinuities remain rejected. Each board owns its material and texture.
- Desk controls sit beside the reachable paper, angled toward the seat, with clear instructions and active-tool text. Only the nearest desk shows its dock. Board tools and Record/Stop/Play use the same charcoal/cyan style, readable labels and vector icons, beside the writable surface.
- The obsolete black GrabMarker and ToolTray were removed from the saved scene and scene builder. Drawing continues through the existing controller-ray contact workflow; a physical marker is not required. Desk objects retain their existing grab components.

## Regression checks

Use Tools > XR Study Whiteboard:

| Check | Result / coverage |
| --- | --- |
| Validate Classroom Setup | 1,055 checks passed after prop removal; scene references, XR configuration, drawing, desk controls and recording |
| Validate Camera Anchor Alignment | Nested tracked head offsets, scaled/translated parent, five destinations, yaw, unchanged local head pose, unchanged rig scale, repeat alignment |
| Validate Drawing and Recording | 23 checks: displayed mesh UVs, material isolation, stroke boundaries, fast final endpoints, replay pixels, timing, clear, colour/tool selection, playback gating and restart |
| Validate Desktop Simulator Input Ownership (Play Mode) | Keyboard movement ownership, mouse/scroll gating, explicit controller posing, trigger press/release, original action assets unchanged |
| Validate Controller Selection (Play Mode) | T/left, Y/right, repeated selection, exclusive hand ownership, mouse/trigger routing and Escape restoration |
| Validate Study Controls | Icon renderer/geometry, hover, one click per press, release/repress and disabled controls |
| Build Android Validation APK | Succeeded with strict mode; output `/tmp/XRStudyWhiteboard-validation.apk` |

Observed red-to-green cases included shared-material isolation and two final-stroke endpoint cases, followed by all 23 drawing/session checks passing. The obsolete-marker and holder checks were added and failed against the old scene before removal. The first post-navigation drag was a visual regression: it produced only a dot before the duplicate click handler was removed, then a complete horizontal stroke afterward. Commits keep related fixes with their regression checks; they do not fabricate a test-first history for work inherited from the interrupted session.

## Game-view interaction results

Native desktop mouse/keyboard events drove the live Game view; test scripts did not call drawing methods to create the visual QA shapes.

- Tables 1, 4, 8, 12 and 16: seated camera, paper, nearby drawing dock and teacher's board visually inspected. Whiteboard, Center View and Stand/Jump navigation checked.
- Paper: horizontal, vertical, closed circle and L passed; partial erasing left nearby strokes intact; Clear Paper removed ink.
- Whiteboard: horizontal, vertical, closed circle, L and fast stroke passed. Black horizontal, red vertical, blue circle and green L were visibly correct. Partial erase and confirmed clear passed; drawing after clear started independently.
- Recording: Record, Stop and Play used through the visible controls. The complete coloured four-shape sequence, small erasure and two-second pause were watched from the empty starting board through normal completion. Final replay pixels matched the recorded board, and event count did not grow during playback. A second recording containing a clear and new stroke also replayed successfully.
- Movement: a live W-key test moved camera and both controllers by the same 1.08 m with unchanged relative offsets. Explicit simulator controller rotation was exercised; posing alone left a clean board unchanged in a fresh Play session.
- Controller selection: live T/mouse movement changed only the left controller, then Y/mouse movement changed only the right controller. The camera stayed fixed and the clean board hash stayed unchanged. Escape restored cursor mode.
- Layout: desk dock stays off the paper; board controls stay beside the board; navigation sits above the collapsed simulator footer. The black marker and holder no longer obstruct the front of the board.

## Scope and cleanup

Recording stores one starting pixel buffer plus timestamped stroke/tool/colour/clear events in memory and reuses the existing drawing brush during playback. It is not screen video and does not persist across application restarts. Microphone/audio recording was not added because reliable capture and synchronized playback could not be verified here.

Simulator-only missing eye-tracking/haptic support messages remain. Unity also reports an OpenXR settings importer consistency message; XR settings were not modified to suppress it. Temporary QA scripts, captures and recordings are excluded from commits. Play Mode notes are not saved into the scene. The pre-existing edit in Documentation/AI_USE.md is left untouched.
