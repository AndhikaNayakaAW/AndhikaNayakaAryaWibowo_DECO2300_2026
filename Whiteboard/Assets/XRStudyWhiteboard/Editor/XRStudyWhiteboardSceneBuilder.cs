using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XRStudyWhiteboard.Editor
{
    /// <summary>
    /// Creates the assessment scene from Unity primitives and existing official
    /// XR template assets. The operation replaces only the generated root, so
    /// it is safe to run again while iterating on layout.
    /// </summary>
    public static class XRStudyWhiteboardSceneBuilder
    {
        private const string ScenePath = "Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity";
        private const string RootName = "XRStudyClassroomRoot";
        private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
        // Use the controller rig for the classroom. It includes the physical
        // left/right controller visuals, ray interactors, grab input, and
        // locomotion bindings. The hands-only rig made desktop testing look
        // as if no controller existed and left the whiteboard ray attached
        // to a hand-tracking transform instead.
        private const string XROriginPrefabPath = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
        private const string HandsPermissionsPrefabPath = "Assets/VRTemplateAssets/Prefabs/Setup/Hands Permissions Manager.prefab";
        private const string ImportedClassroomPath = "Assets/XRStudyWhiteboard/Art/Classroom/classroom.fbx";
        private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab";
        private const string SimulatorObjectName = "XR Device Simulator (Editor Test)";

        private static readonly Color ClassroomBlue = new Color(0.08f, 0.16f, 0.24f);
        private static readonly Color ClassroomWall = new Color(0.82f, 0.86f, 0.88f);
        private static readonly Color DeskWood = new Color(0.34f, 0.20f, 0.11f);
        private static readonly Color BoardWhite = new Color(0.96f, 0.97f, 0.96f);

        [MenuItem("Tools/XR Study Whiteboard/Build - Repair Classroom", priority = 1)]
        public static void BuildClassroom()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = NewObject(RootName, null);

            Material floorMaterial = CreateMaterial("Classroom Floor", new Color(0.17f, 0.21f, 0.25f), 0.05f, 0.35f);
            Material wallMaterial = CreateMaterial("Classroom Walls", ClassroomWall, 0f, 0.2f);
            Material woodMaterial = CreateMaterial("Classroom Wood", DeskWood, 0f, 0.3f);
            Material blueMaterial = CreateMaterial("Classroom Blue", ClassroomBlue, 0f, 0.25f);
            Material boardMaterial = CreateMaterial("Whiteboard Surface", BoardWhite, 0f, 0.25f);
            Material blackMaterial = CreateMaterial("Marker Black", new Color(0.02f, 0.025f, 0.03f), 0.1f, 0.35f);

            GameObject environment = NewObject("Environment", root.transform);
            GameObject floor = CreatePrimitive("Floor", PrimitiveType.Cube, environment.transform, new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 14f), floorMaterial, Layer("Teleport"));
            CreateTeleportArea(floor);
            CreatePrimitive("Ceiling", PrimitiveType.Cube, environment.transform, new Vector3(0f, 4.5f, 0f), new Vector3(12f, 0.1f, 14f), wallMaterial, Layer("Environment"));
            CreatePrimitive("FrontWall", PrimitiveType.Cube, environment.transform, new Vector3(0f, 2.2f, -6.5f), new Vector3(12f, 4.5f, 0.1f), wallMaterial, Layer("Environment"));
            CreatePrimitive("BackWall", PrimitiveType.Cube, environment.transform, new Vector3(0f, 2.2f, 6.5f), new Vector3(12f, 4.5f, 0.1f), wallMaterial, Layer("Environment"));
            CreatePrimitive("LeftWall", PrimitiveType.Cube, environment.transform, new Vector3(-6f, 2.2f, 0f), new Vector3(0.1f, 4.5f, 14f), wallMaterial, Layer("Environment"));
            CreatePrimitive("RightWall", PrimitiveType.Cube, environment.transform, new Vector3(6f, 2.2f, 0f), new Vector3(0.1f, 4.5f, 14f), wallMaterial, Layer("Environment"));
            CreatePrimitive("FrontWallAccent", PrimitiveType.Cube, environment.transform, new Vector3(0f, 0.2f, -6.42f), new Vector3(12f, 0.4f, 0.12f), blueMaterial, Layer("Environment"));
            CreateWindow(environment.transform, wallMaterial, blueMaterial);
            CreateDoor(environment.transform, woodMaterial, blueMaterial);

            GameObject classroomFurniture = NewObject("ClassroomFurniture", root.transform);
            CreateTeacherDesk(classroomFurniture.transform, woodMaterial, blueMaterial);
            CreateStudentDesk(classroomFurniture.transform, woodMaterial, new Vector3(-3.1f, 0f, 1.8f), "StudentDesk_01");
            CreateStudentDesk(classroomFurniture.transform, woodMaterial, new Vector3(0f, 0f, 1.8f), "StudentDesk_02");
            CreateStudentDesk(classroomFurniture.transform, woodMaterial, new Vector3(3.1f, 0f, 1.8f), "StudentDesk_03");
            CreateStudentDesk(classroomFurniture.transform, woodMaterial, new Vector3(-1.55f, 0f, 4.25f), "StudentDesk_04");

            GameObject boardRoot = NewObject("StudyWhiteboard", root.transform);
            WhiteboardCanvas board = CreateWhiteboard(boardRoot.transform, boardMaterial, blueMaterial, blackMaterial);

            ClassroomAssetRuntimeSetup classroomSetup = root.AddComponent<ClassroomAssetRuntimeSetup>();
            GameObject importedClassroom = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedClassroomPath);
            GameObject importedInstance = null;
            if (importedClassroom != null)
            {
                importedInstance = PrefabUtility.InstantiatePrefab(importedClassroom) as GameObject;
                if (importedInstance != null)
                {
                    importedInstance.name = "Classroom (Imported FBX)";
                    SceneManager.MoveGameObjectToScene(importedInstance, scene);
                    importedInstance.transform.SetParent(root.transform, false);
                    DisableImportedVisualEffects(importedInstance);
                }
            }

            if (importedInstance == null)
                Debug.LogError("The imported classroom FBX could not be placed: " + ImportedClassroomPath);
            classroomSetup.SetReferences(importedInstance, board);

            GameObject systems = NewObject("Systems", root.transform);
            XRStudyWhiteboardManager manager = systems.AddComponent<XRStudyWhiteboardManager>();
            WhiteboardDrawer drawer = systems.AddComponent<WhiteboardDrawer>();
            manager.SetReferences(board, null, null);
            drawer.SetReferences(board, manager);

            GameObject xrRoot = NewObject("XR", root.transform);
            XRInteractionManager interactionManager = CreateXROrigin(xrRoot.transform, board, drawer, manager);
            XRStudyRoomLocomotion locomotion = xrRoot.AddComponent<XRStudyRoomLocomotion>();
            locomotion.SetOrigin(FindDeepChild(xrRoot.transform, "XR Origin"));
            CreateHandsPermissions(xrRoot.transform);
            CreateEventSystem(xrRoot.transform);
            CreateEditorDeviceSimulator(scene);

            WhiteboardStatusDisplay statusDisplay;
            ClearBoardConfirmation clearConfirmation;
            CreateWhiteboardUI(boardRoot.transform, manager, out statusDisplay, out clearConfirmation);
            manager.SetReferences(board, statusDisplay, clearConfirmation);

            CreateLighting(environment.transform);
            CreateDecorations(environment.transform, blueMaterial, woodMaterial);

            EnsureBuildScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("XR Study Whiteboard classroom built at " + ScenePath + ".");
        }

        [MenuItem("Tools/XR Study Whiteboard/Open Main Scene", priority = 2)]
        public static void OpenMainScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                BuildClassroom();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/XRStudyWhiteboard",
                "Assets/XRStudyWhiteboard/Art",
                "Assets/XRStudyWhiteboard/Audio",
                "Assets/XRStudyWhiteboard/Materials",
                "Assets/XRStudyWhiteboard/Prefabs",
                "Assets/XRStudyWhiteboard/Prefabs/Classroom",
                "Assets/XRStudyWhiteboard/Prefabs/Whiteboard",
                "Assets/XRStudyWhiteboard/Prefabs/UI",
                "Assets/XRStudyWhiteboard/Prefabs/XR",
                "Assets/XRStudyWhiteboard/Scenes",
                "Assets/XRStudyWhiteboard/Input"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                string folder = folders[i];
                if (AssetDatabase.IsValidFolder(folder))
                    continue;

                int slash = folder.LastIndexOf('/');
                AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
            }
        }

        private static GameObject NewObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create XR Study Whiteboard object");
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, int layer)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = scale;
            gameObject.layer = layer;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return gameObject;
        }

        private static WhiteboardCanvas CreateWhiteboard(Transform parent, Material boardMaterial, Material accentMaterial, Material markerMaterial)
        {
            CreatePrimitive("BoardFrame", PrimitiveType.Cube, parent, new Vector3(0f, 2.35f, -5.48f), new Vector3(3.25f, 1.75f, 0.12f), accentMaterial, Layer("Environment"));
            GameObject surface = CreatePrimitive("DrawingSurface", PrimitiveType.Cube, parent, new Vector3(0f, 2.35f, -5.405f), new Vector3(3f, 1.5f, 0.035f), boardMaterial, Layer("Whiteboard"));
            WhiteboardCanvas canvas = surface.AddComponent<WhiteboardCanvas>();

            GameObject cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursor.name = "DrawingCursor";
            cursor.transform.SetParent(surface.transform, false);
            // The old sphere cursor is replaced at runtime by the precise
            // crosshair. Keep the reference for backwards-compatible scene
            // data, but never show the large floating sphere.
            cursor.SetActive(false);
            cursor.transform.localScale = Vector3.one * 0.018f;
            cursor.layer = Layer("Whiteboard");
            Collider cursorCollider = cursor.GetComponent<Collider>();
            if (cursorCollider != null)
                UnityEngine.Object.DestroyImmediate(cursorCollider);
            Renderer cursorRenderer = cursor.GetComponent<Renderer>();
            if (cursorRenderer != null)
                cursorRenderer.sharedMaterial = markerMaterial;

            SerializedObject serializedCanvas = new SerializedObject(canvas);
            serializedCanvas.FindProperty("surfaceRenderer").objectReferenceValue = surface.GetComponent<Renderer>();
            serializedCanvas.FindProperty("surfaceCollider").objectReferenceValue = surface.GetComponent<Collider>();
            serializedCanvas.FindProperty("cursor").objectReferenceValue = cursor.transform;
            serializedCanvas.FindProperty("cursorRenderer").objectReferenceValue = cursorRenderer;
            serializedCanvas.FindProperty("boardWorldSize").vector2Value = new Vector2(3f, 1.5f);
            serializedCanvas.FindProperty("textureWidth").intValue = 1024;
            serializedCanvas.FindProperty("textureHeight").intValue = 512;
            serializedCanvas.FindProperty("markerSize").floatValue = 0.032f;
            serializedCanvas.FindProperty("eraserSize").floatValue = 0.06f;
            serializedCanvas.FindProperty("markerOpacity").floatValue = 0.95f;
            serializedCanvas.FindProperty("interpolationSpacing").floatValue = 0.0006f;
            serializedCanvas.FindProperty("maximumInterpolationSteps").intValue = 8192;
            serializedCanvas.ApplyModifiedPropertiesWithoutUndo();

            CreateMarkerTray(parent, markerMaterial, accentMaterial);
            return canvas;
        }

        private static void CreateMarkerTray(Transform parent, Material markerMaterial, Material accentMaterial)
        {
            CreatePrimitive("ToolTray", PrimitiveType.Cube, parent, new Vector3(-1.25f, 1.18f, -5.15f), new Vector3(0.75f, 0.08f, 0.38f), accentMaterial, Layer("Environment"));
            GameObject marker = CreatePrimitive("GrabMarker", PrimitiveType.Cylinder, parent, new Vector3(-1.25f, 1.35f, -5.14f), new Vector3(0.045f, 0.18f, 0.045f), markerMaterial, Layer("Grabbable"));
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Rigidbody rigidbody = marker.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            XRGrabInteractable grab = marker.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
        }

        private static void CreateStudentDesk(Transform parent, Material woodMaterial, Vector3 position, string name)
        {
            GameObject desk = NewObject(name, parent.transform);
            desk.transform.localPosition = position;
            CreatePrimitive("Top", PrimitiveType.Cube, desk.transform, new Vector3(0f, 1.05f, 0f), new Vector3(2.2f, 0.12f, 1.0f), woodMaterial, Layer("Environment"));
            CreateDeskLeg(desk.transform, woodMaterial, new Vector3(-0.85f, 0.5f, -0.35f));
            CreateDeskLeg(desk.transform, woodMaterial, new Vector3(0.85f, 0.5f, -0.35f));
            CreateDeskLeg(desk.transform, woodMaterial, new Vector3(-0.85f, 0.5f, 0.35f));
            CreateDeskLeg(desk.transform, woodMaterial, new Vector3(0.85f, 0.5f, 0.35f));
            CreateChair(parent, woodMaterial, position + new Vector3(0f, 0f, 0.85f), name.Replace("Desk", "Chair"));
        }

        private static void CreateDeskLeg(Transform parent, Material material, Vector3 position)
        {
            CreatePrimitive("Leg", PrimitiveType.Cube, parent, position, new Vector3(0.1f, 1f, 0.1f), material, Layer("Environment"));
        }

        private static void CreateChair(Transform parent, Material material, Vector3 position, string name)
        {
            GameObject chair = NewObject(name, parent);
            chair.transform.localPosition = position;
            CreatePrimitive("Seat", PrimitiveType.Cube, chair.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.12f, 0.8f), material, Layer("Environment"));
            CreatePrimitive("Back", PrimitiveType.Cube, chair.transform, new Vector3(0f, 0.95f, 0.35f), new Vector3(0.8f, 0.85f, 0.12f), material, Layer("Environment"));
        }

        private static void CreateTeacherDesk(Transform parent, Material woodMaterial, Material accentMaterial)
        {
            GameObject desk = NewObject("TeacherDesk", parent);
            desk.transform.localPosition = new Vector3(0f, 0f, -3.1f);
            CreatePrimitive("Top", PrimitiveType.Cube, desk.transform, new Vector3(0f, 1.1f, 0f), new Vector3(3.2f, 0.15f, 0.9f), woodMaterial, Layer("Environment"));
            CreatePrimitive("FrontPanel", PrimitiveType.Cube, desk.transform, new Vector3(0f, 0.55f, 0.35f), new Vector3(3.1f, 0.9f, 0.1f), accentMaterial, Layer("Environment"));
            CreateDeskLeg(desk.transform, woodMaterial, new Vector3(-1.35f, 0.5f, -0.3f));
            CreateDeskLeg(desk.transform, woodMaterial, new Vector3(1.35f, 0.5f, -0.3f));
        }

        private static void CreateWindow(Transform parent, Material wallMaterial, Material glassMaterial)
        {
            CreatePrimitive("WindowFrame", PrimitiveType.Cube, parent, new Vector3(-4.15f, 2.45f, 6.42f), new Vector3(2.4f, 1.65f, 0.08f), wallMaterial, Layer("Environment"));
            CreatePrimitive("WindowGlass", PrimitiveType.Cube, parent, new Vector3(-4.15f, 2.45f, 6.35f), new Vector3(2.0f, 1.25f, 0.04f), glassMaterial, Layer("Environment"));
            CreatePrimitive("WindowMullion", PrimitiveType.Cube, parent, new Vector3(-4.15f, 2.45f, 6.28f), new Vector3(0.08f, 1.25f, 0.04f), wallMaterial, Layer("Environment"));
        }

        private static void CreateDoor(Transform parent, Material woodMaterial, Material accentMaterial)
        {
            CreatePrimitive("Door", PrimitiveType.Cube, parent, new Vector3(4.45f, 1.5f, 6.42f), new Vector3(1.6f, 3f, 0.12f), woodMaterial, Layer("Environment"));
            CreatePrimitive("DoorSign", PrimitiveType.Cube, parent, new Vector3(4.45f, 3.0f, 6.28f), new Vector3(0.8f, 0.22f, 0.05f), accentMaterial, Layer("Environment"));
        }

        private static void CreateDecorations(Transform parent, Material accentMaterial, Material woodMaterial)
        {
            CreatePrimitive("ClassroomClock", PrimitiveType.Cylinder, parent, new Vector3(4.3f, 3.35f, -6.35f), new Vector3(0.42f, 0.05f, 0.42f), accentMaterial, Layer("Environment"));
        }

        private static void DisableImportedVisualEffects(GameObject importedInstance)
        {
            Transform[] children = importedInstance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string lower = children[i].name.ToLowerInvariant();
                if (!lower.Contains("dust")
                    && !lower.Contains("light_rays")
                    && !lower.Contains("light rays")
                    && !lower.Contains("glow")
                    && lower != "sune"
                    && !lower.Contains("sphere")
                    && !lower.Contains("ball")
                    && !lower.Contains("circle"))
                    continue;

                children[i].gameObject.SetActive(false);

                Renderer[] renderers = children[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    renderers[j].enabled = false;

                ParticleSystem[] particles = children[i].GetComponentsInChildren<ParticleSystem>(true);
                for (int j = 0; j < particles.Length; j++)
                    particles[j].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static void CreateLighting(Transform parent)
        {
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.58f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.7f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 35f;

            GameObject key = NewObject("Lighting", parent);
            Light directional = key.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.1f;
            directional.color = new Color(1f, 0.95f, 0.85f);
            directional.transform.localRotation = Quaternion.Euler(45f, -25f, 0f);

            CreatePointLight(key.transform, new Vector3(-3f, 3.7f, 0f), new Color(0.8f, 0.9f, 1f));
            CreatePointLight(key.transform, new Vector3(3f, 3.7f, 0f), new Color(0.8f, 0.9f, 1f));
        }

        private static void CreatePointLight(Transform parent, Vector3 position, Color color)
        {
            GameObject lightObject = NewObject("Classroom Light", parent);
            lightObject.transform.localPosition = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 2.2f;
            light.color = color;
            light.shadows = LightShadows.None;
        }

        private static XRInteractionManager CreateXROrigin(Transform parent, WhiteboardCanvas board, WhiteboardDrawer drawer, XRStudyWhiteboardManager manager)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPrefabPath);
            GameObject origin;
            if (prefab != null)
            {
                origin = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                origin.name = "XR Origin";
                origin.transform.SetParent(parent, false);
                origin.transform.localPosition = new Vector3(0f, 0f, 2.7f);
                origin.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                DisableTemplateCallouts(origin);
            }
            else
            {
                origin = NewObject("XR Origin", parent.transform);
                Debug.LogWarning("Complete controller XR Origin prefab was not found: " + XROriginPrefabPath);
            }

            XRInteractionManager interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                GameObject managerObject = NewObject("XR Interaction Manager", parent);
                interactionManager = managerObject.AddComponent<XRInteractionManager>();
            }

            Transform rightController = FindDeepChild(origin.transform, "Right Controller", "Right Controller Teleport Stabilized Origin", "Right Hand");
            if (rightController == null)
            {
                rightController = origin.transform;
                Debug.LogWarning("Right controller transform was not found in the reused XR Origin. Check the prefab hierarchy before Quest testing.");
            }

            // Keep the interaction component on the always-active XR root.
            // Controller prefabs can be disabled temporarily by XRI modality
            // while a device is connecting; putting the whiteboard interactor
            // on that object made the board ray disappear in the editor and
            // during headset startup. The ray still originates at the right
            // controller transform.
            XRWhiteboardInteractor interactor = parent.gameObject.GetComponent<XRWhiteboardInteractor>();
            if (interactor == null)
                interactor = parent.gameObject.AddComponent<XRWhiteboardInteractor>();
            interactor.SetReferences(board, drawer, rightController);
            interactor.ConfigureInputActions(AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));

            HandWhiteboardInteractor handInteractor = origin.AddComponent<HandWhiteboardInteractor>();
            handInteractor.SetReferences(board, drawer);

            return interactionManager;
        }

        private static void CreateEditorDeviceSimulator(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("XR Device Simulator prefab was not found: " + SimulatorPrefabPath);
                return;
            }

            GameObject simulator = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (simulator == null)
            {
                Debug.LogWarning("Unity could not instantiate the XR Device Simulator prefab.");
                return;
            }

            simulator.name = SimulatorObjectName;
            SceneManager.MoveGameObjectToScene(simulator, scene);
            simulator.SetActive(false);
            // XRStudyRoomLocomotion enables this only for editor desktop mode;
            // Android starts with the physical controller path and keeps this
            // editor-only helper inactive.
        }

        private static void DisableTemplateCallouts(GameObject origin)
        {
            // Keep Unity's official affordance callouts active. They are part
            // of the built-in XR controller presentation (UI Press, Grab,
            // Blink, etc.) and their scripts expect their GameObjects to stay
            // active while the Device Simulator is running.
        }

        private static void CreateHandsPermissions(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandsPermissionsPrefabPath);
            if (prefab == null)
                return;

            GameObject permissions = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            permissions.name = "Hands Permissions Manager";
            permissions.transform.SetParent(parent, false);
            permissions.SetActive(false);
        }

        private static void CreateEventSystem(Transform parent)
        {
            GameObject eventSystemObject = NewObject("EventSystem", parent);
            eventSystemObject.AddComponent<EventSystem>();
            XRUIInputModule inputModule = eventSystemObject.AddComponent<XRUIInputModule>();
            inputModule.enableXRInput = true;
            // The editor test must be able to operate the Device Simulator
            // panel itself (including its collapse button) with the desktop
            // mouse. XR controller and hand input remain enabled as well.
            inputModule.enableMouseInput = true;
            inputModule.enableBuiltinActionsAsFallback = true;

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            XRUIInputSetup inputSetup = eventSystemObject.AddComponent<XRUIInputSetup>();
            inputSetup.Initialize(actions, inputModule);
        }

        private static void CreateWhiteboardUI(Transform parent, XRStudyWhiteboardManager manager, out WhiteboardStatusDisplay statusDisplay, out ClearBoardConfirmation clearConfirmation)
        {
            GameObject canvasObject = NewObject("WhiteboardUI", parent);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            // TrackedDeviceGraphicRaycaster handles physical XR rays.  The
            // additional GraphicRaycaster keeps the same world-space UI
            // clickable with the Unity Game-view cursor during desktop and
            // Device Simulator testing.
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvas.sortingOrder = 10;
            canvasObject.transform.localPosition = new Vector3(2.08f, 1.95f, -5.30f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.00135f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(640f, 760f);

            GameObject panelObject = NewObject("ToolPanel", canvasObject.transform);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.sizeDelta = canvasRect.sizeDelta;
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.075f, 0.12f, 0.97f);
            panelImage.raycastTarget = false;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            CreateText(panelObject.transform, "Title", "XR STUDY\nWHITEBOARD", font, new Vector2(0f, 315f), new Vector2(560f, 105f), 34f, TextAlignmentOptions.Center, new Color(0.62f, 0.92f, 1f));
            CreateText(panelObject.transform, "Subtitle", "Today's Study Session", font, new Vector2(0f, 245f), new Vector2(560f, 45f), 21f, TextAlignmentOptions.Center, Color.white);
            // Keep the heading inside the panel's -320 px left edge. The
            // previous -240/220 combination started 30 px outside the
            // background, which made COLOUR and TOOL look detached.
            CreateText(panelObject.transform, "ColourHeading", "COLOUR", font, new Vector2(-180f, 180f), new Vector2(190f, 36f), 20f, TextAlignmentOptions.Left, new Color(0.55f, 0.72f, 0.8f));

            WhiteboardColourButton black = CreateColourButton(panelObject.transform, "Black", WhiteboardColour.Black, manager, font, new Vector2(-175f, 120f));
            WhiteboardColourButton red = CreateColourButton(panelObject.transform, "Red", WhiteboardColour.Red, manager, font, new Vector2(-55f, 120f));
            WhiteboardColourButton blue = CreateColourButton(panelObject.transform, "Blue", WhiteboardColour.Blue, manager, font, new Vector2(65f, 120f));
            WhiteboardColourButton green = CreateColourButton(panelObject.transform, "Green", WhiteboardColour.Green, manager, font, new Vector2(185f, 120f));

            CreateText(panelObject.transform, "ToolHeading", "TOOL", font, new Vector2(-180f, 45f), new Vector2(190f, 36f), 20f, TextAlignmentOptions.Left, new Color(0.55f, 0.72f, 0.8f));
            WhiteboardToolButton marker = CreateToolButton(panelObject.transform, "Marker", WhiteboardTool.Marker, manager, font, new Vector2(-125f, -15f));
            WhiteboardToolButton eraser = CreateToolButton(panelObject.transform, "Eraser", WhiteboardTool.Eraser, manager, font, new Vector2(125f, -15f));

            GameObject statusObject = NewObject("Status", panelObject.transform);
            statusObject.AddComponent<RectTransform>().sizeDelta = new Vector2(560f, 135f);
            statusObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -135f);
            TMP_Text toolText = CreateText(statusObject.transform, "ToolStatus", "CURRENT TOOL\nMARKER", font, new Vector2(-180f, 0f), new Vector2(170f, 100f), 18f, TextAlignmentOptions.Left, Color.white);
            TMP_Text colourText = CreateText(statusObject.transform, "ColourStatus", "COLOUR\nBLACK", font, new Vector2(-5f, 0f), new Vector2(145f, 100f), 18f, TextAlignmentOptions.Left, Color.white);
            TMP_Text inputText = CreateText(statusObject.transform, "InputStatus", "INPUT\nCONTROLLER / HANDS", font, new Vector2(185f, 0f), new Vector2(185f, 100f), 14f, TextAlignmentOptions.Left, new Color(0.7f, 0.82f, 0.87f));
            statusDisplay = statusObject.AddComponent<WhiteboardStatusDisplay>();
            statusDisplay.Initialize(toolText, colourText, inputText, null);

            WhiteboardClearButton clearButton;
            GameObject clearObject = CreateButton(panelObject.transform, "ClearBoard", "CLEAR BOARD", font, new Vector2(0f, -285f), new Vector2(350f, 72f), new Color(0.7f, 0.18f, 0.2f), out clearButton);
            clearButton.Initialize(manager);

            GameObject confirmation = NewObject("ClearConfirmation", panelObject.transform);
            RectTransform confirmationRect = confirmation.AddComponent<RectTransform>();
            confirmationRect.sizeDelta = new Vector2(640f, 760f);
            Image overlay = confirmation.AddComponent<Image>();
            overlay.color = new Color(0.01f, 0.02f, 0.04f, 0.96f);
            overlay.raycastTarget = false;
            CreateText(confirmation.transform, "ConfirmTitle", "CLEAR ALL NOTES?", font, new Vector2(0f, 100f), new Vector2(560f, 80f), 32f, TextAlignmentOptions.Center, Color.white);
            object unusedButton;
            Button cancelButton = CreateButton(confirmation.transform, "Cancel", "CANCEL", font, new Vector2(-130f, -55f), new Vector2(220f, 75f), new Color(0.18f, 0.25f, 0.31f), out unusedButton).GetComponent<Button>();
            Button confirmButton = CreateButton(confirmation.transform, "Confirm", "CLEAR", font, new Vector2(130f, -55f), new Vector2(220f, 75f), new Color(0.75f, 0.18f, 0.2f), out unusedButton).GetComponent<Button>();
            clearConfirmation = confirmation.AddComponent<ClearBoardConfirmation>();
            clearConfirmation.Initialize(manager, cancelButton, confirmButton);
            clearConfirmation.Hide();

            // Keep local variables alive for clarity when inspecting the scene in the editor.
            _ = black;
            _ = red;
            _ = blue;
            _ = green;
            _ = marker;
            _ = eraser;
        }

        private static WhiteboardColourButton CreateColourButton(Transform parent, string name, WhiteboardColour colour, XRStudyWhiteboardManager manager, TMP_FontAsset font, Vector2 position)
        {
            WhiteboardColourButton colourButton;
            GameObject buttonObject = CreateButton(parent, name, name.ToUpperInvariant(), font, position, new Vector2(108f, 70f), new Color(0.09f, 0.14f, 0.19f), out colourButton);
            colourButton = buttonObject.AddComponent<WhiteboardColourButton>();
            GameObject swatchObject = NewObject("Swatch", buttonObject.transform);
            RectTransform swatchRect = swatchObject.AddComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0f, 0.5f);
            swatchRect.anchorMax = new Vector2(0f, 0.5f);
            swatchRect.anchoredPosition = new Vector2(14f, 0f);
            swatchRect.sizeDelta = new Vector2(24f, 24f);
            Image swatch = swatchObject.AddComponent<Image>();
            swatch.color = XRStudyWhiteboardManager.GetColour(colour);
            TMP_Text label = buttonObject.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.rectTransform.sizeDelta = new Vector2(68f, 54f);
                label.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            }
            Outline outline = buttonObject.GetComponent<Outline>();
            colourButton.Initialize(manager, colour, swatch, outline);
            return colourButton;
        }

        private static WhiteboardToolButton CreateToolButton(Transform parent, string name, WhiteboardTool tool, XRStudyWhiteboardManager manager, TMP_FontAsset font, Vector2 position)
        {
            WhiteboardToolButton toolButton;
            GameObject buttonObject = CreateButton(parent, name, name.ToUpperInvariant(), font, position, new Vector2(250f, 70f), new Color(0.09f, 0.14f, 0.19f), out toolButton);
            toolButton = buttonObject.AddComponent<WhiteboardToolButton>();
            toolButton.Initialize(manager, tool, buttonObject.GetComponent<Outline>());
            return toolButton;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, Color colour, out WhiteboardColourButton unusedColourButton)
        {
            unusedColourButton = null;
            return CreateButtonInternal(parent, name, label, font, position, size, colour);
        }

        private static GameObject CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, Color colour, out WhiteboardToolButton unusedToolButton)
        {
            unusedToolButton = null;
            return CreateButtonInternal(parent, name, label, font, position, size, colour);
        }

        private static GameObject CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, Color colour, out WhiteboardClearButton clearButton)
        {
            GameObject buttonObject = CreateButtonInternal(parent, name, label, font, position, size, colour);
            clearButton = buttonObject.AddComponent<WhiteboardClearButton>();
            return buttonObject;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, Color colour, out object unused)
        {
            unused = null;
            return CreateButtonInternal(parent, name, label, font, position, size, colour);
        }

        private static GameObject CreateButtonInternal(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, Color colour)
        {
            GameObject buttonObject = NewObject(name, parent);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = buttonObject.AddComponent<Image>();
            image.color = colour;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = colour;
            colors.highlightedColor = Color.Lerp(colour, Color.white, 0.28f);
            colors.pressedColor = Color.Lerp(colour, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.9f, 1f, 1f);
            outline.effectDistance = new Vector2(3f, 3f);
            outline.enabled = false;
            TMP_Text text = CreateText(buttonObject.transform, "Label", label, font, Vector2.zero, size - new Vector2(18f, 12f), 19f, TextAlignmentOptions.Center, Color.white);
            text.raycastTarget = false;
            return buttonObject;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color colour)
        {
            GameObject textObject = NewObject(name, parent);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.font = font;
            textComponent.fontSize = fontSize;
            textComponent.color = colour;
            textComponent.alignment = alignment;
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = Mathf.Max(10f, fontSize * 0.55f);
            textComponent.fontSizeMax = fontSize;
            textComponent.overflowMode = TextOverflowModes.Ellipsis;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static void CreateTeleportArea(GameObject floor)
        {
            TeleportationArea teleport = floor.AddComponent<TeleportationArea>();
            SerializedObject serializedTeleport = new SerializedObject(teleport);
            SerializedProperty colliders = serializedTeleport.FindProperty("m_Colliders");
            if (colliders != null)
            {
                colliders.arraySize = 1;
                colliders.GetArrayElementAtIndex(0).objectReferenceValue = floor.GetComponent<Collider>();
            }
            SerializedProperty layers = serializedTeleport.FindProperty("m_InteractionLayers");
            if (layers != null)
                layers.FindPropertyRelative("m_Bits").intValue = -1;
            serializedTeleport.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeepChild(Transform root, params string[] names)
        {
            if (root == null)
                return null;
            for (int i = 0; i < names.Length; i++)
            {
                if (root.name == names[i])
                    return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), names);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            const string folder = "Assets/XRStudyWhiteboard/Materials";
            string path = folder + "/" + name.Replace(" ", "") + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static int Layer(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            return layer < 0 ? 0 : layer;
        }

        private static void EnsureBuildScene(Scene scene)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            SceneManager.SetActiveScene(scene);
        }
    }
}
