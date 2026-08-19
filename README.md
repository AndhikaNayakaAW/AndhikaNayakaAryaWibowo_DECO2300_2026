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

## Key outcome

Participants completed the writing, colour selection, erasing, tool switching, and clear-board interactions successfully. They were positive about being able to study in different locations while still feeling like they were in a class environment.

## Digital XR Implementation

The existing `Whiteboard` Unity project now contains the main scene at [`Whiteboard/Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity`](Whiteboard/Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity). It includes a lightweight classroom, a runtime whiteboard with black/red/blue/green marker colours, Marker/Eraser modes, clear-board confirmation, controller drawing, optional pinch drawing, teleportation, snap turning, and a grabbable marker.

The implementation reuses the existing VR template XR Origin, OpenXR, XRI Starter Assets, XRI input actions, locomotion, and XR Hands infrastructure. See [`Whiteboard/Documentation/SETUP.md`](Whiteboard/Documentation/SETUP.md) for setup, [`Whiteboard/Documentation/CONTROLS.md`](Whiteboard/Documentation/CONTROLS.md) for controls, and [`Whiteboard/Documentation/IMPLEMENTATION_STATUS.md`](Whiteboard/Documentation/IMPLEMENTATION_STATUS.md) for testing status and limitations.
