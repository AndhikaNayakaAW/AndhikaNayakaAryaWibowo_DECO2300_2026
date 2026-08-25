using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using XRStudyWhiteboard;

namespace XRStudyWhiteboard.Editor
{
    /// <summary>
    /// One-click project and scene audit for the whiteboard. It checks the
    /// references that are otherwise easy to lose when rebuilding a scene.
    /// </summary>
    public static class XRStudyWhiteboardValidation
    {
        private const string ScenePath = "Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity";
        private const string ImportedClassroomPath = "Assets/XRStudyWhiteboard/Art/Classroom/classroom.fbx";
        private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
        private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab";
        private const string SimulatorControlsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator Controls.inputactions";
        private const string ControllerControlsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Controller Controls.inputactions";
        private const string HandControlsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Hand Controls.inputactions";
        private const string OpenXRSettingsPath = "Assets/XR/Settings/OpenXR Package Settings.asset";
        private const string SimulatorObjectName = "XR Device Simulator (Editor Test)";

        [MenuItem("Tools/XR Study Whiteboard/Validate Classroom Setup", priority = 10)]
        public static void ValidateClassroom()
        {
            int errors = 0;
            int checks = 0;

            CheckFile(ScenePath, ref checks, ref errors);
            CheckFile(ImportedClassroomPath, ref checks, ref errors);
            CheckFile(InputActionsPath, ref checks, ref errors);
            CheckFile(SimulatorPrefabPath, ref checks, ref errors);
            CheckFile(SimulatorControlsPath, ref checks, ref errors);
            CheckFile(ControllerControlsPath, ref checks, ref errors);
            CheckFile(HandControlsPath, ref checks, ref errors);
            CheckFile(OpenXRSettingsPath, ref checks, ref errors);
            CheckOpenXRFeature("OculusTouchControllerProfile Android", ref checks, ref errors);
            CheckOpenXRFeature("MetaQuestTouchPlusControllerProfile Android", ref checks, ref errors);
            CheckOpenXRFeature("MetaQuestFeature Android", ref checks, ref errors);
            CheckOpenXRFeature("HandTracking Android", ref checks, ref errors);
            CheckOpenXRFeature("MetaHandTrackingAim Android", ref checks, ref errors);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Check(scene.IsValid(), "Classroom scene opened", ref checks, ref errors);
            if (scene.IsValid())
            {
                ValidateScene(scene, ref checks, ref errors);
            }

            RunDrawingSmokeTests(ref checks, ref errors);
            RunTableToolMenuSmokeTests(ref checks, ref errors);

            if (errors == 0)
            {
                Debug.Log("XR Study Whiteboard validation passed (" + checks + " checks). Quest hardware testing is still required for live tracking and haptics.");
                return;
            }

            string message = "XR Study Whiteboard validation found " + errors + " issue(s). See the Console for details.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        [MenuItem("Tools/XR Study Whiteboard/Build Android Validation APK", priority = 11)]
        public static void BuildAndroidPlayerValidation()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException("Unity could not switch the active build target to Android.");
            }

            string outputPath = "/tmp/XRStudyWhiteboard-validation.apk";
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android validation build failed: " + report.summary.result + ". See the Unity build log.");

            Debug.Log("Android validation build passed: " + outputPath);
        }

        [MenuItem("Tools/XR Study Whiteboard/Add Device Simulator For Editor Test", priority = 20)]
        public static void AddDeviceSimulatorForEditorTest()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("The classroom scene does not exist: " + ScenePath);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            if (FindSceneObject(SimulatorObjectName, activeScene) != null)
            {
                Debug.Log("The editor device simulator is already in the classroom scene.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("The XR Device Simulator prefab could not be loaded: " + SimulatorPrefabPath);
                return;
            }

            GameObject simulator = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (simulator == null)
            {
                Debug.LogError("Unity could not instantiate the XR Device Simulator prefab.");
                return;
            }

            simulator.name = SimulatorObjectName;
            SceneManager.MoveGameObjectToScene(simulator, activeScene);
            Undo.RegisterCreatedObjectUndo(simulator, "Add XR Device Simulator for editor test");
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("XR Device Simulator added. Press Play to test with keyboard and mouse; remove it before an Android build.");
        }

        [MenuItem("Tools/XR Study Whiteboard/Log Imported Classroom Geometry", priority = 22)]
        public static void LogImportedClassroomGeometry()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedClassroomPath);
            if (asset == null)
            {
                Debug.LogError("The imported classroom FBX could not be loaded: " + ImportedClassroomPath);
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(asset);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Debug.Log("Imported classroom geometry report: " + renderers.Length + " renderers.");
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    Material material = renderer.sharedMaterial;
                    string materialName = material != null ? material.name : "<none>";
                    Debug.LogFormat(
                        "FBX renderer '{0}' enabled={1} center={2} size={3} material='{4}' shader='{5}'",
                        GetRelativePath(instance.transform, renderer.transform),
                        renderer.enabled,
                        renderer.bounds.center,
                        renderer.bounds.size,
                        materialName,
                        material != null && material.shader != null ? material.shader.name : "<none>");

                    string rendererName = renderer.transform.name.ToLowerInvariant();
                    if (rendererName == "table" || rendererName == "table.001" || rendererName == "table2")
                        LogTableMeshComponents(instance.transform, renderer);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [MenuItem("Tools/XR Study Whiteboard/Remove Device Simulator For Editor Test", priority = 21)]
        public static void RemoveDeviceSimulatorForEditorTest()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject simulator = FindSceneObject(SimulatorObjectName, scene);
            if (simulator == null)
            {
                Debug.Log("No editor device simulator was found in the active scene.");
                return;
            }

            Undo.DestroyObjectImmediate(simulator);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("XR Device Simulator removed from the classroom scene.");
        }

        private static void ValidateScene(Scene scene, ref int checks, ref int errors)
        {
            CheckMissingScripts(scene, ref checks, ref errors);

            WhiteboardCanvas canvas = FindComponent<WhiteboardCanvas>(scene);
            WhiteboardDrawer drawer = FindComponent<WhiteboardDrawer>(scene);
            XRStudyWhiteboardManager manager = FindComponent<XRStudyWhiteboardManager>(scene);
            XRWhiteboardInteractor controllerInteractor = FindComponent<XRWhiteboardInteractor>(scene);
            HandWhiteboardInteractor handInteractor = FindComponent<HandWhiteboardInteractor>(scene);
            XRStudyRoomLocomotion locomotion = FindComponent<XRStudyRoomLocomotion>(scene);
            ClassroomAssetRuntimeSetup classroomSetup = FindComponent<ClassroomAssetRuntimeSetup>(scene);

            Check(canvas != null, "WhiteboardCanvas exists", ref checks, ref errors);
            Check(drawer != null, "WhiteboardDrawer exists", ref checks, ref errors);
            Check(manager != null, "XRStudyWhiteboardManager exists", ref checks, ref errors);
            Check(controllerInteractor != null, "XRWhiteboardInteractor exists", ref checks, ref errors);
            Check(handInteractor != null, "HandWhiteboardInteractor exists", ref checks, ref errors);
            Check(locomotion != null, "XR study room locomotion exists", ref checks, ref errors);
            Check(classroomSetup != null, "Imported classroom runtime setup exists", ref checks, ref errors);

            if (classroomSetup != null)
            {
                CheckReference(classroomSetup, "classroomInstance", "Imported classroom scene instance reference", ref checks, ref errors);
                CheckReference(classroomSetup, "whiteboardCanvas", "Imported classroom whiteboard reference", ref checks, ref errors);
            }

            if (canvas != null)
            {
                CheckReference(canvas, "surfaceRenderer", "Whiteboard surface renderer", ref checks, ref errors);
                CheckReference(canvas, "surfaceCollider", "Whiteboard surface collider", ref checks, ref errors);
                CheckReference(canvas, "cursor", "Whiteboard drawing cursor", ref checks, ref errors);
                CheckReference(canvas, "cursorRenderer", "Whiteboard cursor renderer", ref checks, ref errors);
            }

            if (drawer != null)
            {
                CheckReference(drawer, "canvas", "Drawer canvas reference", ref checks, ref errors);
                CheckReference(drawer, "manager", "Drawer manager reference", ref checks, ref errors);
            }

            if (manager != null)
            {
                CheckReference(manager, "canvas", "Manager canvas reference", ref checks, ref errors);
                CheckReference(manager, "statusDisplay", "Manager status display reference", ref checks, ref errors);
                CheckReference(manager, "clearConfirmation", "Manager clear confirmation reference", ref checks, ref errors);
            }

            if (controllerInteractor != null)
            {
                CheckReference(controllerInteractor, "canvas", "Controller canvas reference", ref checks, ref errors);
                CheckReference(controllerInteractor, "drawer", "Controller drawer reference", ref checks, ref errors);
                CheckReference(controllerInteractor, "rayOrigin", "Controller ray origin reference", ref checks, ref errors);
                Check(FindComponent<XRRayInteractor>(scene) != null, "XRI controller ray interactor exists", ref checks, ref errors);
            }

            int handInteractors = FindComponents<HandWhiteboardInteractor>(scene).Length;
            Check(handInteractors == 1, "Exactly one hand whiteboard interactor exists", ref checks, ref errors);

            XRInteractionManager[] interactionManagers = FindComponents<XRInteractionManager>(scene);
            Check(interactionManagers.Length == 1, "Exactly one XR Interaction Manager exists", ref checks, ref errors);
            Check(FindSceneObject("XR Origin", scene) != null, "XR Origin exists", ref checks, ref errors);

            EventSystem[] eventSystems = FindComponents<EventSystem>(scene);
            XRUIInputModule[] inputModules = FindComponents<XRUIInputModule>(scene);
            Check(eventSystems.Length == 1, "Exactly one EventSystem exists", ref checks, ref errors);
            Check(inputModules.Length == 1, "Exactly one XR UI Input Module exists", ref checks, ref errors);
            Check(FindComponent<XRUIInputSetup>(scene) != null, "XR UI action setup exists", ref checks, ref errors);
            Check(FindComponents<TrackedDeviceGraphicRaycaster>(scene).Length >= 1, "Tracked-device UI raycaster exists", ref checks, ref errors);

            Canvas[] canvases = FindComponents<Canvas>(scene);
            bool hasWorldSpaceCanvas = false;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.WorldSpace)
                {
                    hasWorldSpaceCanvas = true;
                    break;
                }
            }
            Check(hasWorldSpaceCanvas, "World-space whiteboard UI canvas exists", ref checks, ref errors);

            Check(FindComponent<XRGrabInteractable>(scene) != null, "Grab marker interactable exists", ref checks, ref errors);
            Check(FindComponent<TeleportationArea>(scene) != null, "Teleportation floor exists", ref checks, ref errors);
            Check(FindComponents<Camera>(scene).Length >= 1, "XR camera exists", ref checks, ref errors);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Check(actions != null, "XRI default input actions asset loads", ref checks, ref errors);
            if (actions != null)
            {
                string[] requiredActions =
                {
                    "XRI UI/Point",
                    "XRI UI/Click",
                    "XRI Right Interaction/Select",
                    "XRI Right Interaction/Activate",
                    "XRI Right Locomotion/Move",
                    "XRI Right Locomotion/Turn",
                    "XRI Left Locomotion/Move",
                    "XRI Left Locomotion/Turn"
                };

                for (int i = 0; i < requiredActions.Length; i++)
                    Check(actions.FindAction(requiredActions[i], false) != null, "Input action exists: " + requiredActions[i], ref checks, ref errors);
            }
        }

        private static void CheckMissingScripts(Scene scene, ref int checks, ref int errors)
        {
            GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].scene != scene)
                    continue;

                Component[] components = objects[i].GetComponents<Component>();
                for (int j = 0; j < components.Length; j++)
                {
                    Check(components[j] != null, "No missing script on " + objects[i].name, ref checks, ref errors);
                }
            }
        }

        private static void CheckOpenXRFeature(string featureName, ref int checks, ref int errors)
        {
            if (!File.Exists(OpenXRSettingsPath))
            {
                Check(false, "OpenXR feature enabled: " + featureName, ref checks, ref errors);
                return;
            }

            string text = File.ReadAllText(OpenXRSettingsPath);
            int featureIndex = text.IndexOf("m_Name: " + featureName, StringComparison.Ordinal);
            int nextBlock = featureIndex < 0 ? -1 : text.IndexOf("--- !u!114", featureIndex + 1, StringComparison.Ordinal);
            bool enabled = featureIndex >= 0 && text.Substring(featureIndex, (nextBlock < 0 ? text.Length : nextBlock) - featureIndex).Contains("m_enabled: 1");
            Check(enabled, "OpenXR feature enabled: " + featureName, ref checks, ref errors);
        }

        private static void CheckFile(string path, ref int checks, ref int errors)
        {
            Check(File.Exists(path), "Resource exists: " + path, ref checks, ref errors);
        }

        private static void CheckReference(UnityEngine.Object target, string propertyName, string label, ref int checks, ref int errors)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Check(property != null && property.objectReferenceValue != null, label, ref checks, ref errors);
        }

        private static void Check(bool condition, string label, ref int checks, ref int errors)
        {
            checks++;
            if (condition)
                return;

            errors++;
            Debug.LogError("[XR Study Whiteboard] FAILED: " + label);
        }

        private static void RunDrawingSmokeTests(ref int checks, ref int errors)
        {
            GameObject boardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject paperObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardObject.hideFlags = HideFlags.HideAndDontSave;
            paperObject.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                WhiteboardCanvas board = boardObject.AddComponent<WhiteboardCanvas>();
                board.ConfigureBoardWorldSize(new Vector2(3f, 1.5f));
                board.InitializeSurface();
                DrawCircle(board);
                InvokePrivate(board, "ApplyTexture");
                Texture2D boardTexture = GetPrivateField<Texture2D>(board, "boardTexture");
                Check(HasContinuousPath(boardTexture), "Whiteboard circle smoke test is continuous", ref checks, ref errors);
                board.ClearBoard();
                Check(IsTextureBlank(boardTexture), "Whiteboard clear smoke test", ref checks, ref errors);

                PaperNoteCanvas paper = paperObject.AddComponent<PaperNoteCanvas>();
                paper.Configure(paperObject.GetComponent<Renderer>(), paperObject.GetComponent<Collider>(), new Vector2(0.55f, 0.38f));
                paper.ConfigureWritingSizes(0.009f, 0.032f);
                PaperTool.Select(PaperToolKind.Pencil);
                DrawCircle(paper);
                InvokePrivate(paper, "ApplyTexture");
                Texture2D paperTexture = GetPrivateField<Texture2D>(paper, "noteTexture");
                Check(HasContinuousPath(paperTexture), "Paper circle smoke test is continuous", ref checks, ref errors);
                PaperTool.Select(PaperToolKind.Eraser);
                paper.DrawAtUV(new Vector2(0.5f, 0.2f), true);
                paper.DrawAtUV(new Vector2(0.5f, 0.8f), true);
                paper.ClearNote();
                Check(IsTextureBlank(paperTexture), "Paper clear smoke test", ref checks, ref errors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boardObject);
                UnityEngine.Object.DestroyImmediate(paperObject);
            }
        }

        private static void RunTableToolMenuSmokeTests(ref int checks, ref int errors)
        {
            GameObject tableObject = new GameObject("Table Tool Menu Smoke Test");
            GameObject paperObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableObject.hideFlags = HideFlags.HideAndDontSave;
            paperObject.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                PaperNoteCanvas paper = paperObject.AddComponent<PaperNoteCanvas>();
                paper.Configure(
                    paperObject.GetComponent<Renderer>(),
                    paperObject.GetComponent<Collider>(),
                    new Vector2(0.55f, 0.38f));

                StudyTableToolMenu menu = tableObject.AddComponent<StudyTableToolMenu>();
                menu.Initialize(paper);
                Transform menuTransform = tableObject.transform.Find("TableToolMenu");
                Canvas menuCanvas = menuTransform != null ? menuTransform.GetComponent<Canvas>() : null;
                Check(menuTransform != null, "Table tool menu canvas is created", ref checks, ref errors);
                Check(menuCanvas != null, "Table tool menu has a world canvas", ref checks, ref errors);
                if (menuTransform != null)
                {
                    Check(Vector3.Dot(menuTransform.forward, Vector3.down) > 0.9f, "Table tool menu faces the tabletop", ref checks, ref errors);
                    Check(menuTransform.GetComponent<GraphicRaycaster>() != null, "Table tool menu has a desktop raycaster", ref checks, ref errors);
                    Check(menuTransform.GetComponent<TrackedDeviceGraphicRaycaster>() != null, "Table tool menu has a tracked-device raycaster", ref checks, ref errors);
                }

                Button[] buttons = menuTransform != null
                    ? menuTransform.GetComponentsInChildren<Button>(true)
                    : Array.Empty<Button>();
                Check(buttons.Length == 4, "Table tool menu exposes tools, pencil, eraser, and clear buttons", ref checks, ref errors);

                Transform openTransform = menuTransform != null ? menuTransform.Find("OpenToolsButton") : null;
                Transform panelTransform = menuTransform != null ? menuTransform.Find("FloatingToolPanel") : null;
                Button openButton = openTransform != null ? openTransform.GetComponent<Button>() : null;
                Check(openButton != null && panelTransform != null && !panelTransform.gameObject.activeSelf, "Table tool panel starts closed", ref checks, ref errors);

                if (openButton != null && menuTransform != null)
                {
                    RectTransform openRect = openButton.transform as RectTransform;
                    Vector3 openPoint = openRect.TransformPoint(openRect.rect.center);
                    Ray menuRay = new Ray(openPoint - menuTransform.forward * 0.5f, menuTransform.forward);
                    Check(StudyTableToolMenu.TryHandleAnyRay(menuRay, true), "Controller ray reaches the table tools button", ref checks, ref errors);
                    StudyTableToolMenu.TryHandleAnyRay(menuRay, false);
                    Check(panelTransform.gameObject.activeSelf, "Controller ray opens the table tool panel", ref checks, ref errors);
                }

                Transform eraserTransform = menuTransform != null ? menuTransform.Find("FloatingToolPanel/Eraser") : null;
                Button eraserButton = eraserTransform != null ? eraserTransform.GetComponent<Button>() : null;
                if (eraserButton != null)
                    eraserButton.onClick.Invoke();
                Check(PaperTool.SelectedKind == PaperToolKind.Eraser, "Table eraser action selects the eraser", ref checks, ref errors);
                PaperTool.Select(PaperToolKind.Pencil);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tableObject);
                UnityEngine.Object.DestroyImmediate(paperObject);
            }
        }

        private static void DrawCircle(WhiteboardCanvas board)
        {
            const int points = 64;
            Vector2 first = CirclePoint(0, points);
            board.BeginStroke(first);
            for (int i = 1; i <= points; i++)
                board.ContinueStroke(CirclePoint(i, points));
            board.EndStroke();
        }

        private static void DrawCircle(PaperNoteCanvas paper)
        {
            const int points = 64;
            Vector2 first = CirclePoint(0, points);
            paper.DrawAtUV(first, false);
            for (int i = 1; i <= points; i++)
                paper.DrawAtUV(CirclePoint(i, points), false);
            paper.EndStroke();
        }

        private static Vector2 CirclePoint(int index, int pointCount)
        {
            float angle = index / (float)pointCount * Mathf.PI * 2f;
            return new Vector2(0.5f + Mathf.Cos(angle) * 0.3f, 0.5f + Mathf.Sin(angle) * 0.3f);
        }

        private static bool HasContinuousPath(Texture2D texture)
        {
            if (texture == null)
                return false;

            const int pointCount = 64;
            for (int i = 0; i < pointCount; i++)
            {
                Vector2 uv = CirclePoint(i, pointCount);
                int x = Mathf.RoundToInt(uv.x * (texture.width - 1));
                int y = Mathf.RoundToInt(uv.y * (texture.height - 1));
                bool foundInk = false;
                for (int offsetY = -8; offsetY <= 8 && !foundInk; offsetY++)
                {
                    for (int offsetX = -8; offsetX <= 8; offsetX++)
                    {
                        int sampleX = Mathf.Clamp(x + offsetX, 0, texture.width - 1);
                        int sampleY = Mathf.Clamp(y + offsetY, 0, texture.height - 1);
                        Color32 pixel = texture.GetPixel(sampleX, sampleY);
                        if (pixel.r < 180 && pixel.g < 180 && pixel.b < 180)
                        {
                            foundInk = true;
                            break;
                        }
                    }
                }

                if (!foundInk)
                    return false;
            }

            return true;
        }

        private static bool IsTextureBlank(Texture2D texture)
        {
            if (texture == null)
                return false;

            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r < 250 || pixels[i].g < 250 || pixels[i].b < 250)
                    return false;
            }

            return true;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? (T)field.GetValue(target) : default;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(target, null);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindComponents<T>(scene);
            return components.Length == 0 ? null : components[0];
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == scene)
                    count++;
            }

            T[] sceneComponents = new T[count];
            int index = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == scene)
                    sceneComponents[index++] = components[i];
            }

            return sceneComponents;
        }

        private static GameObject FindSceneObject(string objectName, Scene scene)
        {
            GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].scene == scene && objects[i].name == objectName)
                    return objects[i];
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform child)
        {
            if (child == root)
                return root.name;

            string path = child.name;
            Transform current = child.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return root.name + "/" + path;
        }

        private static void LogTableMeshComponents(Transform root, Renderer renderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return;

            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            int[] component = new int[vertices.Length];
            for (int i = 0; i < component.Length; i++)
                component[i] = -1;

            int componentCount = 0;
            Dictionary<int, Bounds> boundsByComponent = new Dictionary<int, Bounds>();
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                int a = triangles[triangle];
                int b = triangles[triangle + 1];
                int c = triangles[triangle + 2];
                int id = component[a];
                if (id < 0)
                    id = component[b];
                if (id < 0)
                    id = component[c];
                if (id < 0)
                    id = componentCount++;

                component[a] = id;
                component[b] = id;
                component[c] = id;
                Bounds triangleBounds = TransformTriangleBounds(filter.transform, vertices[a], vertices[b], vertices[c]);
                if (boundsByComponent.TryGetValue(id, out Bounds existing))
                    existing.Encapsulate(triangleBounds);
                else
                    existing = triangleBounds;
                boundsByComponent[id] = existing;
            }

            Debug.Log("  mesh='" + mesh.name + "' vertices=" + vertices.Length + " triangles=" + (triangles.Length / 3));
            foreach (KeyValuePair<int, Bounds> pair in boundsByComponent)
            {
                Bounds bounds = pair.Value;
                if (bounds.size.x < 2.5f && bounds.size.z < 2.5f && bounds.size.y < 1.0f)
                {
                    Debug.LogFormat(
                        "  tabletop component {0}: center={1} size={2}",
                        pair.Key,
                        bounds.center,
                        bounds.size);
                }
            }
        }

        private static Bounds TransformTriangleBounds(Transform transform, Vector3 a, Vector3 b, Vector3 c)
        {
            Bounds bounds = new Bounds(transform.TransformPoint(a), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(b));
            bounds.Encapsulate(transform.TransformPoint(c));
            return bounds;
        }
    }
}
