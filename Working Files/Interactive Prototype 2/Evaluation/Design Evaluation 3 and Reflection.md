# Design Evaluation 3 and Reflection

## Objective and Validation Metrics

The final IP2b test examined whether students could follow a board lesson and take notes in VR. It retested the seated view, paper tools, drawing, navigation, tracking, and replay after the earlier usability problems. This evaluation uses the final five-person headset test, documented in the Iteration 2 records. Success measures came from the existing testing plan. The aim was to judge whether the classroom workflow was usable before considering wider educational benefits.

**Table 1**
*Planned Success Measures*

| Aim | Criterion |
| --- | --- |
| Shared view and note taking | At least four of five view both surfaces and copy the lesson content. |
| Paper tools and drawing | At least four of five use the tools. Discovery within 15 seconds and 90% correct strokes were also planned. |
| Navigation and replay | At least four of five move and complete Record, Stop, and Play. Controllers remain aligned. |
| Confidence and comfort | Median confidence is at least 4/5 with no severe comfort, tracking, or data loss issue. |

## Method and Results

Five classmates used the Unity build, a headset, and two physical controllers in moderated task tests. Kai had a voice recording and Syifa had a camera recording. Amy, Serafina, and Safira have notes only. All five completed the eight recorded task categories, including copying the date, line, and circle onto paper. All six rating measures were 5/5 for every participant. No severe comfort problem or technical blocker was recorded. Appendix A contains the results and Appendix B contains the supplied session records.

<!-- page -->

# Analysis and Insights

**Table 2**
*Post-Test Ratings Across Five Participants*

| Measure | P01 | P02 | P03 | P04 | P05 | Median |
| --- | --- | --- | --- | --- | --- | --- |
| Seated view comfort | 5 | 5 | 5 | 5 | 5 | 5 |
| Paper drawing reliability | 5 | 5 | 5 | 5 | 5 | 5 |
| Paper control clarity | 5 | 5 | 5 | 5 | 5 | 5 |
| Movement predictability | 5 | 5 | 5 | 5 | 5 | 5 |
| Recording control clarity | 5 | 5 | 5 | 5 | 5 | 5 |
| Overall confidence | 5 | 5 | 5 | 5 | 5 | 5 |

The shared view supported the study task. Kai described looking between the board and paper without moving away from the desk. All five completed note copying. Comfortable placement therefore deserves priority over adding classroom decoration. The fixed paper also gave the right trigger a clear purpose. These results are consistent with the earlier viewpoint and paper fixes, although the study did not isolate each change.

Task completion did not mean immediate understanding. Kai initially explored the teleport controls before understanding them. Syifa reopened the guide and received instructions about colour selection and recording order. The transcript includes a second recording exercise, despite the checklist listing one attempt. I therefore report completion with support rather than five independent first-attempt successes. The guide helped recovery, but independent discovery remains uncertain.

All five completed replay, and Kai expected it to reproduce his marks. Syifa said playback matched her expectation. This supports replay as a review tool. It does not prove that replay improves recall. Likewise, positive remote-learning comments show interest, not increased concentration. The highest possible score on every rating also hides small differences between users and should be read alongside the observed need for help.

<!-- page -->

# Evaluation of Aims

The shared view and lesson-to-notes aims were met in all five sessions. Navigation and physical controller tracking also passed across the group. Recording was completed by everyone, with explicit moderator support in Syifa's transcript. These findings support the tested workflow under moderated conditions. No severe comfort problem was recorded, but short sessions cannot establish comfort during a full lesson.

Paper tools and drawing were supported by task completion and ratings. The planned 15-second discovery target cannot be verified because selection times and prompt levels were not logged. The 90% stroke target cannot be calculated without raw stroke and defect counts. Those two thresholds remain uncertain. Simulator T/Y controls and hand tracking were outside the physical-controller test. No aim was disproved, but unmeasured aims should not be labelled as passed.

## Updated Concept and Design Decisions

XR Study Whiteboard is a VR classroom where a learner follows the shared board while writing on fixed personal paper. The left controller handles navigation and the right controller selects tools and writes. Visible tool feedback and board-action replay support the study activity. The concept is partially confirmed. Participants could use and understand it, but better learning and sustained attention remain untested.

I would keep the seated viewpoints, fixed paper, and clear controller roles. I would improve first use with a brief practice task and a guide that remains easy to reopen. Recording needs clearer Ready, Recording, Stopped, and Playing states because Syifa needed sequence reminders. Saved notes should be tested before larger rooms or multiplayer, since preserving study work directly supports the core task. These are proposed refinements, not features validated by this test.

<!-- page -->

# Final Reflection

## Prototype Session Review

I chose Syifa's final IP2b session. She copied board content and completed paper tools and replay. Keeping both surfaces visible supported note taking. However, she reopened the guide and needed recording instructions. Her 5/5 ratings showed satisfaction, while the transcript revealed support needs that the pass checklist missed.

## Methodology and Concept

Paper Wizard-of-Oz testing helped me explore gesture meanings quickly, but manually simulating responses could not test tracking. IP1 exposed hidden paper tools and viewing problems. The IP2a/IP2b development cycle moved this work into headset testing. Think aloud revealed expectations, but my leading questions and demonstrations reduced evidence of independent use. Consistent tasks helped comparison, yet three notes-only records and missing timings weakened measurement. I learned to separate task completion from learning effectiveness. The classroom interaction is supported, while the wider educational claim is only partially confirmed.

## Improvements and Extensions

I would test five new users with neutral prompts and log time, assistance, stroke defects, and comfort. A longer lesson should check note quality and later recall. I would also prototype passthrough AR with a virtual board above a real desk. This might support physical note taking, but anchoring, readability, and distractions need comparison with VR before choosing it. AR might also weaken the classroom feeling that motivated VR.

<!-- appendices -->

# Appendix A

# Participant Results and Evidence Register

The facilitator confirmed that the five-person Iteration 2 headset dataset represents the final IP2b/Week 12 test. Participant IDs here belong to that dataset. They should not be combined with the earlier IP1 IDs as if they identify the same people. Only Kai and Syifa have recording links. The other three records are observation notes, not reconstructed interviews.

**Table A1**
*Evidence Available for Each Participant*

| ID | Name | Evidence |
| --- | --- | --- |
| P01 | Kai | Voice recording and supplied transcript |
| P02 | Syifa | Camera recording and supplied transcript |
| P03 | Amy | In-class notes only |
| P04 | Serafina | In-class notes only |
| P05 | Safira | In-class notes only |

**Table A2**
*Observed Task Completion*

| Task | P01 | P02 | P03 | P04 | P05 |
| --- | --- | --- | --- | --- | --- |
| Explore | Pass | Pass | Pass | Pass | Pass |
| Seat and shared view | Pass | Pass | Pass | Pass | Pass |
| Follow and take notes | Pass | Pass | Pass | Pass | Pass |
| Paper tools and strokes | Pass | Pass | Pass | Pass | Pass |
| Move and return | Pass | Pass | Pass | Pass | Pass |
| Whiteboard workflow | Pass | Pass | Pass | Pass | Pass |
| Record and replay | Pass | Pass | Pass | Pass | Pass |
| Physical controller tracking | Pass | Pass | Pass | Pass | Pass |
| Simulator T/Y selection | N/A | N/A | N/A | N/A | N/A |

The source checklist lists one attempt per category. Syifa's supplied transcript contains a repeated recording exercise and several prompts. Pass therefore means the task was completed during a moderated session. It does not establish that every task was completed independently on the first attempt. Table 2 in the assessed response reports all six supplied ratings for each participant. Informal 10/10 satisfaction comments are separate from those 1-to-5 measures.

## Recording Evidence

[Kai voice recording](https://drive.google.com/file/d/1kFrLP1ncaq4-GqdgPC0OP1HqeC7Bd_w9/view?usp=sharing)

[Syifa camera recording](https://drive.google.com/file/d/1gNw8L9R59p3xLFg-9AMe9GkxqopXuk_R/view?usp=sharing)

The supplied transcripts and notes are the basis of this report. The linked media have not been independently retranscribed for this submission. Kai's transcript refers to mouse input in one answer. The facilitator's note identifies the session as headset and physical-controller testing. Both statements are retained in Appendix B so the discrepancy remains visible.

<!-- raw-evidence -->

<!-- page -->

# Appendix C

# Evidence Audit and Development Trace

## Earlier Prototype Evidence

Two participants tested gesture meanings in the Week 2 paper prototype. Simulated responses could not establish tracking quality. IP1 has three recorded interviews and two checklist-only sessions. It identified hidden paper tools, one paper-drawing complaint, inconsistent teleport discovery, and viewing difficulty. Later headset results suggest improvement, but different participants and input conditions prevent a controlled comparison.

## Implementation Trace

Repository commits document seat and simulator fixes in 76d76ece, drawing and replay in 279ef661, study controls in 1661a5b4, Quest navigation in 11375c85, and paper interaction in 88a59400. These records show development. The participant records provide the usability evidence.

## Audit of Claims

The records support task completion, ratings, reported controller alignment, and short-session comfort. Discovery time, stroke percentages, prompt counts, learning gain, long-session comfort, and independent first use remain unmeasured. AR, saved notes, and new studies are proposals. No new participant data have been added.

## Source Record

The repository preserves the complete [final session records](https://github.com/AndhikaNayakaAW/AndhikaNayakaAryaWibowo_DECO2300_2026/blob/main/Design%20Evaluation/Interview%20Transcripts/Iteration2_XRStudyWhiteboard_Transcript_and_Results.md), [IP1 evidence](https://github.com/AndhikaNayakaAW/AndhikaNayakaAryaWibowo_DECO2300_2026/blob/main/Design%20Evaluation/Interview%20Transcripts/README.md), and [paper prototype record](https://github.com/AndhikaNayakaAW/AndhikaNayakaAryaWibowo_DECO2300_2026/blob/main/DesignProcess/Week02_PaperPrototype/README.md). Participant findings and development history are kept distinct throughout this evaluation.
