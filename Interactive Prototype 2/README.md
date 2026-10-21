# Interactive Prototype 2

Preparation package for the allocated studio on 22 or 23 October 2026. Prepared on 21 October 2026. Check your timetable for the exact studio time.

**Status: pre-session preparation. The mandatory studio, working-prototype video, fresh headset check, and actual results are PENDING.** This folder does not claim attendance, an upload, or new testing outcomes.

## Start here

1. Read the [Testing Plan PDF](Testing/Testing%20Plan.pdf) before the session and submit it before the first studio as required.
2. Open the [Participant Record PDF](Testing/Participant%20Record.pdf) or editable DOCX. Print pages 2 and 3 for each actual participant. Page 1 is the title page.
3. Read the [Design Journal PDF](Design%20Journal/Design%20Journal.pdf) for the concept, sketches, decisions, and changes.
4. Install and check the [existing Quest APK](Builds/XRStudyWhiteboard_Quest2_v1.3.apk). This is a local file, not a freshly verified build.
5. Use [Source Code](Source%20Code/README.md) for the live Unity project and the portable project archive.
6. Record a clear demonstration using the [Recording Guide](Media/Recording%20Guide.md).
7. Complete the blank files in [Results](Results/README.md) during the real session.
8. Update the [Evaluation draft](Evaluation/README.md) from the actual assessed session before submitting it as the final result.

## Submission map

| Requirement | Location | Status |
| --- | --- | --- |
| Unity source project | `../Whiteboard` and local `Source Code/Whiteboard Unity Project.zip` | Prepared from tracked source |
| Quest APK | `Builds/XRStudyWhiteboard_Quest2_v1.3.apk` | Packaged. Fresh headset check PENDING |
| Purpose and testing plan | `Testing/Testing Plan.md`, `.docx`, `.pdf` | Prepared. Blackboard upload PENDING |
| Design journal, sketches, and decisions | `Design Journal/` | Prepared as a retrospective journal |
| Participant record | `Testing/Participant Record.md`, `.docx`, `.pdf` | Blank and ready |
| Actual studio outcomes | `Results/` | PENDING. Templates contain no results |
| Working-prototype video | `Media/` | PENDING. No classroom demo video was found |
| Design Evaluation 3 | `Evaluation/` | Existing analysis moved here. Verify session provenance and update after testing |
| Earlier evaluations and evidence | `Earlier Evidence/` | Reference copies. Original files remain in the historical folders |
| Mandatory studio attendance | Your allocated in-person session | PENDING. Cannot be completed by preparing files |
| Final Blackboard submission | Course submission link | PENDING. No upload has been made |

## What moved and what stayed

The latest Evaluation 3 DOCX, PDF, Markdown source, and build/check tools moved into this package. The existing APK moved from the repository root into `Builds`. Earlier evaluations and the five-person evidence record were copied for reference. Their originals were kept to preserve history and links. The existing Unity project stays at `../Whiteboard` so its open project and asset paths continue to work. A portable archive is included locally.

The APK and 115 MB source archive are excluded from Git to avoid putting large submission binaries into repository history. They exist in this folder on this computer. GitHub contains the full tracked Unity project at `Whiteboard`, the package documents, templates, checks, and wiki. Downloading the repository alone does not recreate the local APK or archive.

## Evidence and dates

The Kai, Syifa, Amy, Serafina, and Safira records already existed before this preparation. Only Kai and Syifa have recording links. Earlier confirmation labelled those records as final IP2b, but the mandatory studio is now described as upcoming. Verify the actual session date and assessment context before using them as the Week 12 outcomes. The evaluation is therefore held as a pre-session draft pending that check. Do not rename the five earlier participants as new participants or fill tomorrow's results from their ratings.

The blank forms use planned IDs `W12-P01` to `W12-P05`. These are empty slots, not claims that five people have participated. Record the actual number and any incomplete sessions.

## Tonight and tomorrow

- [ ] Complete the fresh Quest headset check in `Builds/Headset Check.md`.
- [ ] Record and review the working-prototype video.
- [ ] Submit the testing plan before the first studio.
- [ ] Charge the headset, controllers, and laptop. Pack the USB cable and charger.
- [ ] Attend the allocated mandatory studio with the prototype and forms.
- [ ] Obtain consent and collect genuine observations, prompts, times, strokes, and ratings.
- [ ] Review the results and update the evaluation with the actual session date.
- [ ] Submit code, video, journal, plan, and outcomes by the end of the session.

## Document checks

From the repository root, run `python 'Interactive Prototype 2/Tools/check_preparation.py'`. The Evaluation 3 checks now run with `python 'Interactive Prototype 2/Tools/check_evaluation3.py'`. Document tools require `python-docx` and `PyMuPDF`. Preparation documents use A4, black Times New Roman, APA 7 student-paper conventions, and plain English. Tables use compact spacing for readability.
