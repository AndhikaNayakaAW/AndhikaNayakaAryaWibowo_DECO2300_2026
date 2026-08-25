# Quest Build

## Project settings already present

- Android is configured as a project target.
- Android minimum SDK is `32`.
- Android target architecture is set to the ARM64 option in `ProjectSettings/ProjectSettings.asset`.
- Android graphics APIs are already explicitly configured by the existing project.
- OpenXR, Meta OpenXR, and Android XR OpenXR packages are already installed.

These settings were preserved rather than blindly replaced. Confirm the final values in the Unity UI before a headset build because Unity/OpenXR may expose platform-specific options through package settings.

## Build and Run

1. Open the project in exactly Unity `6000.3.21f1`.
2. Connect the Meta Quest 2 or Quest 3 and enable developer/USB access if required.
3. Open `File > Build Profiles` or `File > Build Settings`.
4. Select Android and confirm `XRStudyClassroom` is included and enabled.
5. Check `Project Settings > XR Plug-in Management` and confirm OpenXR is enabled for Android.
6. Check the OpenXR interaction profiles and Quest hand-tracking feature if the installed package version exposes them.
7. Build, or choose Build And Run.
8. Test controller tracking first. Controllers must work even when hand tracking is unavailable.
9. Test drawing, all four colours, Marker, Eraser, Clear Board confirmation, teleportation, snap turning, and the grabbable whiteboard marker.
10. Teleport to each student table, confirm the view faces its paper while the board remains visible, then use the table `TOOLS` menu to select Pencil, Eraser, and Clear Paper.
11. Test hand tracking separately, including pinch drawing and UI selection.

## Status

The project is prepared for Quest testing, but this environment did not run an Android build or connect to Quest hardware. Do not treat editor compilation as hardware validation.
