# Week 02 · Paper Demo & Testing Results

> A completed exploration of how simple hand gestures can make an XR study whiteboard feel natural, focused, and welcoming.

<p align="center">
  <img src="Images/lowfi-1.jpeg" alt="XR Study Whiteboard paper demo" width="680">
</p>

<p align="center"><em>Low-fidelity three-dimensional paper demo · 6 August 2026</em></p>

> **Demo outcome:** Both participants completed the full activity successfully without mistakes or assistance.

## At a glance

| | |
| --- | --- |
| **Course** | DECO2300 – Digital Prototyping and Extended Reality |
| **Student** | Andhika Nayaka Arya Wibowo |
| **Concept** | XR Study Whiteboard |
| **Session** | Completed paper interaction demo |
| **Methods** | Wizard-of-Oz + Think-Aloud Protocol |
| **Result** | Successful, clear, and positively received |

## 01 · Concept

The XR Study Whiteboard is a virtual study space where students can write revision notes on a large virtual whiteboard using simple hand gestures. It supports studying, revision, brainstorming, and note-taking in an immersive environment.

The idea is to give students the freedom to study in different locations while preserving the focus and atmosphere of being in a classroom. The completed demo focused on a single student using the whiteboard.

### Interaction model

| Gesture or control | Result during the demo |
| --- | --- |
| Pinch | Started writing or drawing |
| Release pinch | Stopped writing or drawing |
| Point | Selected a marker colour |
| Two-finger swipe | Switched between marker and eraser |
| `Clear Board` button | Cleared the entire board |

## 02 · Purpose of the demo

The demo tested whether users could understand, remember, and comfortably perform the main whiteboard interactions. It focused on interaction design rather than Unity performance, Meta Quest hand tracking, or handwriting recognition.

The central question was:

> **Can users understand how to write, change colour, and switch between marker and eraser using simple hand gestures?**

The result was positive. Both participants understood the interaction model and completed the activity without mistakes.

## 03 · Design decisions and outcomes

| Design decision | Outcome from testing |
| --- | --- |
| Pinching starts writing | Both participants used the pinch gesture correctly on their first attempt. |
| Releasing the pinch stops writing | Neither participant continued drawing accidentally. |
| Pointing selects a colour | Both participants immediately selected blue when asked. |
| Two-finger swiping changes tools | Both participants switched between marker and eraser correctly. |
| The active tool stays visible | Participants checked the label and understood which tool was active. |
| Clearing uses a visible button | Both participants found and used `Clear Board` confidently. |

## 04 · Paper demo setup

The three-dimensional paper classroom included:

- A paper classroom structure and upright whiteboard.
- A large area for writing study notes.
- Four colour options: black, red, blue, and green.
- A current-tool label and a `Clear Board` button.
- Gesture instruction cards.
- Movable paper labels for marker and eraser modes.

The low-fidelity format made it possible to change the interface immediately while acting as the system. This kept the session focused on the quality of the interaction rather than visual polish or technical implementation.

## 05 · Testing approach

### Wizard-of-Oz

I manually acted as the XR system. When a participant performed a gesture, I changed the paper model to simulate the corresponding digital response:

- The participant could write after performing a pinch.
- The `Marker` label changed to `Eraser` after a two-finger swipe.
- The marker colour changed after pointing at a colour.
- The writing disappeared after selecting `Clear Board`.

### Think-Aloud Protocol

Participants described what they were thinking as they used the demo. This made it possible to hear what they expected each gesture to do and how they interpreted the visual feedback.

## 06 · Participant task

Participants were asked to complete this activity:

> Imagine that you are using this whiteboard inside a virtual classroom. Write one short study note, change the marker colour to blue, erase one word, switch back to the marker, and then clear the board.

The gesture instructions were visible, but the interaction was not explained step by step beforehand. This allowed the first response to each control to be observed naturally.

### Session flow

1. Introduce the XR Study Whiteboard and the paper classroom.
2. Explain that the hand gestures control the whiteboard.
3. Show the whiteboard and gesture cards.
4. Give the participants the task and ask them to think aloud.
5. Simulate each system response using the Wizard-of-Oz method.
6. Record actions, comments, and reactions during the activity.
7. Ask for feedback after the task.

## 07 · Testing results

### Performance summary

| Observation | Result |
| --- | --- |
| Understood the pinch gesture | Yes — both participants used it correctly on the first attempt. |
| Knew how to stop writing | Yes — both released the pinch naturally. |
| Understood colour selection | Yes — both selected blue immediately. |
| Understood the two-finger swipe | Yes — both switched tools correctly without prompting. |
| Noticed the current-tool label | Yes — both used it to confirm the active tool. |
| Understood the clear-board button | Yes — both found and pressed it confidently. |
| Most hesitation | None observed. |
| Completed the full task | Yes — both participants completed every step without mistakes. |

### Successful interactions

Every interaction in the task was completed successfully:

- Participants began writing with the pinch gesture.
- They stopped writing by releasing the pinch.
- They selected blue by pointing at the colour control.
- They changed to the eraser with a two-finger swipe.
- They erased one word and returned to the marker.
- They cleared the board using the visible button.

### Observed behaviour

- Both participants looked at the gesture cards briefly and then acted independently.
- Both began with the pinch gesture and understood the response immediately.
- Neither participant hesitated when changing colour or switching tools.
- Both noticed the current-tool label and used it as confirmation.
- Participants smiled and remained engaged throughout the activity.
- No accidental drawing, incorrect tool changes, or unintended clearing was observed.

### Participant feedback

> “It feels like I can study anywhere, but I still get the vibe of being in class.”

> “The gestures are simple enough that I can focus on my notes instead of looking through menus.”

> “I liked that the label showed me whether I was writing or erasing.”

The feedback suggests that the concept successfully combined location flexibility with the familiar focus of a classroom study environment.

## 08 · Evaluation

The completed demo validated the main interaction model. Participants understood the gestures without step-by-step coaching and completed the full task accurately.

The visible controls and feedback supported the gestures well. Pointing at a colour felt direct because the options were visible. The active-tool label gave participants confidence when moving between marker and eraser. The `Clear Board` button was also easy to find and did not cause accidental activation.

The most important success was the overall feeling of the experience. Participants understood the whiteboard as a flexible study space: they could imagine using it anywhere while still having the atmosphere and structure of a classroom.

## 09 · Design direction

The testing results confirmed the following design direction:

### Keep the gesture set simple

Pinch, release, point, and two-finger swipe were easy to understand and remember during the demo.

### Keep the active-tool feedback visible

The current-tool label should remain a central part of the interface. A marker or eraser icon and a subtle active-state highlight can reinforce this feedback in the digital version.

### Keep colour selection visible

The four-colour set — black, red, blue, and green — was easy to scan and select. Keeping the controls visible supports quick note-taking.

### Keep clearing as a deliberate action

The visible `Clear Board` button worked well because it was easy to locate but still required an intentional press. A confirmation message can protect the user’s work in the digital implementation.

## 10 · Reflection

The session showed that a gesture can feel natural when it has a clear visual response. Participants did not need extensive instructions because every action produced an obvious change in the paper model.

The demo also showed that XR study tools do not need to imitate a complete physical classroom to create the right feeling. The whiteboard, the simple controls, and the focused task were enough for participants to describe the experience as studying anywhere with a classroom atmosphere.

The Wizard-of-Oz method was especially useful because it allowed the interaction to feel responsive before any technical system existed. The Think-Aloud Protocol then made the participants’ expectations visible while they were using the design.

## 11 · Next development direction

The successful paper demo supports moving into a simple digital Unity version with:

1. A three-dimensional classroom or study room.
2. A large whiteboard surface.
3. Mouse- or controller-based drawing.
4. Four marker colours.
5. Marker and eraser modes.
6. A visible current-tool indicator.
7. A clear-board button.

Mouse or Meta Quest controllers can be used to validate the drawing system before adding full hand tracking. Once the digital foundation is stable, the concept can explore pinch-based drawing, spatial whiteboard positioning, physical marker and eraser objects, richer feedback, and collaborative study.

## 12 · Key learning

| Principle | Learning from the session |
| --- | --- |
| **Explore before polishing** | The paper demo confirmed the interaction before technical development began. |
| **Test the whole body** | XR gestures need to be experienced as physical movements in space. |
| **Make feedback immediate** | Clear visual responses help users feel confident and stay focused. |
| **Design for atmosphere as well as function** | A flexible study location can still feel structured and familiar like a classroom. |
