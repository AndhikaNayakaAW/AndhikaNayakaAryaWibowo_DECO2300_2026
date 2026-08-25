# Architecture

```text
Controller ray + trigger ─┐
Hand pinch ────────────────┼──> WhiteboardDrawer ───> WhiteboardCanvas texture
Whiteboard world UI ──────┘             │
                                       ▼
                              XRStudyWhiteboardManager
                                │       │        │
                              tool    colour   clear confirmation

Table `TOOLS` button ──────> StudyTableToolMenu ──> PaperTool selection
                                                        │
                                                        ▼
                                              PaperNoteCanvas texture
```

## Drawing

`XRWhiteboardInteractor` reads the right controller trigger and casts a focused ray against the board collider. `HandWhiteboardInteractor` uses the tracked right index fingertip, thumb fingertip, and pinch distance as a secondary input path.

Both paths send UV points to `WhiteboardDrawer`. The drawer starts and ends strokes and delegates marker/eraser behaviour to `WhiteboardCanvas`.

`WhiteboardCanvas` owns a runtime `Texture2D`. It stamps circular brush pixels and interpolates points between successive UV samples so movement does not produce isolated dots. The marker and eraser sizes are serialized settings in the script.

## State and UI

`XRStudyWhiteboardManager` is the single source of truth for the current tool, current colour, and clear-board flow. UI buttons call the manager; the status display and active outlines refresh from manager state.

The clear overlay is a separate world-space UI object. It is only shown after `Clear Board` is selected, so the destructive action requires a second intentional selection.

Each detected tabletop creates a `PaperNoteCanvas`, a `StudyTableToolMenu`, and a `StudyTableTeleportPoint` at runtime. Desk pencil and eraser meshes are not created. The menu changes the selected paper tool, clears only its associated paper, and is reachable through the existing XR UI ray.

`StudyTableTeleportPoint` gives the editor table shortcuts a paper-facing target. On Quest, the same locations remain reachable through the floor `TeleportationArea`; the point's view pitch aims down at the paper without losing the board from view.

## XR infrastructure

The scene builder reuses the existing VR template complete hands XR Origin, its action asset, locomotion, controller rays, hand interaction support, and hands permission manager. It creates one XRI Interaction Manager and one XR UI Input Module for this scene. No networking or second XR Origin is introduced.
