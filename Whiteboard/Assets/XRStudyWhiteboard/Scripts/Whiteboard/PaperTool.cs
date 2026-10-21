// script overview: this script stores the pencil or eraser selection used by the desk papers. menus call
// select and receive a notification when the choice changes. compatibility handlers also recognise held
// physical tools in older scenes. the current generated classroom uses fixed paper and menu-selected tools.
// paper tool state is separate from the whiteboard tool state.

// search: paper writing | pencil | eraser | shared paper tool | old grab tools.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in system available to this script.
using System;
// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make the types in unity engine.xr.interaction.toolkit available to this script.
using UnityEngine.XR.Interaction.Toolkit;
// make the types in unity engine.xr.interaction.toolkit.interactables available to this script.
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // define the named paper tool kind choices used instead of unexplained numbers.
    public enum PaperToolKind
    // begin the enum paper tool kind block.
    {
        // add pencil as one of the available choices.
        Pencil,
        // add eraser as one of the available choices.
        Eraser
    // finish the enum paper tool kind block.
    }

    // stores the selected paper tool. physical tools are still supported for
    // old scenes, but the classroom now uses the per-table floating menu so
    // no pencil or eraser has to sit on the desk.
    // declare the paper tool class based on unity behaviour.
    public sealed class PaperTool : MonoBehaviour
    // begin the class paper tool block.
    {
        // keep active tools as list<paper tool>, initially a new list<paper tool>.
        private static readonly List<PaperTool> ActiveTools = new List<PaperTool>();
        // keep the pencil or eraser selection shared across desk papers.
        // selected kind: the paper pencil or eraser choice shared across desks. keep selected kind as paper
        // tool kind, initially paper tool kind pencil.
        private static PaperToolKind selectedKind = PaperToolKind.Pencil;

        // keep kind as paper tool kind, initially paper tool kind pencil. allow unity to save and configure
        // this field in the inspector.
        [SerializeField] private PaperToolKind kind = PaperToolKind.Pencil;
        // keep grab interactable as xr grab interactable for later use. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private XRGrabInteractable grabInteractable;

        // expose kind as kind.
        public PaperToolKind Kind => kind;
        // expose the is held property for reading while keeping writes inside this class.
        public bool IsHeld { get; private set; }
        // expose selected kind as selected kind.
        public static PaperToolKind SelectedKind => selectedKind;

        // declare selection changed so other components can subscribe to notifications.
        public static event Action<PaperToolKind> SelectionChanged;

        // expose is pencil active as selected kind equals paper tool kind pencil or has held tool using paper
        // tool kind pencil.
        public static bool IsPencilActive => selectedKind == PaperToolKind.Pencil || HasHeldTool(PaperToolKind.Pencil);
        // expose is eraser active as selected kind equals paper tool kind eraser or has held tool using paper
        // tool kind eraser.
        public static bool IsEraserActive => selectedKind == PaperToolKind.Eraser || HasHeldTool(PaperToolKind.Eraser);

        // expose the is any eraser held property.
        public static bool IsAnyEraserHeld
        // begin the accessor list block.
        {
            // return is eraser active from this property accessor.
            get => IsEraserActive;
        // finish the accessor list block.
        }

        // expose the is any pencil held property.
        public static bool IsAnyPencilHeld
        // begin the accessor list block.
        {
            // return is pencil active from this property accessor.
            get => IsPencilActive;
        // finish the accessor list block.
        }

        // method: select.
        // set the shared paper pencil or eraser choice, notify menus and papers, and request a short
        // vibration.
        public static void Select(PaperToolKind tool)
        // begin the method select block.
        {
            // run the following statement only when selected kind equals tool.
            if (selectedKind == tool)
            // begin the if statement block.
            {
                // request a short right-controller vibration if the device supports it.
                ControllerHaptics.PulseRightController();
                // if selection changed exists, schedule selected kind after seconds.
                SelectionChanged?.Invoke(selectedKind);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // set selected kind to tool.
            selectedKind = tool;
            // if selection changed exists, schedule selected kind after seconds.
            SelectionChanged?.Invoke(selectedKind);
            // request a short right-controller vibration if the device supports it.
            ControllerHaptics.PulseRightController();
        // finish the method select block.
        }

        // method: has held tool.
        // check older physical tool instances for a held pencil or eraser while removing stale entries.
        private static bool HasHeldTool(PaperToolKind tool)
        // begin the method has held tool block.
        {
            // repeat this loop while i is at least 0. decrease i by one after each pass.
            for (int i = ActiveTools.Count - 1; i >= 0; i--)
            // begin the for statement block.
            {
                // store active tool as the entry at i in active tools.
                PaperTool activeTool = ActiveTools[i];
                // run the following statement only when active tool is missing.
                if (activeTool == null)
                // begin the if statement block.
                {
                    // remove the entry at i from active tools.
                    ActiveTools.RemoveAt(i);
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // finish the if statement block.
                }

                // run the following statement only when active tool is held and active tool kind equals tool.
                if (activeTool.IsHeld && activeTool.Kind == tool)
                    // return true to the caller.
                    return true;
            // finish the for statement block.
            }

            // return false to the caller.
            return false;
        // finish the method has held tool block.
        }

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(PaperToolKind toolKind, XRGrabInteractable interactable)
        // begin the method initialize block.
        {
            // set kind to tool kind.
            kind = toolKind;
            // set grab interactable to interactable.
            grabInteractable = interactable;
            // call subscribe: attach grab and release listeners once, supporting physical tools in older
            // scenes.
            Subscribe();
        // finish the method initialize block.
        }

        // method: on enable.
        // register this component and attach the listeners it needs while it is active.
        private void OnEnable()
        // begin the method on enable block.
        {
            // run the following statement only when not (whether active tools contains this).
            if (!ActiveTools.Contains(this))
                // append this to active tools.
                ActiveTools.Add(this);
            // call subscribe: attach grab and release listeners once, supporting physical tools in older
            // scenes.
            Subscribe();
        // finish the method on enable block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // call unsubscribe: remove this component from the events it previously listened to, preventing
            // stale callbacks.
            Unsubscribe();
            // remove this from active tools.
            ActiveTools.Remove(this);
            // set is held to false.
            IsHeld = false;
        // finish the method on disable block.
        }

        // method: subscribe.
        // attach grab and release listeners once, supporting physical tools in older scenes.
        private void Subscribe()
        // begin the method subscribe block.
        {
            // run the following statement only when grab interactable is missing.
            if (grabInteractable == null)
                // set grab interactable to the xr grab interactable component attached to this object.
                grabInteractable = GetComponent<XRGrabInteractable>();
            // run the following statement only when grab interactable is missing.
            if (grabInteractable == null)
                // leave this method now without running the remaining statements.
                return;

            // remove the on select entered callback from grab interactable select entered.
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            // remove the on select exited callback from grab interactable select exited.
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
            // register on select entered as a callback on grab interactable select entered.
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            // register on select exited as a callback on grab interactable select exited.
            grabInteractable.selectExited.AddListener(OnSelectExited);
        // finish the method subscribe block.
        }

        // method: unsubscribe.
        // remove this component from the events it previously listened to, preventing stale callbacks.
        private void Unsubscribe()
        // begin the method unsubscribe block.
        {
            // run the following statement only when grab interactable is missing.
            if (grabInteractable == null)
                // leave this method now without running the remaining statements.
                return;

            // remove the on select entered callback from grab interactable select entered.
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            // remove the on select exited callback from grab interactable select exited.
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        // finish the method unsubscribe block.
        }

        // method: on select entered.
        // mark this legacy physical tool as held.
        private void OnSelectEntered(SelectEnterEventArgs args)
        // begin the method on select entered block.
        {
            // set is held to true.
            IsHeld = true;
        // finish the method on select entered block.
        }

        // method: on select exited.
        // mark this legacy physical tool as released.
        private void OnSelectExited(SelectExitEventArgs args)
        // begin the method on select exited block.
        {
            // set is held to false.
            IsHeld = false;
        // finish the method on select exited block.
        }
    // finish the class paper tool block.
    }
// finish the namespace block.
}
