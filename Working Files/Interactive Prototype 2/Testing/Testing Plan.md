# Interactive Prototype 2b Testing Plan

## Purpose and Prototype

XR Study Whiteboard is a Unity VR classroom with a shared teaching board and fixed personal note paper. This plan is prepared on 21 October 2026 for the allocated Week 12 studio on 22 or 23 October. It asks whether a new user can follow the board, take notes, navigate, and replay board actions with little assistance. The study tests interaction usability. It does not measure improved concentration or long-term learning.

## What Changed From IP2a

Earlier evidence identified hard-to-find paper tools, occasional paper drawing problems, uncomfortable viewing, and unclear teleport controls. The revised design uses fixed paper, a shared seated view, separate controller roles, navigation shortcuts, and a guide. The left controller handles movement. X moves to the whiteboard and Y moves through the tables. The right trigger selects tools and writes. A closes or reopens the guide.

## Construction Decisions

The test includes both study surfaces, paper tools, marker colours, erasing, clearing, navigation, and board-action replay. These features form one classroom task. Paper stays fixed to prevent grabbing from competing with writing. The guide stays available during the session. Saved notes, microphone recording, multiplayer, hand tracking, and AR are excluded. They would add separate questions and are not required to evaluate this controller workflow.

<!-- page -->

# Participants, Measures, and Setup

## Participants and Timing

Aim for five classmates if the studio allocation permits. Use planned IDs W12-P01 to W12-P05 and record the actual sample size. Record prior VR experience without identifying details in the results file. Allow about 10 to 15 minutes per person. Shorten the task set only if needed for the allocated studio and record any change. Never enter a pass for an omitted activity.

**Table 1**
*Success Criteria Declared Before Testing*

| Aim | Success criterion |
| --- | --- |
| Shared seated view | At least four of five can view both surfaces and copy the date, line, and circle without leaving the desk. |
| Paper tool discovery | At least four of five select Pencil without a moderator hint within 15 seconds of the desk task instruction. |
| Paper drawing | At least four of five produce at least nine acceptable strokes out of ten planned strokes. |
| Navigation and tracking | At least four of five reach the board and a table. No persistent controller misalignment prevents the task. |
| Recording | At least four of five complete Record, draw, Stop, and Play, then explain what was replayed. Record assistance separately. |
| Confidence and comfort | Median confidence is at least 4/5. No severe discomfort, data loss, or unresolved blocker occurs. |

If fewer than five participate, report counts and proportions without claiming that a four-of-five threshold was met. A target can remain uncertain. A successful assisted task is not an independent success.

## Setup Before Each Session

Use the same tested APK and headset for every participant. Note the APK filename and source snapshot. Clear the surfaces and reset recording. Put the date 22 October 2026, a horizontal line, and a circle on the teaching board. Confirm the desk paper is blank and the guide is available. Keep the headset in a clear space. Start the observation sheet and check recording consent before capturing any person.

<!-- page -->

# Participant Task Sequence

Say: "This is a VR classroom for following a lesson and taking notes. Please tell me what you expect and what you are trying. The prototype is being tested. You can stop at any time."

**Table 2**
*Tasks and Observation Focus*

| Task | Participant instruction and observation |
| --- | --- |
| 1. Explore | Look around and tell me what you think you can do. Observe the guide, expectations, and both controllers. |
| 2. Shared view | Move to a student desk. Can you see the paper and teaching board comfortably? Record seat adjustment or help. |
| 3. Take notes | Copy the date, horizontal line, and circle from the board onto the paper. Observe attention switching and any missing marks. |
| 4. Paper tools | Select Pencil, write, erase a small part, and clear the paper. Then complete the ten-stroke test described on the next page. |
| 5. Move and return | Visit another table, then return to the whiteboard. Observe controller alignment and navigation discovery. |
| 6. Whiteboard | Write a short word, change colour, erase a small part, and clear the board. Record accidental actions and prompts. |
| 7. Record and replay | Record a short board action, stop, and play it. Tell me what you think was recorded and whether replay matches your expectation. |
| 8. Tracking and comfort | While moving and drawing, do the controllers follow your hands? Ask about discomfort and stop if needed. |

The task instructions name the goal rather than the required button. Do not demonstrate X, Y, A, the trigger, or the recording order before the participant has had a chance to use the guide. If help is needed, record it. Do not let a participant remain stuck or uncomfortable just to preserve a timing measure.

<!-- page -->

# Recording Observations Fairly

## Discovery and Moderator Assistance

At the desk, say "Select the paper tool you would use to write" and start the timer. Stop when Pencil is selected. At 15 seconds, record whether the independent target was met. Ask "What would you try next?" before giving a control hint. Log each prompt and time. Use levels 0 for none, 1 for a neutral question, 2 for a hint, and 3 for a demonstration. Any prompt before selection makes discovery assisted.

## Ten-Stroke Paper Check

Ask for a horizontal line, vertical line, circle, L shape, and short word. Repeat once for ten strokes. One trigger hold is one attempt. Record initial attempts in Stroke Record.csv. Accept a stroke if it appears on the intended paper, stays continuous, stops on release, and does not connect to the next stroke. Judge system response rather than handwriting. Log gaps, missing marks, unwanted continuation, and wrong-surface marks as defects. Keep retries separate.

## Outcome Definitions

Use Independent for completion without moderator input, Assisted for completion with help, Incomplete for an unfinished task, Blocked for a technical problem, and Not run for an omission. Record retries and factual observations. Ask about comfort directly rather than inferring it from silence.

## Feedback Questions

Ask what was easiest, hardest, and worth improving, and whether remote study would be useful. Collect the six measures on the participant form from 1, very poor, to 5, excellent. Do not suggest a preferred score.

<!-- page -->

# Analysis and Submission Procedure

## Analysis After the Session

Report participant count, independent and assisted completions, median ratings, discovery times, acceptable strokes, and tracking or comfort issues. Compare each aim with Table 1 as Met, Not met, or Uncertain. Missing measurements remain Uncertain. Separate observations from quotations. Quote only words captured in a consented recording or written verbatim at the time.

## Practical Contingencies

If a participant feels unwell, stop without pressure to continue. If the APK fails, record the error and check the device before using a backup. Desktop discussion does not count as physical-controller testing. Record any device, build, or task change. The required working-prototype video is a separate recording, not in-app board replay.

## Before the First Studio

Submit this plan through the course link. Confirm the studio time. Bring the tested headset, charged controllers, laptop, cable, source archive, APK, journal, and forms. Keep local copies for offline use.

## At the End of Testing

Save participant records, results, consent status, and media links. Review the demo video and update the evaluation from actual observations. Submit source, video, journal, plan, and outcomes through the course link by the end of the session. Preparing this folder does not complete the submission.
