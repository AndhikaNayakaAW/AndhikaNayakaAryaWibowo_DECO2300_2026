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
    /// Tracks whether a physical pencil or eraser is currently held. The
    /// writing interactor uses this so an eraser grabbed from under a desk
    /// really erases the paper instead of only looking decorative.
    /// </summary>
    public sealed class PaperTool : MonoBehaviour
    {
        private static readonly List<PaperTool> ActiveTools = new List<PaperTool>();

        [SerializeField] private PaperToolKind kind = PaperToolKind.Pencil;
        [SerializeField] private XRGrabInteractable grabInteractable;

        public PaperToolKind Kind => kind;
        public bool IsHeld { get; private set; }

        public static bool IsAnyEraserHeld
        {
            get
            {
                for (int i = ActiveTools.Count - 1; i >= 0; i--)
                {
                    PaperTool tool = ActiveTools[i];
                    if (tool == null)
                    {
                        ActiveTools.RemoveAt(i);
                        continue;
                    }

                    if (tool.IsHeld && tool.Kind == PaperToolKind.Eraser)
                        return true;
                }

                return false;
            }
        }

        public static bool IsAnyPencilHeld
        {
            get
            {
                for (int i = ActiveTools.Count - 1; i >= 0; i--)
                {
                    PaperTool tool = ActiveTools[i];
                    if (tool == null)
                    {
                        ActiveTools.RemoveAt(i);
                        continue;
                    }

                    if (tool.IsHeld && tool.Kind == PaperToolKind.Pencil)
                        return true;
                }

                return false;
            }
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
