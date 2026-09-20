using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Connects the project's existing official XRI action asset to the world
    /// space UI module at runtime. This keeps UI selection working even when
    /// the scene is rebuilt by the editor tool.
    /// </summary>
    public sealed class XRUIInputSetup : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private XRUIInputModule inputModule;

        private void Awake()
        {
            // menghubungkan action xri ke ui world space.
            if (inputModule == null)
                inputModule = GetComponent<XRUIInputModule>();
            if (actions == null || inputModule == null)
                return;

            inputModule.enableXRInput = true;
            // EditorDesktopInteractor dispatches the mouse to world UI once.
            // Keep XR rays on the official input module without a second click.
            inputModule.enableMouseInput = false;
            inputModule.enableBuiltinActionsAsFallback = true;
            inputModule.pointAction = Reference("XRI UI/Point");
            inputModule.leftClickAction = Reference("XRI UI/Click");
            inputModule.middleClickAction = Reference("XRI UI/MiddleClick");
            inputModule.rightClickAction = Reference("XRI UI/RightClick");
            inputModule.scrollWheelAction = Reference("XRI UI/ScrollWheel");
            inputModule.navigateAction = Reference("XRI UI/Navigate");
            inputModule.submitAction = Reference("XRI UI/Submit");
            inputModule.cancelAction = Reference("XRI UI/Cancel");
        }

        public void Initialize(InputActionAsset inputActions, XRUIInputModule module)
        {
            actions = inputActions;
            inputModule = module;
        }

        private InputActionReference Reference(string actionName)
        {
            InputAction action = actions.FindAction(actionName, false);
            return action == null ? null : InputActionReference.Create(action);
        }
    }
}
