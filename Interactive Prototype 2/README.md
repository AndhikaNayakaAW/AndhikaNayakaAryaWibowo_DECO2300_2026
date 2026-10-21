# Interactive Prototype 2

Preparation package for the allocated studio on 22 or 23 October 2026. Prepared on 21 October 2026. Check your timetable for the exact studio time.

**Status: prototype, existing participant testing, and working-prototype video are provided. Mandatory studio attendance and course uploads are PENDING.** This folder contains existing results plus pre-session forms for any additional studio testing. Preparing files does not claim attendance or an upload.

## Start here

1. Read the [Testing Plan PDF](Testing/Testing%20Plan.pdf) before the session and submit it before the first studio as required.
2. Open the [Participant Record PDF](Testing/Participant%20Record.pdf) or editable DOCX. Print pages 2 and 3 for each actual participant. Page 1 is the title page.
3. Read the [Design Journal PDF](Design%20Journal/Design%20Journal.pdf) for the concept, sketches, decisions, and changes.
4. Install and check the [existing Quest APK](Builds/XRStudyWhiteboard_Quest2_v1.3.apk). This is a local file, not a freshly verified build.
5. Use [Source Code](Source%20Code/README.md) for the live Unity project and the portable project archive.
6. Use the supplied Syifa video described in [Video Evidence](Media/Video%20Evidence.md). Check full playback and marker access before submission.
7. Use the blank files in [Results](Results/README.md) for any additional studio observations. Existing testing is already documented in Earlier Evidence.
8. Update the [Evaluation draft](Evaluation/README.md) from the actual assessed session before submitting it as the final result.

## Submission map

| Requirement | Location | Status |
| --- | --- | --- |
| Unity source project | `../Whiteboard` and local `Source Code/Whiteboard Unity Project.zip` | Prepared from tracked source |
| Quest APK | `Builds/XRStudyWhiteboard_Quest2_v1.3.apk` | Packaged. Fresh headset check PENDING |
| Purpose and testing plan | `Testing/Testing Plan.md`, `.docx`, `.pdf` | Prepared. Blackboard upload PENDING |
| Design journal, sketches, and decisions | `Design Journal/` | Prepared as a retrospective journal |
| Participant record | `Testing/Participant Record.md`, `.docx`, `.pdf` | Blank and ready |
| Existing participant outcomes | `Earlier Evidence/` and `Evaluation/` | Provided. Preserve actual session dates |
| Additional studio outcomes | `Results/` | Blank forms ready if required tomorrow |
| Working-prototype video | `Media/` | PROVIDED. Syifa MOV and 1080p MP4, about 5 minutes 38 seconds |
| Design Evaluation 3 | `Evaluation/` | Existing analysis moved here. Verify session provenance and update after testing |
| Earlier evaluations and evidence | `Earlier Evidence/` | Reference copies. Original files remain in the historical folders |
| Mandatory studio attendance | Your allocated in-person session | PENDING. Cannot be completed by preparing files |
| Final Blackboard submission | Course submission link | PENDING. No upload has been made |

The [Code Study Guide PDF](Code%20Study/Code%20Study%20Guide.pdf) explains all 34 custom C# scripts, their methods, why the folders are separated, and how each feature works. Editable DOCX and Markdown versions and an exact code index are in Code Study.

## What moved and what stayed

The latest Evaluation 3 DOCX, PDF, Markdown source, and build/check tools moved into this package. The existing APK moved from the repository root into `Builds`. Earlier evaluations and the five-person evidence record were copied for reference. Their originals were kept to preserve history and links. The existing Unity project stays at `../Whiteboard` so its open project and asset paths continue to work. A portable archive is included locally.

The APK, 115 MB source archive, original MOV, and 1080p MP4 are excluded from Git because they are large submission files. They exist in this folder on this computer. GitHub contains the full tracked Unity project at `Whiteboard`, documents, templates, checks, video manifest, and wiki. Downloading the repository alone does not recreate the local binaries or videos.

## Evidence and dates

The student confirms that the Kai, Syifa, Amy, Serafina, and Safira testing has already been completed using the latest APK. Only Kai and Syifa have recording evidence. Syifa's actual MOV is now included locally. The completed sessions remain valid existing evidence. Keep their real dates and distinguish them from attendance or any additional testing at the upcoming mandatory studio. Do not duplicate the same people as new sessions or copy their ratings into the new blank forms.

The blank forms use planned IDs `W12-P01` to `W12-P05`. These are empty slots, not claims that five people have participated. Record the actual number and any incomplete sessions.

## Tonight and tomorrow

- [x] Keep the existing participant results and Syifa video in the package.
- [ ] Briefly check the known working Quest build before the studio using `Builds/Headset Check.md`.
- [ ] Check full video playback and marker access to the uploaded file or link.
- [ ] Submit the testing plan before the first studio.
- [ ] Charge the headset, controllers, and laptop. Pack the USB cable and charger.
- [ ] Attend the allocated mandatory studio with the prototype and forms.
- [ ] Obtain consent and record any additional testing requested in the studio. Do not repeat earlier tests merely to fill the blank forms.
- [ ] Review the results and update the evaluation with the actual session date.
- [ ] Submit code, video, journal, plan, and outcomes by the end of the session.

## Document checks

From the repository root, run `python 'Interactive Prototype 2/Tools/check_preparation.py'`. The Evaluation 3 checks now run with `python 'Interactive Prototype 2/Tools/check_evaluation3.py'`. Document tools require `python-docx` and `PyMuPDF`. Preparation documents use A4, black Times New Roman, APA 7 student-paper conventions, and plain English. Tables use compact spacing for readability.
