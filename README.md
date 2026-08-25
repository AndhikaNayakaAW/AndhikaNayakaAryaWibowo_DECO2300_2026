# XR Study Whiteboard

> A gesture-driven virtual whiteboard for studying, revision, brainstorming, and note-taking in XR.

<p align="center">
  <img src="DesignProcess/Week02_PaperPrototype/Images/lowfi-1.jpeg" alt="XR Study Whiteboard paper demo" width="560">
</p>

<p align="center"><em>Week 2 · Completed interaction demo</em></p>

## Project snapshot

| | |
| --- | --- |
| **Course** | DECO2300 – Digital Prototyping and Extended Reality |
| **Student** | Andhika Nayaka Arya Wibowo |
| **Concept** | XR Study Whiteboard |
| **Demo outcome** | Successful completion with no observed mistakes |
| **Testing methods** | Wizard-of-Oz + Think-Aloud Protocol |

The XR Study Whiteboard gives students a focused virtual study space where they can write notes with simple hand gestures while keeping the atmosphere of a classroom.

## Documentation

- [Week 2 — Paper demo, testing results, and reflection](DesignProcess/Week02_PaperPrototype/README.md)

## Digital Prototype

The Unity prototype is implemented in `Whiteboard` using Unity `6000.3.21f1` with URP, OpenXR, XR Interaction Toolkit, XR Hands, and the Input System.

Current features include:

- Continuous controller and hand pinch drawing on the whiteboard, with interpolated marker strokes and a wider eraser.
- Black, red, blue, and green marker colours with visible tool/status feedback.
- Clear-board confirmation.
- Student tables with paper-only surfaces. Each table has a `TOOLS` button that opens floating `PENCIL`, `ERASER`, and `CLEAR PAPER` actions. The paper pencil line is intentionally finer than the whiteboard marker.
- Table teleport/view shortcuts that focus the paper while keeping the whiteboard visible, plus floor teleportation and snap turning.
- Editor testing through the XR Device Simulator and desktop fallback controls.

The original paper-prototyping process is preserved under `DesignProcess/`. Start with the [digital prototype setup guide](Whiteboard/Documentation/SETUP.md).

## Key outcome

Participants completed the writing, colour selection, erasing, tool switching, and clear-board interactions successfully. They were positive about being able to study in different locations while still feeling like they were in a class environment.

## Digital XR Implementation

The existing `Whiteboard` Unity project now contains the main scene at [`Whiteboard/Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity`](Whiteboard/Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity). It includes a lightweight classroom, a runtime whiteboard with black/red/blue/green marker colours, Marker/Eraser modes, clear-board confirmation, controller drawing, optional pinch drawing, teleportation, snap turning, and a grabbable marker.

The implementation reuses the existing VR template XR Origin, OpenXR, XRI Starter Assets, XRI input actions, locomotion, and XR Hands infrastructure. See [`Whiteboard/Documentation/SETUP.md`](Whiteboard/Documentation/SETUP.md) for setup, [`Whiteboard/Documentation/CONTROLS.md`](Whiteboard/Documentation/CONTROLS.md) for controls, and [`Whiteboard/Documentation/IMPLEMENTATION_STATUS.md`](Whiteboard/Documentation/IMPLEMENTATION_STATUS.md) for testing status and limitations.
