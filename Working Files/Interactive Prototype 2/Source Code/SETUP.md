# XR Study Whiteboard — Setup

This is the existing Unity project named `Whiteboard`. It uses Unity `6000.3.21f1` (Unity 6.3 LTS), URP, OpenXR, XR Interaction Toolkit, XR Hands, and the Input System already included in the project.

## Main scene

Open:

`Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity`

It is also the only enabled scene in the build settings.

## Build or repair the classroom

If the scene is missing or references need to be rebuilt:

1. Open the `Whiteboard` project in Unity `6000.3.21f1`.
2. Wait for scripts to compile.
3. Choose `Tools > XR Study Whiteboard > Build - Repair Classroom`.
4. Open the main scene when the operation finishes.
5. Save the scene if Unity asks.

The builder is idempotent at the scene level: it creates a fresh generated classroom scene and does not modify package samples or remove XR infrastructure.

## Start

Use Play Mode with an XR simulator if available, or connect a Quest and use `Build Settings > Android > Build And Run`.

The player starts inside the room, facing the whiteboard from several metres away. The board is blank at startup.

## Expected configuration

- URP is preserved; do not switch render pipelines.
- OpenXR and the existing Meta/Android XR packages are preserved.
- The reused template XR Origin is the only active player rig.
- The floor is a teleportation area.
- Controller input is the primary interaction path; hands are optional.

If a scene reference is missing after a package reimport, run the builder again rather than editing package samples.
