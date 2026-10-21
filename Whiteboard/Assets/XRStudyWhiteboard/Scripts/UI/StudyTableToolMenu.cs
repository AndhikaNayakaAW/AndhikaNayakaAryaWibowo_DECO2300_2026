// script overview: this script builds the drawing menu beside a desk paper. it provides pencil, eraser, and
// clear-paper buttons and highlights the selected tool. only the nearest occupied desk shows its full menu.
// pointer checks consume menu hits before they can become accidental ink, and a held press does not repeatedly
// click a button.

// search: paper writing | table controls | pencil | eraser | clear paper | colour feedback.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make text mesh pro types available for readable interface labels.
using TMPro;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make the types in unity engine.event systems available to this script.
using UnityEngine.EventSystems;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;
// make the types in unity engine.xr.interaction.toolkit.ui available to this script.
using UnityEngine.XR.Interaction.Toolkit.UI;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // always-open drawing controls beside the currently occupied desk.
    // declare the study table tool menu class based on unity behaviour.
    public sealed class StudyTableToolMenu : MonoBehaviour
    // begin the class study table tool menu block.
    {
        // keep active menus as list<study table tool menu>, initially a new list<study table tool menu>.
        private static readonly List<StudyTableToolMenu> ActiveMenus = new List<StudyTableToolMenu>();
        // paper: the individual desk sheet controlled by this component. keep paper as paper note canvas for
        // later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private PaperNoteCanvas paper;
        // keep menu object as game object for later use.
        private GameObject menuObject;
        // keep menu panel as game object for later use.
        private GameObject menuPanel;
        // keep selected tool text as tmp text for later use.
        private TMP_Text selectedToolText;
        // keep pencil button as button for later use.
        private Button pencilButton;
        // keep eraser button as button for later use.
        private Button eraserButton;
        // keep hovered button as button for later use.
        private Button hoveredButton;
        // keep direct controller click held as bool for later use.
        private bool directControllerClickHeld;
        // keep desktop click held as bool for later use.
        private bool desktopClickHeld;
        // keep desk bounds as bounds for later use.
        private Bounds deskBounds;
        // keep has desk bounds as bool for later use.
        private bool hasDeskBounds;
        // keep menu canvas as canvas for later use.
        private Canvas menuCanvas;

        // method: try handle desktop screen point.
        // convert the desktop screen position to a world ray before testing the desk menus.
        public static bool TryHandleDesktopScreenPoint(Vector2 screenPoint, Camera eventCamera, bool pressed)
        // begin the method try handle desktop screen point block.
        {
            // run the following statement only when event camera is missing.
            if (eventCamera == null)
                // return false to the caller.
                return false;
            // return try handle any ray internal using event camera: screen point to ray using screen point,
            // pressed, true to the caller.
            return TryHandleAnyRayInternal(eventCamera.ScreenPointToRay(screenPoint), pressed, true);
        // finish the method try handle desktop screen point block.
        }

        // method: end desktop pointer.
        // release desktop click and hover state on every active desk menu.
        public static void EndDesktopPointer()
        // begin the method end desktop pointer block.
        {
            // visit each menu in active menus.
            foreach (StudyTableToolMenu menu in ActiveMenus)
            // begin the for each statement block.
            {
                // run the following statement only when menu is missing.
                if (menu == null)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // set menu desktop click held to false.
                menu.desktopClickHeld = false;
                // menu: set pointer feedback using no object, false.
                menu.SetPointerFeedback(null, false);
            // finish the for each statement block.
            }
        // finish the method end desktop pointer block.
        }

        // method: try handle any ray.
        // check active desk menus for a controller-style ray hit.
        public static bool TryHandleAnyRay(Ray ray, bool pressed)
        // begin the method try handle any ray block.
        {
            // return try handle any ray internal using ray, pressed, false to the caller.
            return TryHandleAnyRayInternal(ray, pressed, false);
        // finish the method try handle any ray block.
        }

        // consumes a native xr ui hit without dispatching its click or pointer state again.
        // method: contains any ray.
        // recognise a ray inside an active desk panel without sending another click through the native xr
        // interface path.
        public static bool ContainsAnyRay(Ray ray)
        // begin the method contains any ray block.
        {
            // visit each menu in active menus.
            foreach (StudyTableToolMenu menu in ActiveMenus)
            // begin the for each statement block.
            {
                // run the following statement only when menu is missing or menu menu object is missing or not
                // (menu menu object active in hierarchy).
                if (menu == null || menu.menuObject == null || !menu.menuObject.activeInHierarchy)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // store plane as a new plane using menu menu object transform forward, menu menu object
                // transform position.
                Plane plane = new Plane(menu.menuObject.transform.forward, menu.menuObject.transform.position);
                // run the following statement only when not (plane: raycast using ray, the output value
                // distance) or distance is less than 0 or distance is greater than 2.2.
                if (!plane.Raycast(ray, out float distance) || distance < 0f || distance > 2.2f)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // store panel as menu menu panel transform converted to rect transform.
                RectTransform panel = (RectTransform)menu.menuPanel.transform;
                // run the following statement only when whether panel rect contains the point distance units
                // along ray converted from world coordinates into panel local coordinates.
                if (panel.rect.Contains(panel.InverseTransformPoint(ray.GetPoint(distance))))
                    // return true to the caller.
                    return true;
            // finish the for each statement block.
            }
            // return false to the caller.
            return false;
        // finish the method contains any ray block.
        }

        // method: try handle any ray internal.
        // check active menus and let only the first handled menu consume the pointer interaction.
        private static bool TryHandleAnyRayInternal(Ray ray, bool pressed, bool desktop)
        // begin the method try handle any ray internal block.
        {
            // store consumed as false.
            bool consumed = false;
            // repeat this loop while i is at least 0. decrease i by one after each pass.
            for (int i = ActiveMenus.Count - 1; i >= 0; i--)
            // begin the for statement block.
            {
                // store menu as the entry at i in active menus.
                StudyTableToolMenu menu = ActiveMenus[i];
                // run the following statement only when menu is missing.
                if (menu == null)
                // begin the if statement block.
                {
                    // remove the entry at i from active menus.
                    ActiveMenus.RemoveAt(i);
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // finish the if statement block.
                }
                // run the following statement only when not (consumed) and menu: try handle ray using ray,
                // pressed, desktop.
                if (!consumed && menu.TryHandleRay(ray, pressed, desktop))
                    // set consumed to true.
                    consumed = true;
            // finish the for statement block.
            }
            // return consumed to the caller.
            return consumed;
        // finish the method try handle any ray internal block.
        }

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(PaperNoteCanvas paperNote)
        // begin the method initialize block.
        {
            // set paper to paper note.
            paper = paperNote;
            // build menu using true.
            BuildMenu(true);
        // finish the method initialize block.
        }

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(PaperNoteCanvas paperNote, Bounds tabletop)
        // begin the method initialize block.
        {
            // initialize using paper note, tabletop, true.
            Initialize(paperNote, tabletop, true);
        // finish the method initialize block.
        }

        // test seam for pointer checks that do not need an xr physics raycaster.
        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(PaperNoteCanvas paperNote, Bounds tabletop, bool includeTrackedRaycaster)
        // begin the method initialize block.
        {
            // set desk bounds to tabletop.
            deskBounds = tabletop;
            // set has desk bounds to true.
            hasDeskBounds = true;
            // set paper to paper note.
            paper = paperNote;
            // build menu using include tracked raycaster.
            BuildMenu(includeTrackedRaycaster);
        // finish the method initialize block.
        }

        // method: on enable.
        // register this component and attach the listeners it needs while it is active.
        private void OnEnable()
        // begin the method on enable block.
        {
            // run the following statement only when not (whether active menus contains this).
            if (!ActiveMenus.Contains(this))
                // append this to active menus.
                ActiveMenus.Add(this);
            // subscribe refresh selection to paper tool selection changed so it runs when the event is raised.
            PaperTool.SelectionChanged += RefreshSelection;
        // finish the method on enable block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // remove this from active menus.
            ActiveMenus.Remove(this);
            // unsubscribe refresh selection from paper tool selection changed so this object no longer
            // receives the event.
            PaperTool.SelectionChanged -= RefreshSelection;
            // set pointer feedback using no object, false.
            SetPointerFeedback(null, false);
        // finish the method on disable block.
        }

        // method: build menu.
        // create the desk world-space panel beside the paper, face it toward the seated head, and add pencil,
        // eraser, and clear-paper actions.
        private void BuildMenu(bool includeTrackedRaycaster)
        // begin the method build menu block.
        {
            // run the following statement only when menu object exists.
            if (menuObject != null)
                // leave this method now without running the remaining statements.
                return;
            // set menu object to a new game object using the text "tabletoolmenu", the type information for
            // rect transform.
            menuObject = new GameObject("TableToolMenu", typeof(RectTransform));
            // parent menu object transform under transform using local coordinates.
            menuObject.transform.SetParent(transform, false);
            // students face world -z; their right is world -x. the panel's
            // nearest edge stays outside the paper's 0.55m footprint, and its
            // bottom edge stays above the tabletop. aim the complete canvas
            // toward the intended seated head, so labels and hitboxes agree.
            // store panel position as transform position plus a new three-dimensional vector using the
            // negative of 0.45, 0.2, 0.04.
            Vector3 panelPosition = transform.position + new Vector3(-0.45f, 0.20f, 0.04f);
            // store seated head as if has desk bounds, use a new three-dimensional vector using desk bounds
            // center x, desk bounds max y plus 0.62, desk bounds max z plus 0.22, otherwise use transform
            // position plus a new three-dimensional vector using 0, 0.62, 0.62.
            Vector3 seatedHead = hasDeskBounds
                // if has desk bounds, use a new three-dimensional vector using desk bounds center x, desk
                // bounds max y plus 0.62, desk bounds max z plus 0.22, otherwise use transform position plus a
                // new three-dimensional vector using 0, 0.62, 0.62.
                ? new Vector3(deskBounds.center.x, deskBounds.max.y + 0.62f, deskBounds.max.z + 0.22f)
                // if has desk bounds, use a new three-dimensional vector using desk bounds center x, desk
                // bounds max y plus 0.62, desk bounds max z plus 0.22, otherwise use transform position plus a
                // new three-dimensional vector using 0, 0.62, 0.62.
                : transform.position + new Vector3(0f, 0.62f, 0.62f);
            // place menu object transform at panel position facing quaternion: look rotation using panel
            // position minus seated head, the upward unit vector.
            menuObject.transform.SetPositionAndRotation(panelPosition, Quaternion.LookRotation(panelPosition - seatedHead, Vector3.up));
            // set menu object transform local scale to unit scale on all three axes multiplied by 0.00075.
            menuObject.transform.localScale = Vector3.one * 0.00075f;
            // set menu canvas to a newly attached add component<canvas> component on menu object.
            menuCanvas = menuObject.AddComponent<Canvas>();
            // set menu canvas render mode to render mode world space.
            menuCanvas.renderMode = RenderMode.WorldSpace;
            // set menu canvas sorting order to 20.
            menuCanvas.sortingOrder = 20;
            // set menu canvas world camera to camera main.
            menuCanvas.worldCamera = Camera.main;
            // set a newly attached add component<canvas scaler> component on menu object ui scale mode to
            // canvas scaler scale mode constant pixel size.
            menuObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            // a newly attached add component<graphic raycaster> component on menu object.
            menuObject.AddComponent<GraphicRaycaster>();
            // run the following statement only when include tracked raycaster.
            if (includeTrackedRaycaster)
                // a newly attached add component<tracked device graphic raycaster> component on menu object.
                menuObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            // set the the rect transform component attached to this object component on menu object size delta
            // to a new two-dimensional vector using 380, 400.
            menuObject.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 400f);

            // set menu panel to study ui style: create panel using menu object transform, the text
            // "floatingtoolpanel", the zero two-dimensional vector, a new two-dimensional vector using 380,
            // 400, study ui style panel, true.
            menuPanel = StudyUiStyle.CreatePanel(menuObject.transform, "FloatingToolPanel", Vector2.zero, new Vector2(380f, 400f), StudyUiStyle.Panel, true);
            // set study ui style: create text using menu panel transform, the text "menutitle", the text
            // "drawing tools", a new two-dimensional vector using 0, 152, a new two-dimensional vector using
            // 330, 38, 27, text alignment options left, study ui style text font style to font styles bold.
            StudyUiStyle.CreateText(menuPanel.transform, "MenuTitle", "DRAWING TOOLS", new Vector2(0f, 152f), new Vector2(330f, 38f), 27f, TextAlignmentOptions.Left, StudyUiStyle.Text).fontStyle = FontStyles.Bold;
            // study ui style: create text using menu panel transform, the text "instruction", the text "choose
            // a tool, then draw / on the paper.", a new two-dimensional vector using 0, 101, a new
            // two-dimensional vector using 330, 62, 20, text alignment options left, study ui style muted.
            StudyUiStyle.CreateText(menuPanel.transform, "Instruction", "Choose a tool, then draw\non the paper.", new Vector2(0f, 101f), new Vector2(330f, 62f), 20f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            // study ui style: create text using menu panel transform, the text "toolheading", the text "tool",
            // a new two-dimensional vector using 0, 49, a new two-dimensional vector using 330, 26, 16, text
            // alignment options left, study ui style muted.
            StudyUiStyle.CreateText(menuPanel.transform, "ToolHeading", "TOOL", new Vector2(0f, 49f), new Vector2(330f, 26f), 16f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            // set pencil button to study ui style: create button using menu panel transform, the text
            // "pencil", the text "pencil", a new two-dimensional vector using the negative of 85, 0, a new
            // two-dimensional vector using 158, 62, a callback that select tool using paper tool kind pencil.
            pencilButton = StudyUiStyle.CreateButton(menuPanel.transform, "Pencil", "PENCIL", new Vector2(-85f, 0f), new Vector2(158f, 62f), () => SelectTool(PaperToolKind.Pencil));
            // set eraser button to study ui style: create button using menu panel transform, the text
            // "eraser", the text "eraser", a new two-dimensional vector using 85, 0, a new two-dimensional
            // vector using 158, 62, a callback that select tool using paper tool kind eraser.
            eraserButton = StudyUiStyle.CreateButton(menuPanel.transform, "Eraser", "ERASER", new Vector2(85f, 0f), new Vector2(158f, 62f), () => SelectTool(PaperToolKind.Eraser));
            // set selected tool text to study ui style: create text using menu panel transform, the text
            // "selectedtool", the text "pencil selected", a new two-dimensional vector using 0, the negative
            // of 59, a new two-dimensional vector using 330, 36, 21, text alignment options left, study ui
            // style accent.
            selectedToolText = StudyUiStyle.CreateText(menuPanel.transform, "SelectedTool", "Pencil selected", new Vector2(0f, -59f), new Vector2(330f, 36f), 21f, TextAlignmentOptions.Left, StudyUiStyle.Accent);
            // study ui style: create text using menu panel transform, the text "actionheading", the text
            // "actions", a new two-dimensional vector using 0, the negative of 105, a new two-dimensional
            // vector using 330, 26, 16, text alignment options left, study ui style muted.
            StudyUiStyle.CreateText(menuPanel.transform, "ActionHeading", "ACTIONS", new Vector2(0f, -105f), new Vector2(330f, 26f), 16f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            // study ui style: create button using menu panel transform, the text "clearpaper", the text "clear
            // paper", a new two-dimensional vector using 0, the negative of 152, a new two-dimensional vector
            // using 330, 58, clear paper.
            StudyUiStyle.CreateButton(menuPanel.transform, "ClearPaper", "CLEAR PAPER", new Vector2(0f, -152f), new Vector2(330f, 58f), ClearPaper);
            // refresh selection using paper tool selected kind.
            RefreshSelection(PaperTool.SelectedKind);
        // finish the method build menu block.
        }

        // method: late update.
        // show the menu only for the nearest occupied desk so multiple full panels do not clutter the room.
        private void LateUpdate()
        // begin the method late update block.
        {
            // run the following statement only when menu object is missing.
            if (menuObject == null)
                // leave this method now without running the remaining statements.
                return;
            // store camera as camera main.
            Camera camera = Camera.main;
            // run the following statement only when camera is missing.
            if (camera == null)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when menu canvas world camera is missing.
            if (menuCanvas.worldCamera == null)
                // set menu canvas world camera to camera.
                menuCanvas.worldCamera = camera;

            // only the closest occupied desk presents controls. sixteen full
            // panels otherwise obscure furniture and compete with the board.
            // store nearest as no object.
            StudyTableToolMenu nearest = null;
            // store nearest distance as 1.1 multiplied by 1.1.
            float nearestDistance = 1.1f * 1.1f;
            // visit each candidate in active menus.
            foreach (StudyTableToolMenu candidate in ActiveMenus)
            // begin the for each statement block.
            {
                // run the following statement only when candidate is missing or candidate paper is missing.
                if (candidate == null || candidate.paper == null)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // store delta as camera transform position minus candidate transform position.
                Vector3 delta = camera.transform.position - candidate.transform.position;
                // run the following statement only when delta z is less than 0.08 or the absolute value of
                // delta y is greater than 1.3.
                if (delta.z < 0.08f || Mathf.Abs(delta.y) > 1.3f)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // set delta y to 0.
                delta.y = 0f;
                // run the following statement only when the squared length of delta is less than nearest
                // distance.
                if (delta.sqrMagnitude < nearestDistance)
                // begin the if statement block.
                {
                    // set nearest distance to the squared length of delta.
                    nearestDistance = delta.sqrMagnitude;
                    // set nearest to candidate.
                    nearest = candidate;
                // finish the if statement block.
                }
            // finish the for each statement block.
            }
            // store visible as nearest equals this.
            bool visible = nearest == this;
            // run the following statement only when menu object active self differs from visible.
            if (menuObject.activeSelf != visible)
            // begin the if statement block.
            {
                // set the active state of menu object using visible.
                menuObject.SetActive(visible);
                // set pointer feedback using no object, false.
                SetPointerFeedback(null, false);
                // set direct controller click held to set desktop click held to false.
                directControllerClickHeld = desktopClickHeld = false;
            // finish the if statement block.
            }
        // finish the method late update block.
        }

        // method: try handle ray.
        // find an eligible button under the ray and invoke it only on the start of a press.
        private bool TryHandleRay(Ray ray, bool pressed, bool desktop)
        // begin the method try handle ray block.
        {
            // run the following statement only when menu object is missing or not (menu object active in
            // hierarchy).
            if (menuObject == null || !menuObject.activeInHierarchy)
                // return false to the caller.
                return false;
            // store plane as a new plane using menu object transform forward, menu object transform position.
            Plane plane = new Plane(menuObject.transform.forward, menuObject.transform.position);
            // run the following statement only when not (plane: raycast using ray, the output value distance)
            // or distance is less than 0 or distance is greater than 2.2.
            if (!plane.Raycast(ray, out float distance) || distance < 0f || distance > 2.2f)
            // begin the if statement block.
            {
                // set pointer feedback using no object, false.
                SetPointerFeedback(null, false);
                // run the following statement only when desktop.
                if (desktop) desktopClickHeld = false; else directControllerClickHeld = false;
                // return false to the caller.
                return false;
            // finish the if statement block.
            }
            // store world point as the point distance units along ray.
            Vector3 worldPoint = ray.GetPoint(distance);
            // store hit as no object.
            Button hit = null;
            // visit each button in the matching components below menu object.
            foreach (Button button in menuObject.GetComponentsInChildren<Button>(false))
            // begin the for each statement block.
            {
                // run the following statement only when not (button is active and enabled) or not (button
                // interactable).
                if (!button.isActiveAndEnabled || !button.interactable)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // store rect as button transform converted to rect transform.
                RectTransform rect = (RectTransform)button.transform;
                // run the following statement only when whether rect rect contains world point converted from
                // world coordinates into rect local coordinates.
                if (rect.rect.Contains(rect.InverseTransformPoint(worldPoint)))
                // begin the if statement block.
                {
                    // set hit to button.
                    hit = button;
                    // leave the nearest loop or switch after finding the required result.
                    break;
                // finish the if statement block.
                }
            // finish the for each statement block.
            }
            // store held as if desktop, use desktop click held, otherwise use direct controller click held.
            bool held = desktop ? desktopClickHeld : directControllerClickHeld;
            // set pointer feedback using hit, pressed.
            SetPointerFeedback(hit, pressed);
            // run the following statement only when hit exists and pressed and not (held).
            if (hit != null && pressed && !held)
                // invoke hit on click.
                hit.onClick.Invoke();
            // run the following statement only when desktop.
            if (desktop) desktopClickHeld = pressed; else directControllerClickHeld = pressed;
            // store panel as menu panel transform converted to rect transform.
            RectTransform panel = (RectTransform)menuPanel.transform;
            // return whether panel rect contains world point converted from world coordinates into panel local
            // coordinates to the caller.
            return panel.rect.Contains(panel.InverseTransformPoint(worldPoint));
        // finish the method try handle ray block.
        }

        // method: set pointer feedback.
        // update button hover and press appearance, releasing feedback on the previous button.
        private void SetPointerFeedback(Button target, bool pressed)
        // begin the method set pointer feedback block.
        {
            // run the following statement only when event system current is missing.
            if (EventSystem.current == null)
                // leave this method now without running the remaining statements.
                return;
            // store pointer as a new pointer event data using event system current.
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            // run the following statement only when hovered button differs from target and hovered button
            // exists.
            if (hoveredButton != target && hoveredButton != null)
            // begin the if statement block.
            {
                // hovered button: on pointer up using pointer.
                hoveredButton.OnPointerUp(pointer);
                // hovered button: on pointer exit using pointer.
                hoveredButton.OnPointerExit(pointer);
            // finish the if statement block.
            }
            // run the following statement only when target exists.
            if (target != null)
            // begin the if statement block.
            {
                // run the following statement only when hovered button differs from target.
                if (hoveredButton != target)
                    // target: on pointer enter using pointer.
                    target.OnPointerEnter(pointer);
                // run the following statement only when pressed.
                if (pressed)
                    // target: on pointer down using pointer.
                    target.OnPointerDown(pointer);
                // handle the alternative when the preceding condition is false.
                else
                    // target: on pointer up using pointer.
                    target.OnPointerUp(pointer);
            // finish the if statement block.
            }
            // set hovered button to target.
            hoveredButton = target;
        // finish the method set pointer feedback block.
        }

        // method: select tool.
        // change the shared paper tool and refresh this desk menu.
        private void SelectTool(PaperToolKind tool)
        // begin the method select tool block.
        {
            // paper tool: select using tool.
            PaperTool.Select(tool);
            // refresh selection using tool.
            RefreshSelection(tool);
        // finish the method select tool block.
        }

        // method: clear paper.
        // clear only this menu paper and request a short right-controller vibration.
        private void ClearPaper()
        // begin the method clear paper block.
        {
            // run the following statement only when paper exists.
            if (paper != null)
                // paper: clear note.
                paper.ClearNote();
            // request a short right-controller vibration if the device supports it.
            ControllerHaptics.PulseRightController();
        // finish the method clear paper block.
        }

        // method: refresh selection.
        // show the selected paper tool in the label and both tool-button states.
        private void RefreshSelection(PaperToolKind tool)
        // begin the method refresh selection block.
        {
            // run the following statement only when selected tool text exists.
            if (selectedToolText != null)
                // set selected tool text text to if tool equals paper tool kind pencil, use the text "pencil
                // selected", otherwise use the text "eraser selected".
                selectedToolText.text = tool == PaperToolKind.Pencil ? "Pencil selected" : "Eraser selected";
            // refresh tool button using pencil button, tool equals paper tool kind pencil, the text "pencil".
            RefreshToolButton(pencilButton, tool == PaperToolKind.Pencil, "PENCIL");
            // refresh tool button using eraser button, tool equals paper tool kind eraser, the text "eraser".
            RefreshToolButton(eraserButton, tool == PaperToolKind.Eraser, "ERASER");
        // finish the method refresh selection block.
        }

        // method: refresh tool button.
        // style a pencil or eraser button and add the active label when selected.
        private static void RefreshToolButton(Button button, bool selected, string label)
        // begin the method refresh tool button block.
        {
            // run the following statement only when button is missing.
            if (button == null)
                // leave this method now without running the remaining statements.
                return;
            // study ui style: style button using button, selected.
            StudyUiStyle.StyleButton(button, selected);
            // store text as the requested component in the children of button.
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            // set text text to if selected, use label plus the text " / <size=13>active</size>", otherwise use
            // label.
            text.text = selected ? label + "\n<size=13>ACTIVE</size>" : label;
        // finish the method refresh tool button block.
        }
    // finish the class study table tool menu block.
    }
// finish the namespace block.
}
