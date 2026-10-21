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
6. Confirm `Initialize XR on Startup` is enabled for Android. Without it, the APK opens without controller tracking or XR input.
7. Check the OpenXR interaction profiles and Quest hand-tracking feature if the installed package version exposes them.
8. Build, or choose Build And Run.
9. Test controller tracking first. Controllers must work even when hand tracking is unavailable.
10. Test drawing, all four colours, Marker, Eraser, Clear Board confirmation, teleportation, and snap turning.
11. Visit each student table, confirm the view faces its paper while the board remains visible, then select Pencil, Eraser, and Clear Paper.
12. Test hand tracking separately, including pinch drawing and UI selection.

## Controller navigation fallback

If the floor teleport arc is difficult to see, use left-controller `X` to stand directly in front of the whiteboard and left-controller `Y` to cycle through the student tables. Use the right controller trigger to select drawing tools and write on the board or fixed paper. The in-headset controller guide opens at startup; right-controller `A` closes or reopens it.
