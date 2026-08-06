# Week 02 · Paper Prototyping & Testing

> Exploring whether simple hand gestures can make an XR study whiteboard feel natural, understandable, and useful.

<p align="center">
  <img src="Images/lowfi-1.jpeg" alt="Low-fidelity paper prototype of the XR Study Whiteboard" width="680">
</p>

<p align="center"><em>Low-fidelity three-dimensional paper prototype · 6 August 2026</em></p>

## At a glance

| | |
| --- | --- |
| **Course** | DECO2300 – Digital Prototyping and Extended Reality |
| **Student** | Andhika Nayaka Arya Wibowo |
| **Concept** | XR Study Whiteboard |
| **Prototype** | Low-fidelity three-dimensional paper classroom |
| **Methods** | Wizard-of-Oz + Think-Aloud Protocol |
| **Focus** | Gesture clarity, tool switching, feedback, and comfort |

## 01 · Concept

The XR Study Whiteboard is a virtual study space where students can write revision notes on a large virtual whiteboard using simple hand gestures. It is designed for studying, revision, brainstorming, and note-taking in an immersive environment.

The first concept is single-user. Multiplayer collaboration may be explored later, but it is outside the scope of this paper prototype.

### Interaction model

| Gesture or control | Intended action |
| --- | --- |
| Pinch | Begin writing or drawing |
| Release pinch | Stop writing or drawing |
| Point | Select a marker colour |
| Two-finger swipe | Switch between marker and eraser |
| `Clear Board` button | Clear the entire board |

## 02 · Prototype question

> **Can users understand how to write, change colour, and switch between marker and eraser using simple hand gestures?**

This prototype explored the interaction design rather than Unity performance, Meta Quest hand tracking, or handwriting recognition.

## 03 · Design assumptions

1. **Pinching is suitable for writing.** Bringing the thumb and index finger together begins drawing; releasing the pinch stops it.
2. **Pointing is suitable for colour selection.** Users can point directly at visible colour buttons.
3. **A two-finger swipe can switch tools.** Users can move between marker and eraser modes with a left or right swipe.
4. **The active tool must remain visible.** A clear label should always show whether the marker or eraser is active.
5. **Clearing should use a button.** A visible button reduces the number of gestures to remember and lowers the risk of accidental deletion.

## 04 · Prototype details

The paper prototype included:

- A paper classroom structure and upright whiteboard.
- A large area for writing study notes.
- Four colour options: black, red, blue, and green.
- A current-tool label and a `Clear Board` button.
- Gesture instruction cards.
- Movable paper labels for marker and eraser modes.

The prototype was intentionally simple. Its purpose was to expose interaction problems before time was spent building a polished digital version.

## 05 · Testing plan

### What was tested?

- Beginning and stopping a writing action.
- Changing the marker colour.
- Switching between marker and eraser.
- Understanding which tool was active.
- Clearing the whiteboard.

### What did I need to learn?

- Are the gestures understandable without extensive explanation?
- Are they easy to remember and comfortable to perform?
- Can users distinguish marker and eraser modes?
- Are the colour controls visible and understandable?
- Do users need more visual feedback?

### Methods

#### Wizard-of-Oz

I manually acted as the XR system. When the participant performed a gesture, I changed the paper prototype to simulate the response of the future digital system.

Examples:

- Allowing the participant to write after a pinch.
- Replacing `Marker` with `Eraser` after a two-finger swipe.
- Changing the marker colour after the participant pointed at a colour.
- Removing the writing after `Clear Board` was selected.

#### Think-Aloud Protocol

The participant described their thoughts while using the prototype. This helped reveal what they believed each gesture would do, which elements they noticed first, where they became confused, and what feedback they expected.

## 06 · Participant task

> Imagine that you are using this whiteboard inside a virtual classroom. Write one short study note, change the marker colour to blue, erase one word, switch back to the marker, and then clear the board.

The participant was encouraged to think aloud throughout the activity. Gesture instructions were visible, but I avoided explaining every step in advance so I could observe independent understanding.

### Testing process

1. Introduce the XR Study Whiteboard concept.
2. Explain that the paper model represents a virtual classroom.
3. Show the prototype and gesture cards.
4. Give the participant the task and ask them to think aloud.
5. Observe their first response and any hesitation.
6. Simulate each system response using the Wizard-of-Oz method.
7. Record successful actions, mistakes, comments, and unexpected behaviour.
8. Ask follow-up questions and compare results across participants where possible.

## 07 · Evidence & results

> This section is ready to complete after the testing session. Replace the placeholders with observed behaviour and exact participant comments.

| Observation | Result |
| --- | --- |
| Understood the pinch gesture? | `[Add result]` |
| Knew how to stop writing? | `[Add result]` |
| Understood colour selection? | `[Add result]` |
| Understood the two-finger swipe? | `[Add result]` |
| Noticed the current-tool label? | `[Add result]` |
| Understood the clear-board button? | `[Add result]` |
| Interaction causing the most hesitation | `[Add result]` |
| Completed the full task? | `[Yes / No / Partially]` |

### Successful interactions

- `[Add successful interaction.]`
- `[Add successful interaction.]`
- `[Add successful interaction.]`

### Confusing interactions

- `[Add confusing interaction.]`
- `[Add confusing interaction.]`

### Observed behaviour

- `[Describe what the participant did first.]`
- `[Describe where they hesitated.]`
- `[Describe whether they looked at the gesture cards.]`
- `[Describe whether they noticed the current-tool indicator.]`
- `[Describe any unexpected behaviour.]`

### Participant feedback

> “[Insert participant comment.]”

> “[Insert participant comment.]”

## 08 · Evaluation

The paper prototype demonstrates that visible controls and feedback remain important even when the primary interaction uses hand gestures.

Colour selection is a direct pointing action, so it may be easier to understand because users can see and select an available option. The marker-to-eraser interaction needs more testing: a two-finger swipe is not directly connected to the physical action of erasing and may require additional explanation.

The active-tool indicator should clearly show:

```text
Current Tool: Marker
```

or:

```text
Current Tool: Eraser
```

Potential feedback improvements include:

- A marker or eraser icon.
- A cursor that changes shape.
- A short sound or small animation.
- A colour highlight around the active tool.

## 09 · Iteration decisions

### Make the active tool more visible

The next prototype could use a larger tool label, an active marker or eraser icon, a highlighted border, and a cursor that changes shape.

### Reconsider the swipe gesture

If testing shows that the two-finger swipe is confusing, possible alternatives include:

- Pointing at a visible marker or eraser button.
- Holding an open palm to open a small tool menu.
- Using a controller button in the first Unity prototype.
- Physically picking up a virtual marker or eraser.

### Keep colour selection simple

The first digital prototype will keep four visible colour choices: black, red, blue, and green.

### Keep clearing as a button

`Clear Board` should remain a visible button because accidental activation would remove all of the user’s work. A confirmation step may later be added:

```text
Clear all notes?

[Cancel] [Clear]
```

## 10 · Reflection

A gesture can appear logical to the designer but still be unclear to another user. Paper prototyping made it possible to test the interaction before investing time in Unity.

The process also reinforced that XR interactions need clear feedback. Users should understand what tool is active, whether a gesture has been recognised, whether they are drawing or erasing, and what will happen before a destructive action is selected.

The most important advice for the next testing session is to test one clear interaction question rather than the entire concept at once:

- Prepare clear tasks before testing.
- Avoid explaining the intended interaction too early.
- Observe the user’s first natural response.
- Ask participants to think aloud.
- Record exact comments instead of relying on memory.
- Test with more than one participant.
- Separate usability problems from technical problems.
- Photograph each prototype version.
- Keep evidence organised in Git.

## 11 · Next prototype

The next step is a simple digital Unity version containing:

1. A basic three-dimensional classroom or study room.
2. A large whiteboard surface.
3. Mouse- or controller-based drawing.
4. Four marker colours.
5. Marker and eraser modes.
6. A visible current-tool indicator.
7. A clear-board button.

Starting with mouse or Meta Quest controllers will allow the drawing and tool-selection system to be tested before adding more complex gesture recognition. Later XR exploration can include pinch-based drawing, hand tracking, spatial whiteboard positioning, physical marker and eraser objects, improved feedback, multiple whiteboards, and collaboration.

## 12 · Key learning

| Principle | Takeaway |
| --- | --- |
| **Explore before polishing** | A paper model can expose design problems before development begins. |
| **Test the whole body** | XR gestures need to be tested as physical movements in space, not only as interface buttons. |
| **Let evidence guide iteration** | Participant behaviour and comments should shape the next design decisions. |

## 13 · Next actions

- [ ] Add photographs of the remaining prototype versions.
- [ ] Add photographs of the testing session.
- [ ] Write the participant’s exact comments.
- [ ] Complete the testing-results table.
- [ ] Describe the main problem identified during testing.
- [ ] Add the updated concept to the Concept Design Report.
- [ ] Commit and push the completed evidence to GitHub.

## Suggested commit

```text
[Add] Week 2 paper prototype testing and reflection
```
