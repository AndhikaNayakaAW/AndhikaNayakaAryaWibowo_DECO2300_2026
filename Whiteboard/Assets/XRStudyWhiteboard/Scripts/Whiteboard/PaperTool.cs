using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRStudyWhiteboard
{
    public enum PaperToolKind
    {
        Pencil,
        Eraser
    }

    /// <summary>
    /// Stores the selected paper tool. Physical tools are still supported for
    /// old scenes, but the classroom now uses the per-table floating menu so
    /// no pencil or eraser has to sit on the desk.
    /// </summary>
    public sealed class PaperTool : MonoBehaviour
    {
        private static readonly List<PaperTool> ActiveTools = new List<PaperTool>();
        private static PaperToolKind selectedKind = PaperToolKind.Pencil;

        [SerializeField] private PaperToolKind kind = PaperToolKind.Pencil;
        [SerializeField] private XRGrabInteractable grabInteractable;

        public PaperToolKind Kind => kind;
        public bool IsHeld { get; private set; }
        public static PaperToolKind SelectedKind => selectedKind;

        public static event Action<PaperToolKind> SelectionChanged;

        public static bool IsPencilActive => selectedKind == PaperToolKind.Pencil || HasHeldTool(PaperToolKind.Pencil);
        public static bool IsEraserActive => selectedKind == PaperToolKind.Eraser || HasHeldTool(PaperToolKind.Eraser);

        public static bool IsAnyEraserHeld
        {
            get => IsEraserActive;
        }

        public static bool IsAnyPencilHeld
        {
            get => IsPencilActive;
        }

        public static void Select(PaperToolKind tool)
        {
            if (selectedKind == tool)
            {
                ControllerHaptics.PulseRightController();
                SelectionChanged?.Invoke(selectedKind);
                return;
            }

            selectedKind = tool;
            SelectionChanged?.Invoke(selectedKind);
            ControllerHaptics.PulseRightController();
        }

        private static bool HasHeldTool(PaperToolKind tool)
        {
            for (int i = ActiveTools.Count - 1; i >= 0; i--)
            {
                PaperTool activeTool = ActiveTools[i];
                if (activeTool == null)
                {
                    ActiveTools.RemoveAt(i);
                    continue;
                }

                if (activeTool.IsHeld && activeTool.Kind == tool)
                    return true;
            }

            return false;
        }

        public void Initialize(PaperToolKind toolKind, XRGrabInteractable interactable)
        {
            kind = toolKind;
            grabInteractable = interactable;
            Subscribe();
        }

        private void OnEnable()
        {
            if (!ActiveTools.Contains(this))
                ActiveTools.Add(this);
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ActiveTools.Remove(this);
            IsHeld = false;
        }

        private void Subscribe()
        {
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
                return;

            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        private void Unsubscribe()
        {
            if (grabInteractable == null)
                return;

            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            IsHeld = true;
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            IsHeld = false;
        }
    }
}
