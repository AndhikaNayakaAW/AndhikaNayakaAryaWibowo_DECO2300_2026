# Interactive Prototype 2b Design Journal

## Journal Scope

This journal was assembled on 21 October 2026 from the existing paper prototype, testing records, source files, and Git history. It is a retrospective account of documented development, not a claim that new dated sketches were made during every earlier stage. The existing paper prototype photograph is included in this folder. Figure 1 is a new summary sketch of the current interaction.

## Original Idea and Early Exploration

The original idea was a study space that could preserve the feeling of a classroom while allowing remote participation. The Week 2 paper model used a shared board, visible colours, tool labels, and gesture cards. Wizard-of-Oz responses let two users try pinch, release, pointing, and tool changes before a full XR system existed. The model was useful for discussing expectations. It could not establish actual tracking quality, latency, or headset comfort.

## What IP1 Revealed

Five classmates completed the earlier Unity workflow. Three sessions have recorded interview evidence and two have checklist records. The table tools were hard to notice, one participant described paper drawing as glitchy, teleport discovery was inconsistent, and vertical viewing was uncomfortable for one participant. These were important problems because the classroom depends on moving attention between the teaching board and personal notes. A working whiteboard alone was not enough.

<!-- page -->

# Development Decisions and Trace

**Table 1**
*Documented Development and Design Reasoning*

| Record | Decision and reason |
| --- | --- |
| 20 September, 76d76ece | Align seated camera anchors and simulator movement. The learner needs a usable view and predictable controller positions. |
| 20 September, 279ef661 | Align drawing surfaces and add timed board-action replay. Marks need to appear where the learner aims. |
| 20 September, 1661a5b4 | Improve study controls and remove unnecessary board clutter. The paper tools need to be visible beside the writing surface. |
| 30 September, 90b03e2f | Restore Quest controls and navigation. Desktop operation alone cannot establish controller usability. |
| 1 October, 11375c85 | Add left-controller destination shortcuts. X reaches the board and Y cycles through the tables. |
| 1 October, 88a59400 | Improve whiteboard and paper interaction. Fixed paper and right-controller tool selection reduce competing grab and write actions. |

Dates above are repository commit dates. They document implementation, not the date or outcome of a new participant session.

## Alternatives and Scope

The earlier gesture idea remains an exploration, while the assessed study task uses physical controllers. Controllers provide explicit writing and selection controls for this test. Grabbable paper was excluded because it competed with note taking. A classroom expansion, multiplayer, and saved notes were deferred so the session can evaluate the basic lesson-to-notes workflow. Recording stores board actions and timing, not microphone audio or a screen video. AR remains a future comparison rather than an implemented claim.

<!-- page -->

# Current Interaction Sketch and Learning

**Figure 1**
*Retrospective Sketch of the Current Study Workflow*

<!-- figure:Interaction Sketch.png -->

This sketch was prepared on 21 October 2026. The left controller changes location. The right controller selects tools and writes. At the desk, both study surfaces remain visible.

## Learning From the Existing Headset Records

The existing records report completion and high ratings, but Syifa's transcript includes prompts and repeated recording. Independent first use remains uncertain. The new plan logs time, assistance, and ten initial strokes, while keeping headset and simulator evidence separate.

## What Remains Open

Check the headset, attend the studio, and collect actual outcomes. Verify earlier session dates before claiming Week 12 evidence. Compare observations with the targets and update the evaluation. Satisfaction alone does not establish better learning.
