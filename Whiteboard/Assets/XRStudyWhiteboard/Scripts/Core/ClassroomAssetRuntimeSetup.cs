using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Adapts an imported classroom model to the existing XR study systems.
    /// The model is visual environment content; the interactive whiteboard
    /// and desk stationery stay project-owned and remain independent of the
    /// downloaded asset's original board and furniture components.
    /// </summary>
    public sealed class ClassroomAssetRuntimeSetup : MonoBehaviour
    {
        [SerializeField] private GameObject classroomInstance;
        [SerializeField] private float targetRoomWidth = 11f;
        [SerializeField] private float targetRoomDepth = 12.5f;
        [SerializeField] private float targetRoomHeight = 4.2f;
        [SerializeField] private Vector3 classroomEulerAngles;
        [SerializeField] private WhiteboardCanvas whiteboardCanvas;

        private Transform sceneRoot;
        private bool stationeryCreated;

        public void SetReferences(GameObject instance, WhiteboardCanvas canvas)
        {
            classroomInstance = instance;
            whiteboardCanvas = canvas;
        }

        private void Start()
        {
            sceneRoot = transform.root;
            if (whiteboardCanvas == null)
                whiteboardCanvas = sceneRoot.GetComponentInChildren<WhiteboardCanvas>(true);

            DisableGeneratedEnvironment();

            if (classroomInstance == null)
            {
                Transform existingInstance = FindChild(sceneRoot, "Classroom (Imported FBX)");
                if (existingInstance != null)
                    classroomInstance = existingInstance.gameObject;
            }

            if (classroomInstance == null)
            {
                Debug.LogError("[XR Study Whiteboard] The imported classroom prefab is not placed in the classroom scene.", this);
                return;
            }

            classroomInstance.SetActive(true);
            classroomInstance.name = "Classroom (Imported FBX)";
            DisableImportedLightingEffects(classroomInstance.transform);
            FitClassroomToRoom(classroomInstance.transform);
            PlaceInteractiveWhiteboard(classroomInstance.transform);
            CreateStudentStationery(classroomInstance.transform);
        }

        private static void DisableImportedLightingEffects(Transform model)
        {
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string lower = children[i].name.ToLowerInvariant();
                if (!IsImportedVisualEffectName(lower))
                    continue;

                // These are decorative export objects (sun/light-ray meshes
                // and the unexplained black sphere beside the door), not
                // classroom content. Disable the whole object so neither a
                // renderer nor a leftover collider can block the room.
                children[i].gameObject.SetActive(false);

                Renderer[] renderers = children[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    renderers[j].enabled = false;

                ParticleSystem[] particles = children[i].GetComponentsInChildren<ParticleSystem>(true);
                for (int j = 0; j < particles.Length; j++)
                    particles[j].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static bool IsImportedVisualEffectName(string lowerName)
        {
            return lowerName.Contains("dust")
                || lowerName.Contains("light_rays")
                || lowerName.Contains("light rays")
                || lowerName.Contains("glow")
                || lowerName == "sune"
                || lowerName.Contains("sphere")
                || lowerName.Contains("ball")
                || lowerName.Contains("circle");
        }

        private void DisableGeneratedEnvironment()
        {
            Transform generatedFurniture = FindChild(sceneRoot, "ClassroomFurniture");
            if (generatedFurniture != null)
                generatedFurniture.gameObject.SetActive(false);

            Transform generatedEnvironment = FindChild(sceneRoot, "Environment");
            if (generatedEnvironment == null)
                return;

            string[] replaceableObjects =
            {
                "Ceiling", "FrontWall", "BackWall", "LeftWall", "RightWall",
                "FrontWallAccent", "WindowFrame", "WindowGlass", "WindowMullion",
                "Door", "DoorSign"
            };

            for (int i = 0; i < replaceableObjects.Length; i++)
            {
                Transform child = FindChild(generatedEnvironment, replaceableObjects[i]);
                if (child != null)
                    child.gameObject.SetActive(false);
            }
        }

        private void FitClassroomToRoom(Transform model)
        {
            model.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(classroomEulerAngles));
            model.localScale = Vector3.one;

            if (!TryGetBounds(model, out Bounds bounds))
                return;

            model.localScale = new Vector3(
                targetRoomWidth / Mathf.Max(0.01f, bounds.size.x),
                targetRoomHeight / Mathf.Max(0.01f, bounds.size.y),
                targetRoomDepth / Mathf.Max(0.01f, bounds.size.z));

            if (!TryGetBounds(model, out bounds))
                return;

            model.position = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        private void PlaceInteractiveWhiteboard(Transform model)
        {
            Renderer originalBoard = FindOriginalBoard(model);
            if (originalBoard == null)
                return;

            Bounds boardBounds = originalBoard.bounds;
            // Hide the exact renderer used to locate the imported board as a
            // final guard.  Some FBX exports name the mesh child "Plane" so
            // a name-only hierarchy pass can miss the dark board surface.
            originalBoard.enabled = false;
            Collider[] originalBoardColliders = originalBoard.GetComponents<Collider>();
            for (int i = 0; i < originalBoardColliders.Length; i++)
                originalBoardColliders[i].enabled = false;
            DisableOriginalBoard(model);

            Transform surface = FindChild(sceneRoot, "DrawingSurface");
            Transform frame = FindChild(sceneRoot, "BoardFrame");
            Transform toolTray = FindChild(sceneRoot, "ToolTray");
            Transform marker = FindChild(sceneRoot, "GrabMarker");
            Transform whiteboardUi = FindChild(sceneRoot, "WhiteboardUI");
            if (surface == null || whiteboardCanvas == null)
                return;

            Vector3 up = Vector3.up;
            Vector3 normal;
            Vector3 right;
            Vector3 size = boardBounds.size;
            if (size.x <= size.y && size.x <= size.z)
            {
                normal = originalBoard.transform.right.normalized;
                right = originalBoard.transform.forward.normalized;
            }
            else
            {
                normal = originalBoard.transform.forward.normalized;
                right = originalBoard.transform.right.normalized;
            }

            normal.y = 0f;
            if (normal.sqrMagnitude < 0.1f)
                normal = Vector3.forward;
            normal.Normalize();
            Vector3 boardCenter = boardBounds.center; // Use the imported board's fitted world center.
            // The imported FBX can have its board normal pointing toward the
            // wall.  Always face the interactive surface into the classroom
            // so the original dark board cannot occlude it.
            Vector3 towardRoom = -boardCenter;
            towardRoom.y = 0f;
            if (towardRoom.sqrMagnitude > 0.1f && Vector3.Dot(normal, towardRoom.normalized) < 0f)
                normal = -normal;
            right.y = 0f;
            if (right.sqrMagnitude < 0.1f)
                right = Vector3.right;
            right.Normalize();

            float width = Mathf.Max(1.2f, 2f * (Mathf.Abs(right.x) * boardBounds.extents.x + Mathf.Abs(right.z) * boardBounds.extents.z) * 0.9f);
            float height = Mathf.Max(0.8f, boardBounds.size.y * 0.86f);
            Quaternion boardRotation = Quaternion.LookRotation(normal, up);
            Vector3 boardFrontOffset = towardRoom.sqrMagnitude > 0.1f
                ? towardRoom.normalized * 0.22f
                : normal * 0.22f;
            Vector3 boardFrontCenter = boardCenter + boardFrontOffset;
            Vector3 boardFrameCenter = boardFrontCenter - boardFrontOffset.normalized * 0.09f;

            surface.SetPositionAndRotation(boardFrontCenter, boardRotation);
            surface.localScale = new Vector3(width, height, 0.035f);
            whiteboardCanvas.ConfigureBoardWorldSize(new Vector2(width, height));

            if (frame != null)
            {
                // BoardFrame is a solid backing cube, so keep it behind the
                // thin writing surface instead of letting it occlude it.
                frame.SetPositionAndRotation(boardFrameCenter, boardRotation);
                frame.localScale = new Vector3(width + 0.2f, height + 0.2f, 0.12f);
            }

            if (toolTray != null)
            {
                toolTray.SetPositionAndRotation(
                    boardFrontCenter - up * (height * 0.5f + 0.14f) - right * (width * 0.28f),
                    boardRotation);
            }

            if (marker != null && toolTray != null)
            {
                marker.SetPositionAndRotation(
                    toolTray.position + up * 0.18f,
                    boardRotation * Quaternion.Euler(90f, 0f, 0f));
            }

            if (whiteboardUi != null)
            {
                // World-space Unity canvases present their graphic front on
                // the opposite side from this imported board normal. The
                // old rotation therefore displayed every label backwards.
                Quaternion uiRotation = boardRotation * Quaternion.Euler(0f, 180f, 0f);
                // Keep the control panel completely outside the board
                // collider and slightly toward the room.  The previous
                // 0.35 m side gap left the panel's left edge overlapping the
                // board, so controller rays hit the whiteboard first and the
                // colour/tool buttons became impossible to select.
                Vector3 frontDirection = boardFrontOffset.sqrMagnitude > 0.01f
                    ? boardFrontOffset.normalized
                    : normal;
                whiteboardUi.SetPositionAndRotation(
                    boardFrontCenter
                    + right * (width * 0.5f + 0.72f)
                    + frontDirection * 0.14f
                    + up * 0.02f,
                    uiRotation);
            }
        }

        private void DisableOriginalBoard(Transform model)
        {
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (!IsImportedBoardTransform(children[i]))
                    continue;

                Renderer[] renderers = children[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    renderers[j].enabled = false;

                Collider[] colliders = children[i].GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < colliders.Length; j++)
                    colliders[j].enabled = false;
            }
        }

        private void CreateStudentStationery(Transform model)
        {
            if (stationeryCreated)
                return;
            stationeryCreated = true;

            Transform stationeryParent = new GameObject("Student Desk Stationery").transform;
            stationeryParent.SetParent(sceneRoot, false);

            List<Transform> desks = FindDeskObjects(model);
            if (!TryGetBounds(model, out Bounds roomBounds))
                return;

            List<Bounds> tabletopBounds = FindTabletopBounds(model, roomBounds);
            if (tabletopBounds.Count > 0)
            {
                for (int i = 0; i < tabletopBounds.Count; i++)
                    CreateStationerySet(stationeryParent, tabletopBounds[i], i + 1);
                return;
            }

            float deskTopHeight = TryGetDeskTopHeight(model, roomBounds, out float detectedDeskTop)
                ? detectedDeskTop
                : roomBounds.min.y + roomBounds.size.y * 0.58f;
            if (desks.Count < 4)
            {
                Bounds deskLayoutBounds = roomBounds;
                if (TryGetCombinedBounds(desks, out Bounds detectedDeskLayout))
                    deskLayoutBounds = detectedDeskLayout;

                int fallbackIndex = 0;
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        // The FBX contains the student desks as three large
                        // combined meshes. Use their footprint instead of
                        // the room edges so each set lands on a real desk.
                        float xMargin = Mathf.Clamp(deskLayoutBounds.size.x * 0.16f, 0.8f, 1.3f);
                        float zMargin = Mathf.Clamp(deskLayoutBounds.size.z * 0.17f, 0.75f, 1.25f);
                        float x = Mathf.Lerp(deskLayoutBounds.min.x + xMargin, deskLayoutBounds.max.x - xMargin, column / 2f);
                        float z = Mathf.Lerp(deskLayoutBounds.max.z - zMargin, deskLayoutBounds.min.z + zMargin, row / 2f);
                        // The downloaded FBX combines most desks into a
                        // small number of meshes, so use the detected desk
                        // height while retaining nine usable student spots.
                        float deskHeight = Mathf.Max(0.65f, deskTopHeight - roomBounds.min.y);
                        CreateStationerySet(
                            stationeryParent,
                            new Bounds(
                                new Vector3(x, roomBounds.min.y + deskHeight * 0.5f, z),
                                new Vector3(1.2f, deskHeight, 0.8f)),
                            ++fallbackIndex);
                    }
                }
                return;
            }

            for (int i = 0; i < desks.Count; i++)
            {
                if (TryGetBounds(desks[i], out Bounds deskBounds))
                    CreateStationerySet(stationeryParent, deskBounds, i + 1);
            }
        }

        private static List<Bounds> FindTabletopBounds(Transform model, Bounds roomBounds)
        {
            List<Bounds> tabletops = new List<Bounds>();
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.ToLowerInvariant() != "table")
                    continue;

                MeshFilter meshFilter = children[i].GetComponent<MeshFilter>();
                Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (mesh == null)
                    continue;

                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                int[] component = new int[vertices.Length];
                for (int vertex = 0; vertex < component.Length; vertex++)
                    component[vertex] = -1;

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
                    Bounds triangleBounds = TransformTriangleBounds(meshFilter.transform, vertices[a], vertices[b], vertices[c]);
                    if (boundsByComponent.TryGetValue(id, out Bounds existing))
                        existing.Encapsulate(triangleBounds);
                    else
                        existing = triangleBounds;
                    boundsByComponent[id] = existing;
                }

                foreach (KeyValuePair<int, Bounds> pair in boundsByComponent)
                {
                    Bounds bounds = pair.Value;
                    // The exported "table" mesh contains one disconnected
                    // tabletop per student desk. Ignore trim/bolt-sized
                    // components and keep only the shallow desk-top pieces.
                    if (bounds.size.x < 0.35f || bounds.size.x > 1.8f
                        || bounds.size.z < 0.35f || bounds.size.z > 1.8f
                        || bounds.size.y > 0.3f
                        || bounds.max.y <= roomBounds.min.y + 0.4f
                        || bounds.max.y >= roomBounds.max.y - 0.5f)
                        continue;

                    tabletops.Add(bounds);
                }

                if (tabletops.Count > 0)
                    break;
            }

            tabletops.Sort((left, right) =>
            {
                int zCompare = left.center.z.CompareTo(right.center.z);
                return zCompare != 0 ? zCompare : left.center.x.CompareTo(right.center.x);
            });
            return tabletops;
        }

        private static Bounds TransformTriangleBounds(Transform transform, Vector3 a, Vector3 b, Vector3 c)
        {
            Bounds bounds = new Bounds(transform.TransformPoint(a), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(b));
            bounds.Encapsulate(transform.TransformPoint(c));
            return bounds;
        }

        private List<Transform> FindDeskObjects(Transform model)
        {
            List<Transform> desks = new List<Transform>();
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string lower = children[i].name.ToLowerInvariant();
                bool tableName = lower == "table" || lower.StartsWith("table.") || lower.StartsWith("table2") || lower.Contains("desk");
                if (!tableName || lower == "tables" || lower.Contains("fronk") || lower.Contains("teacher") || !TryGetBounds(children[i], out Bounds bounds))
                    continue;
                if (!lower.Contains("table") && (bounds.size.x > 4f || bounds.size.z > 4f))
                    continue;
                desks.Add(children[i]);
            }
            return desks;
        }

        private static bool TryGetCombinedBounds(List<Transform> objects, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            for (int i = 0; i < objects.Count; i++)
            {
                if (!TryGetBounds(objects[i], out Bounds candidate))
                    continue;

                if (!found)
                {
                    bounds = candidate;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(candidate);
                }
            }

            return found;
        }

        private void CreateStationerySet(Transform parent, Bounds deskBounds, int index)
        {
            GameObject setObject = new GameObject("Student Desk Stationery " + index.ToString("00"));
            setObject.transform.SetParent(parent, false);
            // The downloaded classroom combines several table tops and legs
            // into a small number of meshes.  The old placement used a point
            // well below max.y, which put the stationery inside/under the
            // desks.  Keep every set visible and reachable on the tabletop.
            float tabletopHeight = deskBounds.max.y + 0.025f;
            setObject.transform.position = new Vector3(deskBounds.center.x, tabletopHeight, deskBounds.center.z);

            Material paperMaterial = CreateRuntimeMaterial("Student Paper", Color.white);

            // Keep the tabletop clear: the paper is the only physical study
            // item. Pencil, eraser, and clear-paper actions are provided by
            // the small floating table menu beside it.
            GameObject paper = CreatePrimitive("Paper Note", PrimitiveType.Cube, setObject.transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.55f, 0.018f, 0.38f), paperMaterial);
            PaperNoteCanvas paperCanvas = paper.AddComponent<PaperNoteCanvas>();
            paperCanvas.Configure(paper.GetComponent<Renderer>(), paper.GetComponent<Collider>(), new Vector2(0.55f, 0.38f));
            paperCanvas.ConfigureWritingSizes(0.009f, 0.032f);
            AddGrabbable(paper);

            StudyTableToolMenu toolMenu = setObject.AddComponent<StudyTableToolMenu>();
            toolMenu.Initialize(paperCanvas);

            StudyTableTeleportPoint teleportPoint = setObject.AddComponent<StudyTableTeleportPoint>();
            teleportPoint.Initialize(paperCanvas);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            return item;
        }

        private static XRGrabInteractable AddGrabbable(GameObject item)
        {
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            XRGrabInteractable grab = item.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            return grab;
        }

        private static Material CreateRuntimeMaterial(string name, Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = name,
                color = colour
            };
            return material;
        }

        private static Renderer FindOriginalBoard(Transform model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (IsImportedBoardTransform(renderers[i].transform))
                    return renderers[i];
            }
            return null;
        }

        private static bool IsImportedBoardTransform(Transform candidate)
        {
            for (Transform current = candidate; current != null; current = current.parent)
            {
                string lower = current.name.ToLowerInvariant();
                if (lower.Contains("blackboard")
                    || lower == "board"
                    || lower.StartsWith("board1")
                    || lower.StartsWith("board2")
                    || lower.StartsWith("boards3"))
                    return true;
            }

            return false;
        }

        private static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                // Disabled imported dust/light-ray/glow renderers must not
                // enlarge the classroom bounds used for runtime fitting.
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;

                if (!found)
                {
                    bounds = renderers[i].bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            return found;
        }

        private static bool TryGetDeskTopHeight(Transform model, Bounds roomBounds, out float deskTopHeight)
        {
            deskTopHeight = 0f;
            int samples = 0;
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string lower = children[i].name.ToLowerInvariant();
                bool tableName = lower == "table" || lower.StartsWith("table.") || lower.StartsWith("table2");
                if (!tableName || !TryGetBounds(children[i], out Bounds bounds))
                    continue;

                // Ignore a mesh that is effectively the whole room rather
                // than a desk/table grouping.
                if (bounds.size.x > roomBounds.size.x * 0.85f || bounds.size.z > roomBounds.size.z * 0.85f)
                    continue;
                if (bounds.max.y <= roomBounds.min.y + 0.5f || bounds.max.y >= roomBounds.max.y - 0.15f)
                    continue;

                deskTopHeight = Mathf.Max(deskTopHeight, bounds.max.y);
                samples++;
            }

            return samples > 0;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
