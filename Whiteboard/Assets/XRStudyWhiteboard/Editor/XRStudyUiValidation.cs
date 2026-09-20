using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard.Editor
{
    /// <summary>Checks runtime icon dependencies and desktop UI press boundaries.</summary>
    public static class XRStudyUiValidation
    {
        [MenuItem("Tools/XR Study Whiteboard/Validate Study Controls", priority = 14)]
        public static void ValidateStudyControls()
        {
            // Keep the temporary hierarchy in the active scene. A
            // HideAndDontSave root can have an invalid PhysicsScene while
            // TrackedDeviceGraphicRaycaster runs Awake in Play Mode.
            GameObject root = new GameObject("Study UI validation", typeof(RectTransform), typeof(Canvas));
            try
            {
                ValidateSessionIcons(root.transform);
                ValidateTablePointer(root.transform);
                Debug.Log("Study UI validation passed: session icons supply CanvasRenderer and valid geometry; table hover, one click per press, release/repress, and disabled controls.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateSessionIcons(Transform parent)
        {
            // Add the graphic alone, as dynamic UI builders do. It must
            // provide its native renderer without relying on prefab setup.
            GameObject iconObject = new GameObject("StateIcon", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);
            StudySessionIcon icon = iconObject.AddComponent<StudySessionIcon>();
            Require(iconObject.GetComponent<CanvasRenderer>() != null,
                "A dynamically created session icon has a CanvasRenderer");
            icon.rectTransform.sizeDelta = new Vector2(16f, 16f);
            MethodInfo populate = typeof(StudySessionIcon).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);
            Require(populate != null, "Session icon geometry callback exists");
            foreach (StudySessionIcon.Kind kind in Enum.GetValues(typeof(StudySessionIcon.Kind)))
            {
                icon.SetKind(kind);
                using (VertexHelper vertices = new VertexHelper())
                {
                    populate.Invoke(icon, new object[] { vertices });
                    Require(vertices.currentVertCount >= 3 && vertices.currentIndexCount >= 3,
                        kind + " has visible triangle geometry");
                }
                // Uses Graphic's actual renderer path, which previously
                // threw MissingComponentException on the first repaint.
                icon.Rebuild(CanvasUpdate.PreRender);
            }
        }

        private static void ValidateTablePointer(Transform parent)
        {
            GameObject menuObject = new GameObject("Table controls");
            menuObject.transform.SetParent(parent, false);
            StudyTableToolMenu menu = menuObject.AddComponent<StudyTableToolMenu>();
            menu.Initialize(null, new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 0.7f)), false);
            Button button = menuObject.transform.Find("TableToolMenu/FloatingToolPanel/Pencil").GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);
            Transform panel = menuObject.transform.Find("TableToolMenu");
            Ray ray = new Ray(button.transform.position - panel.forward * 0.7f, panel.forward);
            MethodInfo handleRay = typeof(StudyTableToolMenu).GetMethod("TryHandleRay", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(handleRay != null, "Table pointer handler exists");

            Require(HandleTablePointer(handleRay, menu, ray, false) && clicks == 0,
                "Hover consumes the panel without invoking a click");
            HandleTablePointer(handleRay, menu, ray, true);
            HandleTablePointer(handleRay, menu, ray, true);
            Require(clicks == 1, "A held desktop press invokes exactly once");
            HandleTablePointer(handleRay, menu, ray, false);
            HandleTablePointer(handleRay, menu, ray, true);
            Require(clicks == 2, "Releasing then pressing permits the next click");
            HandleTablePointer(handleRay, menu, ray, false);
            button.interactable = false;
            Require(HandleTablePointer(handleRay, menu, ray, true) && clicks == 2,
                "A disabled button consumes the panel without invoking its action");
            HandleTablePointer(handleRay, menu, ray, false);
        }

        private static bool HandleTablePointer(MethodInfo method, StudyTableToolMenu menu, Ray ray, bool pressed)
        {
            return (bool)method.Invoke(menu, new object[] { ray, pressed, true });
        }

        private static void Require(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException("Study UI validation failed: " + label);
        }
    }
}
