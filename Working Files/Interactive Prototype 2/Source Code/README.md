# Unity source

The live project remains at `../../Whiteboard`. Open it with Unity `6000.3.21f1`. The main scene is `Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity`.

The local archive `Whiteboard Unity Project.zip` contains the tracked Assets, Packages, and ProjectSettings needed to import the project. Extract it, then open its `Whiteboard` folder in Unity Hub. Library, Temp, build caches, backup folders, and local uncommitted edits are excluded. Unity will reimport assets. Do not open both copies for editing and accidentally diverge them.

`Manifest.json` records the source commit and archive SHA-256. This archive is a snapshot of committed source. It does not include the unrelated local AI_USE edit. The existing APK has its own manifest in `../Builds`. Its exact source match and fresh headset operation still need verification. It was not rebuilt during this packaging task.

The 115 MB archive is kept locally and excluded from Git. The tracked source is already available in the main GitHub repository under `Whiteboard`. If the submission requires an uploaded project rather than a repository link, upload this local archive through the approved submission system.

The copied setup, controls, and Quest build notes are references. The fresh headset check in `../Builds/Headset Check.md` determines readiness for the upcoming session. Historical QA notes should not be treated as today's physical-device test.
