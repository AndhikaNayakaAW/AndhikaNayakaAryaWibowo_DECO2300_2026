// script overview: this script connects the imported classroom model to the interactive study features. it
// fits the room, positions the project whiteboard, detects desk surfaces, and creates each desk paper, tool
// menu, and seated teleport destination. the classroom model supplies the artwork. the project scripts supply
// drawing and interaction. paper is kept fixed so a writing press does not grab it.

// search: classroom setup | whiteboard placement | table setup | fixed paper.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make the types in unity engine.xr.interaction.toolkit.interactables available to this script.
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // adapts an imported classroom model to the existing xr study systems.
    // the model is visual environment content; the interactive whiteboard
    // and desk stationery stay project-owned and remain independent of the
    // downloaded asset's original board and furniture components.
    // declare the classroom asset runtime setup class based on unity behaviour.
    public sealed class ClassroomAssetRuntimeSetup : MonoBehaviour
    // begin the class classroom asset runtime setup block.
    {
        // keep classroom instance as game object for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private GameObject classroomInstance;
        // keep target room width as float, initially 11. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private float targetRoomWidth = 11f;
        // keep target room depth as float, initially 12.5. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private float targetRoomDepth = 12.5f;
        // keep target room height as float, initially 4.2. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private float targetRoomHeight = 4.2f;
        // keep classroom euler angles as three-dimensional vector for later use. allow unity to save and
        // configure this field in the inspector.
        [SerializeField] private Vector3 classroomEulerAngles;
        // keep whiteboard canvas as whiteboard canvas for later use. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private WhiteboardCanvas whiteboardCanvas;

        // keep scene root as transform for later use.
        private Transform sceneRoot;
        // keep stationery created as bool for later use.
        private bool stationeryCreated;

        // method: set references.
        // connect the existing scene objects used by this component rather than creating another copy of those
        // systems.
        public void SetReferences(GameObject instance, WhiteboardCanvas canvas)
        // begin the method set references block.
        {
            // set classroom instance to instance.
            classroomInstance = instance;
            // set whiteboard canvas to canvas.
            whiteboardCanvas = canvas;
        // finish the method set references block.
        }

        // method: start.
        // find the imported classroom, replace the fallback environment, fit the artwork, position the
        // interactive whiteboard, and create desk study surfaces.
        private void Start()
        // begin the method start block.
        {
            // set scene root to transform root.
            sceneRoot = transform.root;
            // run the following statement only when whiteboard canvas is missing.
            if (whiteboardCanvas == null)
                // set whiteboard canvas to the requested component in the children of scene root.
                whiteboardCanvas = sceneRoot.GetComponentInChildren<WhiteboardCanvas>(true);

            // call disable generated environment: hide the old generated furniture and walls that are replaced
            // by the imported classroom artwork.
            DisableGeneratedEnvironment();

            // run the following statement only when classroom instance is missing.
            if (classroomInstance == null)
            // begin the if statement block.
            {
                // store existing instance as find child using scene root, the text "classroom (imported fbx)".
                Transform existingInstance = FindChild(sceneRoot, "Classroom (Imported FBX)");
                // run the following statement only when existing instance exists.
                if (existingInstance != null)
                    // set classroom instance to existing instance game object.
                    classroomInstance = existingInstance.gameObject;
            // finish the if statement block.
            }

            // run the following statement only when classroom instance is missing.
            if (classroomInstance == null)
            // begin the if statement block.
            {
                // debug: log error using the text "[xr study whiteboard] the imported classroom prefab is not
                // placed in the classroom scene.", this.
                Debug.LogError("[XR Study Whiteboard] The imported classroom prefab is not placed in the classroom scene.", this);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // show and enable classroom instance.
            classroomInstance.SetActive(true);
            // set classroom instance name to the text "classroom (imported fbx)".
            classroomInstance.name = "Classroom (Imported FBX)";
            // disable imported lighting effects using classroom instance transform.
            DisableImportedLightingEffects(classroomInstance.transform);
            // fit classroom to room using classroom instance transform.
            FitClassroomToRoom(classroomInstance.transform);
            // place interactive whiteboard using classroom instance transform.
            PlaceInteractiveWhiteboard(classroomInstance.transform);
            // create student stationery using classroom instance transform.
            CreateStudentStationery(classroomInstance.transform);
        // finish the method start block.
        }

        // method: disable imported lighting effects.
        // disable decorative exported light effects and matching leftover colliders or particles that can
        // obstruct the classroom.
        private static void DisableImportedLightingEffects(Transform model)
        // begin the method disable imported lighting effects block.
        {
            // store children as the matching components below model.
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            // repeat this loop while i is less than the number of items in children. increase i by one after
            // each pass.
            for (int i = 0; i < children.Length; i++)
            // begin the for statement block.
            {
                // store lower as the entry at i in children name: to lower invariant.
                string lower = children[i].name.ToLowerInvariant();
                // run the following statement only when not (is imported visual effect name using lower).
                if (!IsImportedVisualEffectName(lower))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // these are decorative export objects (sun/light-ray meshes
                // and the unexplained black sphere beside the door), not
                // classroom content. disable the whole object so neither a
                // renderer nor a leftover collider can block the room.
                // hide and disable the entry at i in children game object.
                children[i].gameObject.SetActive(false);

                // store renderers as the matching components below the entry at i in children.
                Renderer[] renderers = children[i].GetComponentsInChildren<Renderer>(true);
                // repeat this loop while j is less than the number of items in renderers. increase j by one
                // after each pass.
                for (int j = 0; j < renderers.Length; j++)
                    // set whether the entry at j in renderers is enabled to false.
                    renderers[j].enabled = false;

                // store particles as the matching components below the entry at i in children.
                ParticleSystem[] particles = children[i].GetComponentsInChildren<ParticleSystem>(true);
                // repeat this loop while j is less than the number of items in particles. increase j by one
                // after each pass.
                for (int j = 0; j < particles.Length; j++)
                    // the entry at j in particles: stop using true, particle system stop behavior stop
                    // emitting and clear.
                    particles[j].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // finish the for statement block.
            }
        // finish the method disable imported lighting effects block.
        }

        // method: is imported visual effect name.
        // recognise the imported decorative object names filtered by the cleanup pass.
        private static bool IsImportedVisualEffectName(string lowerName)
        // begin the method is imported visual effect name block.
        {
            // return whether lower name contains the text "dust" or whether lower name contains the text
            // "light_rays" or whether lower name contains the text "light rays" or whether lower name contains
            // the text "glow" or lower name equals the text "sune" or whether lower name contains the text
            // "sphere" or whether lower name contains the text "ball" or whether lower name contains the text
            // "circle" to the caller.
            return lowerName.Contains("dust")
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays".
                || lowerName.Contains("light_rays")
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays" or whether lower name contains the text "light rays".
                || lowerName.Contains("light rays")
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays" or whether lower name contains the text "light rays" or whether lower name
                // contains the text "glow".
                || lowerName.Contains("glow")
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays" or whether lower name contains the text "light rays" or whether lower name
                // contains the text "glow" or lower name equals the text "sune".
                || lowerName == "sune"
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays" or whether lower name contains the text "light rays" or whether lower name
                // contains the text "glow" or lower name equals the text "sune" or whether lower name contains
                // the text "sphere".
                || lowerName.Contains("sphere")
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays" or whether lower name contains the text "light rays" or whether lower name
                // contains the text "glow" or lower name equals the text "sune" or whether lower name contains
                // the text "sphere" or whether lower name contains the text "ball".
                || lowerName.Contains("ball")
                // whether lower name contains the text "dust" or whether lower name contains the text
                // "light_rays" or whether lower name contains the text "light rays" or whether lower name
                // contains the text "glow" or lower name equals the text "sune" or whether lower name contains
                // the text "sphere" or whether lower name contains the text "ball" or whether lower name
                // contains the text "circle".
                || lowerName.Contains("circle");
        // finish the method is imported visual effect name block.
        }

        // method: disable generated environment.
        // hide the old generated furniture and walls that are replaced by the imported classroom artwork.
        private void DisableGeneratedEnvironment()
        // begin the method disable generated environment block.
        {
            // store generated furniture as find child using scene root, the text "classroomfurniture".
            Transform generatedFurniture = FindChild(sceneRoot, "ClassroomFurniture");
            // run the following statement only when generated furniture exists.
            if (generatedFurniture != null)
                // hide and disable generated furniture game object.
                generatedFurniture.gameObject.SetActive(false);

            // store generated environment as find child using scene root, the text "environment".
            Transform generatedEnvironment = FindChild(sceneRoot, "Environment");
            // run the following statement only when generated environment is missing.
            if (generatedEnvironment == null)
                // leave this method now without running the remaining statements.
                return;

            // store replaceable objects as the listed initial values.
            string[] replaceableObjects =
            // begin the array initializer expression values.
            {
                // the text "ceiling".
                "Ceiling", "FrontWall", "BackWall", "LeftWall", "RightWall",
                // the text "frontwallaccent".
                "FrontWallAccent", "WindowFrame", "WindowGlass", "WindowMullion",
                // the text "door".
                "Door", "DoorSign"
            // finish the array initializer expression values.
            };

            // repeat this loop while i is less than the number of items in replaceable objects. increase i by
            // one after each pass.
            for (int i = 0; i < replaceableObjects.Length; i++)
            // begin the for statement block.
            {
                // store child as find child using generated environment, the entry at i in replaceable
                // objects.
                Transform child = FindChild(generatedEnvironment, replaceableObjects[i]);
                // run the following statement only when child exists.
                if (child != null)
                    // hide and disable child game object.
                    child.gameObject.SetActive(false);
            // finish the for statement block.
            }
        // finish the method disable generated environment block.
        }

        // method: fit classroom to room.
        // scale the imported model to the target room dimensions, then centre it and align its bottom with the
        // floor.
        private void FitClassroomToRoom(Transform model)
        // begin the method fit classroom to room block.
        {
            // place model at the zero vector facing quaternion: euler using classroom euler angles.
            model.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(classroomEulerAngles));
            // set model local scale to unit scale on all three axes.
            model.localScale = Vector3.one;

            // run the following statement only when not (try get bounds using model, the output value bounds).
            if (!TryGetBounds(model, out Bounds bounds))
                // leave this method now without running the remaining statements.
                return;

            // set model local scale to a new three-dimensional vector using target room width divided by the
            // larger of 0.01 and bounds size x, target room height divided by the larger of 0.01 and bounds
            // size y, target room depth divided by the larger of 0.01 and bounds size z.
            model.localScale = new Vector3(
                // supply target room width divided by the larger of 0.01 and bounds size x to the enclosing
                // call.
                targetRoomWidth / Mathf.Max(0.01f, bounds.size.x),
                // supply target room height divided by the larger of 0.01 and bounds size y to the enclosing
                // call.
                targetRoomHeight / Mathf.Max(0.01f, bounds.size.y),
                // supply target room depth divided by the larger of 0.01 and bounds size z to the enclosing
                // call.
                targetRoomDepth / Mathf.Max(0.01f, bounds.size.z));

            // run the following statement only when not (try get bounds using model, bounds).
            if (!TryGetBounds(model, out bounds))
                // leave this method now without running the remaining statements.
                return;

            // set model position to a new three-dimensional vector using the negative of bounds center x, the
            // negative of bounds min y, the negative of bounds center z.
            model.position = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        // finish the method fit classroom to room block.
        }

        // method: place interactive whiteboard.
        // find the imported board location and put the project writing surface, backing frame, and controls
        // there facing into the classroom.
        private void PlaceInteractiveWhiteboard(Transform model)
        // begin the method place interactive whiteboard block.
        {
            // store original board as find original board using model.
            Renderer originalBoard = FindOriginalBoard(model);
            // run the following statement only when original board is missing.
            if (originalBoard == null)
                // leave this method now without running the remaining statements.
                return;

            // store board bounds as original board bounds.
            Bounds boardBounds = originalBoard.bounds;
            // hide the exact renderer used to locate the imported board as a
            // final guard.  some fbx exports name the mesh child "plane" so
            // a name-only hierarchy pass can miss the dark board surface.
            // set whether original board is enabled to false.
            originalBoard.enabled = false;
            // store original board colliders as original board: get components.
            Collider[] originalBoardColliders = originalBoard.GetComponents<Collider>();
            // repeat this loop while i is less than the number of items in original board colliders. increase
            // i by one after each pass.
            for (int i = 0; i < originalBoardColliders.Length; i++)
                // set whether the entry at i in original board colliders is enabled to false.
                originalBoardColliders[i].enabled = false;
            // disable original board using model.
            DisableOriginalBoard(model);

            // store surface as find child using scene root, the text "drawingsurface".
            Transform surface = FindChild(sceneRoot, "DrawingSurface");
            // store frame as find child using scene root, the text "boardframe".
            Transform frame = FindChild(sceneRoot, "BoardFrame");
            // store whiteboard ui as find child using scene root, the text "whiteboardui".
            Transform whiteboardUi = FindChild(sceneRoot, "WhiteboardUI");
            // run the following statement only when surface is missing or whiteboard canvas is missing.
            if (surface == null || whiteboardCanvas == null)
                // leave this method now without running the remaining statements.
                return;

            // store up as the upward unit vector.
            Vector3 up = Vector3.up;
            // store normal as a local three-dimensional vector value.
            Vector3 normal;
            // store right as a local three-dimensional vector value.
            Vector3 right;
            // store size as board bounds size.
            Vector3 size = boardBounds.size;
            // run the following statement only when size x is at most size y and size x is at most size z.
            if (size.x <= size.y && size.x <= size.z)
            // begin the if statement block.
            {
                // set normal to the unit direction of original board transform right.
                normal = originalBoard.transform.right.normalized;
                // set right to the unit direction of original board transform forward.
                right = originalBoard.transform.forward.normalized;
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // set normal to the unit direction of original board transform forward.
                normal = originalBoard.transform.forward.normalized;
                // set right to the unit direction of original board transform right.
                right = originalBoard.transform.right.normalized;
            // finish the else clause block.
            }

            // set normal y to 0.
            normal.y = 0f;
            // run the following statement only when the squared length of normal is less than 0.1.
            if (normal.sqrMagnitude < 0.1f)
                // set normal to the forward unit vector.
                normal = Vector3.forward;
            // make normal a unit-length direction.
            normal.Normalize();
            // store board center as board bounds center.
            Vector3 boardCenter = boardBounds.center; // Use the imported board's fitted world center.
            // the imported fbx can have its board normal pointing toward the
            // wall.  always face the interactive surface into the classroom
            // so the original dark board cannot occlude it.
            // store toward room as the negative of board center.
            Vector3 towardRoom = -boardCenter;
            // set toward room y to 0.
            towardRoom.y = 0f;
            // run the following statement only when the squared length of toward room is greater than 0.1 and
            // the dot product of normal and the unit direction of toward room is less than 0.
            if (towardRoom.sqrMagnitude > 0.1f && Vector3.Dot(normal, towardRoom.normalized) < 0f)
                // set normal to the negative of normal.
                normal = -normal;
            // set right y to 0.
            right.y = 0f;
            // run the following statement only when the squared length of right is less than 0.1.
            if (right.sqrMagnitude < 0.1f)
                // set right to the rightward unit vector.
                right = Vector3.right;
            // make right a unit-length direction.
            right.Normalize();

            // store width as the larger of 1.2 and 2 multiplied by the absolute value of right x multiplied by
            // board bounds extents x plus the absolute value of right z multiplied by board bounds extents z
            // multiplied by 0.9.
            float width = Mathf.Max(1.2f, 2f * (Mathf.Abs(right.x) * boardBounds.extents.x + Mathf.Abs(right.z) * boardBounds.extents.z) * 0.9f);
            // store height as the larger of 0.8 and board bounds size y multiplied by 0.86.
            float height = Mathf.Max(0.8f, boardBounds.size.y * 0.86f);
            // store board rotation as quaternion: look rotation using normal, up.
            Quaternion boardRotation = Quaternion.LookRotation(normal, up);
            // store board front offset as if the squared length of toward room is greater than 0.1, use the
            // unit direction of toward room multiplied by 0.22, otherwise use normal multiplied by 0.22.
            Vector3 boardFrontOffset = towardRoom.sqrMagnitude > 0.1f
                // if the squared length of toward room is greater than 0.1, use the unit direction of toward
                // room multiplied by 0.22, otherwise use normal multiplied by 0.22.
                ? towardRoom.normalized * 0.22f
                // if the squared length of toward room is greater than 0.1, use the unit direction of toward
                // room multiplied by 0.22, otherwise use normal multiplied by 0.22.
                : normal * 0.22f;
            // store board front center as board center plus board front offset.
            Vector3 boardFrontCenter = boardCenter + boardFrontOffset;
            // store board frame center as board front center minus the unit direction of board front offset
            // multiplied by 0.09.
            Vector3 boardFrameCenter = boardFrontCenter - boardFrontOffset.normalized * 0.09f;

            // place surface at board front center facing board rotation.
            surface.SetPositionAndRotation(boardFrontCenter, boardRotation);
            // set surface local scale to a new three-dimensional vector using width, height, 0.035.
            surface.localScale = new Vector3(width, height, 0.035f);
            // whiteboard canvas: configure board world size using a new two-dimensional vector using width,
            // height.
            whiteboardCanvas.ConfigureBoardWorldSize(new Vector2(width, height));

            // run the following statement only when frame exists.
            if (frame != null)
            // begin the if statement block.
            {
                // boardframe is a solid backing cube, so keep it behind the
                // thin writing surface instead of letting it occlude it.
                // place frame at board frame center facing board rotation.
                frame.SetPositionAndRotation(boardFrameCenter, boardRotation);
                // set frame local scale to a new three-dimensional vector using width plus 0.2, height plus
                // 0.2, 0.12.
                frame.localScale = new Vector3(width + 0.2f, height + 0.2f, 0.12f);
            // finish the if statement block.
            }

            // run the following statement only when whiteboard ui exists.
            if (whiteboardUi != null)
            // begin the if statement block.
            {
                // world-space unity canvases present their graphic front on
                // the opposite side from this imported board normal. the
                // old rotation therefore displayed every label backwards.
                // store ui rotation as board rotation multiplied by quaternion: euler using 0, 180, 0.
                Quaternion uiRotation = boardRotation * Quaternion.Euler(0f, 180f, 0f);
                // keep the control panel just outside the board collider but
                // close enough to remain in the seated whiteboard view. a
                // large side gap pushed the status/colour labels beyond the
                // game view and made the panel look clipped.
                // store front direction as if the squared length of board front offset is greater than 0.01,
                // use the unit direction of board front offset, otherwise use normal.
                Vector3 frontDirection = boardFrontOffset.sqrMagnitude > 0.01f
                    // if the squared length of board front offset is greater than 0.01, use the unit direction
                    // of board front offset, otherwise use normal.
                    ? boardFrontOffset.normalized
                    // if the squared length of board front offset is greater than 0.01, use the unit direction
                    // of board front offset, otherwise use normal.
                    : normal;
                // place whiteboard ui at board front center plus right multiplied by width multiplied by 0.5
                // plus 0.7 plus front direction multiplied by 0.14 plus up multiplied by 0.02 facing ui
                // rotation.
                whiteboardUi.SetPositionAndRotation(
                    // supply board front center plus right multiplied by width multiplied by 0.5 plus 0.7 plus
                    // front direction multiplied by 0.14 plus up multiplied by 0.02 to the enclosing call.
                    boardFrontCenter
                    // the panel is roughly 1.04m wide at the runtime scale.
                    // leave enough clearance for its left edge to stay off
                    // the board instead of covering the writing surface.
                    // board front center plus right multiplied by width multiplied by 0.5 plus 0.7.
                    + right * (width * 0.5f + 0.70f)
                    // board front center plus right multiplied by width multiplied by 0.5 plus 0.7 plus front
                    // direction multiplied by 0.14.
                    + frontDirection * 0.14f
                    // board front center plus right multiplied by width multiplied by 0.5 plus 0.7 plus front
                    // direction multiplied by 0.14 plus up multiplied by 0.02.
                    + up * 0.02f,
                    // supply ui rotation to the enclosing call.
                    uiRotation);
                // keep the complete colour/tool/status panel inside the
                // whiteboard view while leaving the writing surface unobstructed.
                // set whiteboard ui local scale to unit scale on all three axes multiplied by 0.0018.
                whiteboardUi.localScale = Vector3.one * 0.0018f;
                // apply whiteboard ui layout using whiteboard ui.
                ApplyWhiteboardUiLayout(whiteboardUi);
            // finish the if statement block.
            }
        // finish the method place interactive whiteboard block.
        }

        // method: apply whiteboard ui layout.
        // apply the shared control-panel layout to the relocated board interface.
        private static void ApplyWhiteboardUiLayout(Transform whiteboardUi)
        // begin the method apply whiteboard ui layout block.
        {
            // store panel as the the whiteboard control panel component attached to this object component on
            // whiteboard ui.
            WhiteboardControlPanel panel = whiteboardUi.GetComponent<WhiteboardControlPanel>();
            // run the following statement only when panel is missing.
            if (panel == null)
                // set panel to a newly attached add component<whiteboard control panel> component on
                // whiteboard ui game object.
                panel = whiteboardUi.gameObject.AddComponent<WhiteboardControlPanel>();
            // panel: initialize using the requested component in the children of whiteboard ui root.
            panel.Initialize(whiteboardUi.root.GetComponentInChildren<XRStudyWhiteboardManager>(true));
        // finish the method apply whiteboard ui layout block.
        }

        // method: disable original board.
        // hide the imported board and related pieces so they cannot cover or obstruct the interactive board.
        private void DisableOriginalBoard(Transform model)
        // begin the method disable original board block.
        {
            // store children as the matching components below model.
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            // repeat this loop while i is less than the number of items in children. increase i by one after
            // each pass.
            for (int i = 0; i < children.Length; i++)
            // begin the for statement block.
            {
                // run the following statement only when not (is imported board transform using the entry at i
                // in children).
                if (!IsImportedBoardTransform(children[i]))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store renderers as the matching components below the entry at i in children.
                Renderer[] renderers = children[i].GetComponentsInChildren<Renderer>(true);
                // repeat this loop while j is less than the number of items in renderers. increase j by one
                // after each pass.
                for (int j = 0; j < renderers.Length; j++)
                    // set whether the entry at j in renderers is enabled to false.
                    renderers[j].enabled = false;

                // store colliders as the matching components below the entry at i in children.
                Collider[] colliders = children[i].GetComponentsInChildren<Collider>(true);
                // repeat this loop while j is less than the number of items in colliders. increase j by one
                // after each pass.
                for (int j = 0; j < colliders.Length; j++)
                    // set whether the entry at j in colliders is enabled to false.
                    colliders[j].enabled = false;
            // finish the for statement block.
            }
        // finish the method disable original board block.
        }

        // method: create student stationery.
        // detect the real tabletops and create one paper, menu, and teleport destination per desk. use a
        // fallback layout only when the imported mesh cannot be separated.
        private void CreateStudentStationery(Transform model)
        // begin the method create student stationery block.
        {
            // run the following statement only when stationery created.
            if (stationeryCreated)
                // leave this method now without running the remaining statements.
                return;
            // set stationery created to true.
            stationeryCreated = true;

            // store stationery parent as a new game object using the text "student desk stationery" transform.
            Transform stationeryParent = new GameObject("Student Desk Stationery").transform;
            // parent stationery parent under scene root using local coordinates.
            stationeryParent.SetParent(sceneRoot, false);

            // store desks as find desk objects using model.
            List<Transform> desks = FindDeskObjects(model);
            // run the following statement only when not (try get bounds using model, the output value room
            // bounds).
            if (!TryGetBounds(model, out Bounds roomBounds))
                // leave this method now without running the remaining statements.
                return;

            // store tabletop bounds as find tabletop bounds using model, room bounds.
            List<Bounds> tabletopBounds = FindTabletopBounds(model, roomBounds);
            // run the following statement only when the number of items in tabletop bounds is greater than 0.
            if (tabletopBounds.Count > 0)
            // begin the if statement block.
            {
                // repeat this loop while i is less than the number of items in tabletop bounds. increase i by
                // one after each pass.
                for (int i = 0; i < tabletopBounds.Count; i++)
                    // create stationery set using stationery parent, the entry at i in tabletop bounds, i plus
                    // 1.
                    CreateStationerySet(stationeryParent, tabletopBounds[i], i + 1);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store desk top height as if try get desk top height using model, room bounds, the output value
            // detected desk top, use detected desk top, otherwise use room bounds min y plus room bounds size
            // y multiplied by 0.58.
            float deskTopHeight = TryGetDeskTopHeight(model, roomBounds, out float detectedDeskTop)
                // if try get desk top height using model, room bounds, the output value detected desk top, use
                // detected desk top, otherwise use room bounds min y plus room bounds size y multiplied by
                // 0.58.
                ? detectedDeskTop
                // if try get desk top height using model, room bounds, the output value detected desk top, use
                // detected desk top, otherwise use room bounds min y plus room bounds size y multiplied by
                // 0.58.
                : roomBounds.min.y + roomBounds.size.y * 0.58f;
            // run the following statement only when the number of items in desks is less than 4.
            if (desks.Count < 4)
            // begin the if statement block.
            {
                // store desk layout bounds as room bounds.
                Bounds deskLayoutBounds = roomBounds;
                // run the following statement only when try get combined bounds using desks, the output value
                // detected desk layout.
                if (TryGetCombinedBounds(desks, out Bounds detectedDeskLayout))
                    // set desk layout bounds to detected desk layout.
                    deskLayoutBounds = detectedDeskLayout;

                // store fallback index as 0.
                int fallbackIndex = 0;
                // repeat this loop while row is less than 3. increase row by one after each pass.
                for (int row = 0; row < 3; row++)
                // begin the for statement block.
                {
                    // repeat this loop while column is less than 3. increase column by one after each pass.
                    for (int column = 0; column < 3; column++)
                    // begin the for statement block.
                    {
                        // the fbx contains the student desks as three large
                        // combined meshes. use their footprint instead of
                        // the room edges so each set lands on a real desk.
                        // store x margin as desk layout bounds size x multiplied by 0.16 clamped between 0.8
                        // and 1.3.
                        float xMargin = Mathf.Clamp(deskLayoutBounds.size.x * 0.16f, 0.8f, 1.3f);
                        // store z margin as desk layout bounds size z multiplied by 0.17 clamped between 0.75
                        // and 1.25.
                        float zMargin = Mathf.Clamp(deskLayoutBounds.size.z * 0.17f, 0.75f, 1.25f);
                        // store x as an interpolated value from desk layout bounds min x plus x margin toward
                        // desk layout bounds max x minus x margin using column divided by 2.
                        float x = Mathf.Lerp(deskLayoutBounds.min.x + xMargin, deskLayoutBounds.max.x - xMargin, column / 2f);
                        // store z as an interpolated value from desk layout bounds max z minus z margin toward
                        // desk layout bounds min z plus z margin using row divided by 2.
                        float z = Mathf.Lerp(deskLayoutBounds.max.z - zMargin, deskLayoutBounds.min.z + zMargin, row / 2f);
                        // the downloaded fbx combines most desks into a
                        // small number of meshes, so use the detected desk
                        // height while retaining nine usable student spots.
                        // store desk height as the larger of 0.65 and desk top height minus room bounds min y.
                        float deskHeight = Mathf.Max(0.65f, deskTopHeight - roomBounds.min.y);
                        // create stationery set using stationery parent, a new bounds using a new
                        // three-dimensional vector using x, room bounds min y plus desk height multiplied by
                        // 0.5, z, a new three-dimensional vector using 1.2, desk height, 0.8, increase
                        // fallback index by one.
                        CreateStationerySet(
                            // supply stationery parent to the enclosing call.
                            stationeryParent,
                            // supply a new bounds using a new three-dimensional vector using x, room bounds
                            // min y plus desk height multiplied by 0.5, z, a new three-dimensional vector
                            // using 1.2, desk height, 0.8 to the enclosing call.
                            new Bounds(
                                // supply a new three-dimensional vector using x, room bounds min y plus desk
                                // height multiplied by 0.5, z to the enclosing call.
                                new Vector3(x, roomBounds.min.y + deskHeight * 0.5f, z),
                                // supply a new three-dimensional vector using 1.2, desk height, 0.8 to the
                                // enclosing call.
                                new Vector3(1.2f, deskHeight, 0.8f)),
                            // supply increase fallback index by one to the enclosing call.
                            ++fallbackIndex);
                    // finish the for statement block.
                    }
                // finish the for statement block.
                }
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // repeat this loop while i is less than the number of items in desks. increase i by one after each
            // pass.
            for (int i = 0; i < desks.Count; i++)
            // begin the for statement block.
            {
                // run the following statement only when try get bounds using the entry at i in desks, the
                // output value desk bounds.
                if (TryGetBounds(desks[i], out Bounds deskBounds))
                    // create stationery set using stationery parent, desk bounds, i plus 1.
                    CreateStationerySet(stationeryParent, deskBounds, i + 1);
            // finish the for statement block.
            }
        // finish the method create student stationery block.
        }

        // method: find tabletop bounds.
        // inspect disconnected mesh pieces and keep desk-sized shallow bounds, then sort them for consistent
        // table placement.
        private static List<Bounds> FindTabletopBounds(Transform model, Bounds roomBounds)
        // begin the method find tabletop bounds block.
        {
            // store tabletops as a new list<bounds>.
            List<Bounds> tabletops = new List<Bounds>();
            // store children as the matching components below model.
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            // repeat this loop while i is less than the number of items in children. increase i by one after
            // each pass.
            for (int i = 0; i < children.Length; i++)
            // begin the for statement block.
            {
                // run the following statement only when the entry at i in children name: to lower invariant
                // differs from the text "table".
                if (children[i].name.ToLowerInvariant() != "table")
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store mesh filter as the the mesh filter component attached to this object component on the
                // entry at i in children.
                MeshFilter meshFilter = children[i].GetComponent<MeshFilter>();
                // store mesh as if mesh filter exists, use mesh filter shared mesh, otherwise use no object.
                Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                // run the following statement only when mesh is missing.
                if (mesh == null)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store vertices as mesh vertices.
                Vector3[] vertices = mesh.vertices;
                // store triangles as mesh triangles.
                int[] triangles = mesh.triangles;
                // store component as a new int[vertices.length] array.
                int[] component = new int[vertices.Length];
                // repeat this loop while vertex is less than the number of items in component. increase vertex
                // by one after each pass.
                for (int vertex = 0; vertex < component.Length; vertex++)
                    // set the entry at vertex in component to the negative of 1.
                    component[vertex] = -1;

                // store component count as 0.
                int componentCount = 0;
                // store bounds by component as a new dictionary<int, bounds>.
                Dictionary<int, Bounds> boundsByComponent = new Dictionary<int, Bounds>();
                // repeat this loop while triangle is less than the number of items in triangles. add 3 to
                // triangle after each pass.
                for (int triangle = 0; triangle < triangles.Length; triangle += 3)
                // begin the for statement block.
                {
                    // store a as the entry at triangle in triangles.
                    int a = triangles[triangle];
                    // store b as the entry at triangle plus 1 in triangles.
                    int b = triangles[triangle + 1];
                    // store c as the entry at triangle plus 2 in triangles.
                    int c = triangles[triangle + 2];
                    // store id as the entry at a in component.
                    int id = component[a];
                    // run the following statement only when id is less than 0.
                    if (id < 0)
                        // set id to the entry at b in component.
                        id = component[b];
                    // run the following statement only when id is less than 0.
                    if (id < 0)
                        // set id to the entry at c in component.
                        id = component[c];
                    // run the following statement only when id is less than 0.
                    if (id < 0)
                        // set id to increase component count by one.
                        id = componentCount++;

                    // set the entry at a in component to id.
                    component[a] = id;
                    // set the entry at b in component to id.
                    component[b] = id;
                    // set the entry at c in component to id.
                    component[c] = id;
                    // store triangle bounds as transform triangle bounds using mesh filter transform, the
                    // entry at a in vertices, the entry at b in vertices, the entry at c in vertices.
                    Bounds triangleBounds = TransformTriangleBounds(meshFilter.transform, vertices[a], vertices[b], vertices[c]);
                    // run the following statement only when bounds by component: try get value using id, the
                    // output value existing.
                    if (boundsByComponent.TryGetValue(id, out Bounds existing))
                        // existing: encapsulate using triangle bounds.
                        existing.Encapsulate(triangleBounds);
                    // handle the alternative when the preceding condition is false.
                    else
                        // set existing to triangle bounds.
                        existing = triangleBounds;
                    // set the entry at id in bounds by component to existing.
                    boundsByComponent[id] = existing;
                // finish the for statement block.
                }

                // visit each pair in bounds by component.
                foreach (KeyValuePair<int, Bounds> pair in boundsByComponent)
                // begin the for each statement block.
                {
                    // store bounds as pair value.
                    Bounds bounds = pair.Value;
                    // the exported "table" mesh contains one disconnected
                    // tabletop per student desk. ignore trim/bolt-sized
                    // components and keep only the shallow desk-top pieces.
                    // run the following statement only when bounds size x is less than 0.35 or bounds size x
                    // is greater than 1.8 or bounds size z is less than 0.35 or bounds size z is greater than
                    // 1.8 or bounds size y is greater than 0.3 or bounds max y is at most room bounds min y
                    // plus 0.4 or bounds max y is at least room bounds max y minus 0.5.
                    if (bounds.size.x < 0.35f || bounds.size.x > 1.8f
                        // bounds size x is less than 0.35 or bounds size x is greater than 1.8 or bounds size
                        // z is less than 0.35.
                        || bounds.size.z < 0.35f || bounds.size.z > 1.8f
                        // bounds size x is less than 0.35 or bounds size x is greater than 1.8 or bounds size
                        // z is less than 0.35 or bounds size z is greater than 1.8 or bounds size y is greater
                        // than 0.3.
                        || bounds.size.y > 0.3f
                        // bounds size x is less than 0.35 or bounds size x is greater than 1.8 or bounds size
                        // z is less than 0.35 or bounds size z is greater than 1.8 or bounds size y is greater
                        // than 0.3 or bounds max y is at most room bounds min y plus 0.4.
                        || bounds.max.y <= roomBounds.min.y + 0.4f
                        // bounds size x is less than 0.35 or bounds size x is greater than 1.8 or bounds size
                        // z is less than 0.35 or bounds size z is greater than 1.8 or bounds size y is greater
                        // than 0.3 or bounds max y is at most room bounds min y plus 0.4 or bounds max y is at
                        // least room bounds max y minus 0.5.
                        || bounds.max.y >= roomBounds.max.y - 0.5f)
                        // skip the rest of this loop pass and move to the next entry.
                        continue;

                    // append bounds to tabletops.
                    tabletops.Add(bounds);
                // finish the for each statement block.
                }

                // run the following statement only when the number of items in tabletops is greater than 0.
                if (tabletops.Count > 0)
                    // leave the nearest loop or switch after finding the required result.
                    break;
            // finish the for statement block.
            }

            // tabletops: sort using a callback that runs the following block.
            tabletops.Sort((left, right) =>
            // begin the parenthesized lambda expression block.
            {
                // store z compare as left center z: compare to using right center z.
                int zCompare = left.center.z.CompareTo(right.center.z);
                // return if z compare differs from 0, use z compare, otherwise use left center x: compare to
                // using right center x to the caller.
                return zCompare != 0 ? zCompare : left.center.x.CompareTo(right.center.x);
            // finish the parenthesized lambda expression block.
            });
            // return tabletops to the caller.
            return tabletops;
        // finish the method find tabletop bounds block.
        }

        // method: transform triangle bounds.
        // convert three mesh vertices to world coordinates and calculate the bounds of that triangle.
        private static Bounds TransformTriangleBounds(Transform transform, Vector3 a, Vector3 b, Vector3 c)
        // begin the method transform triangle bounds block.
        {
            // store bounds as a new bounds using a converted from transform local coordinates into world
            // coordinates, the zero vector.
            Bounds bounds = new Bounds(transform.TransformPoint(a), Vector3.zero);
            // bounds: encapsulate using b converted from transform local coordinates into world coordinates.
            bounds.Encapsulate(transform.TransformPoint(b));
            // bounds: encapsulate using c converted from transform local coordinates into world coordinates.
            bounds.Encapsulate(transform.TransformPoint(c));
            // return bounds to the caller.
            return bounds;
        // finish the method transform triangle bounds block.
        }

        // method: find desk objects.
        // find likely desk transforms in the imported hierarchy for the fallback desk layout.
        private List<Transform> FindDeskObjects(Transform model)
        // begin the method find desk objects block.
        {
            // store desks as a new list<transform>.
            List<Transform> desks = new List<Transform>();
            // store children as the matching components below model.
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            // repeat this loop while i is less than the number of items in children. increase i by one after
            // each pass.
            for (int i = 0; i < children.Length; i++)
            // begin the for statement block.
            {
                // store lower as the entry at i in children name: to lower invariant.
                string lower = children[i].name.ToLowerInvariant();
                // store table name as lower equals the text "table" or lower: starts with using the text
                // "table." or lower: starts with using the text "table2" or whether lower contains the text
                // "desk".
                bool tableName = lower == "table" || lower.StartsWith("table.") || lower.StartsWith("table2") || lower.Contains("desk");
                // run the following statement only when not (table name) or lower equals the text "tables" or
                // whether lower contains the text "fronk" or whether lower contains the text "teacher" or not
                // (try get bounds using the entry at i in children, the output value bounds).
                if (!tableName || lower == "tables" || lower.Contains("fronk") || lower.Contains("teacher") || !TryGetBounds(children[i], out Bounds bounds))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // run the following statement only when not (whether lower contains the text "table") and
                // bounds size x is greater than 4 or bounds size z is greater than 4.
                if (!lower.Contains("table") && (bounds.size.x > 4f || bounds.size.z > 4f))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // append the entry at i in children to desks.
                desks.Add(children[i]);
            // finish the for statement block.
            }
            // return desks to the caller.
            return desks;
        // finish the method find desk objects block.
        }

        // method: try get combined bounds.
        // combine the bounds of the supplied desk objects when usable renderers exist.
        private static bool TryGetCombinedBounds(List<Transform> objects, out Bounds bounds)
        // begin the method try get combined bounds block.
        {
            // set bounds to default.
            bounds = default;
            // store found as false.
            bool found = false;
            // repeat this loop while i is less than the number of items in objects. increase i by one after
            // each pass.
            for (int i = 0; i < objects.Count; i++)
            // begin the for statement block.
            {
                // run the following statement only when not (try get bounds using the entry at i in objects,
                // the output value candidate).
                if (!TryGetBounds(objects[i], out Bounds candidate))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // run the following statement only when not (found).
                if (!found)
                // begin the if statement block.
                {
                    // set bounds to candidate.
                    bounds = candidate;
                    // set found to true.
                    found = true;
                // finish the if statement block.
                }
                // handle the alternative when the preceding condition is false.
                else
                // begin the else clause block.
                {
                    // bounds: encapsulate using candidate.
                    bounds.Encapsulate(candidate);
                // finish the else clause block.
                }
            // finish the for statement block.
            }

            // return found to the caller.
            return found;
        // finish the method try get combined bounds block.
        }

        // method: create stationery set.
        // place a fixed paper above one tabletop, configure its writing sizes, and attach the tool menu and
        // seated destination.
        private void CreateStationerySet(Transform parent, Bounds deskBounds, int index)
        // begin the method create stationery set block.
        {
            // store set object as a new game object using the text "student desk stationery " plus index: to
            // string using the text "00".
            GameObject setObject = new GameObject("Student Desk Stationery " + index.ToString("00"));
            // parent set object transform under parent using local coordinates.
            setObject.transform.SetParent(parent, false);
            // the downloaded classroom combines several table tops and legs
            // into a small number of meshes.  the old placement used a point
            // well below max.y, which put the stationery inside/under the
            // desks.  keep every set visible and reachable on the tabletop.
            // store tabletop height as desk bounds max y plus 0.025.
            float tabletopHeight = deskBounds.max.y + 0.025f;
            // set set object transform position to a new three-dimensional vector using desk bounds center x,
            // tabletop height, desk bounds center z.
            setObject.transform.position = new Vector3(deskBounds.center.x, tabletopHeight, deskBounds.center.z);

            // store paper material as create runtime material using the text "student paper", colour white.
            Material paperMaterial = CreateRuntimeMaterial("Student Paper", Color.white);

            // keep the tabletop clear: the paper is the only physical study
            // item. pencil, eraser, and clear-paper actions are provided by
            // the small floating table menu beside it.
            // store paper as create primitive using the text "paper note", primitive type cube, set object
            // transform, a new three-dimensional vector using 0, 0.015, 0, a new three-dimensional vector
            // using 0.55, 0.018, 0.38, paper material.
            GameObject paper = CreatePrimitive("Paper Note", PrimitiveType.Cube, setObject.transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.55f, 0.018f, 0.38f), paperMaterial);
            // store paper canvas as a newly attached add component<paper note canvas> component on paper.
            PaperNoteCanvas paperCanvas = paper.AddComponent<PaperNoteCanvas>();
            // paper canvas: configure using the the renderer component attached to this object component on
            // paper, the the collider component attached to this object component on paper, a new
            // two-dimensional vector using 0.55, 0.38.
            paperCanvas.Configure(paper.GetComponent<Renderer>(), paper.GetComponent<Collider>(), new Vector2(0.55f, 0.38f));
            // paper canvas: configure writing sizes using 0.014, 0.032.
            paperCanvas.ConfigureWritingSizes(0.014f, 0.032f);
            // configure paper for writing using paper.
            ConfigurePaperForWriting(paper);

            // store tool menu as a newly attached add component<study table tool menu> component on set
            // object.
            StudyTableToolMenu toolMenu = setObject.AddComponent<StudyTableToolMenu>();
            // tool menu: initialize using paper canvas, desk bounds.
            toolMenu.Initialize(paperCanvas, deskBounds);

            // store teleport point as a newly attached add component<study table teleport point> component on
            // set object.
            StudyTableTeleportPoint teleportPoint = setObject.AddComponent<StudyTableTeleportPoint>();
            // teleport point: initialize using paper canvas, desk bounds, index.
            teleportPoint.Initialize(paperCanvas, deskBounds, index);
        // finish the method create stationery set block.
        }

        // method: configure paper for writing.
        // remove grab interaction and its rigidbody so the paper stays on the desk and can receive writing
        // instead of being picked up.
        public static void ConfigurePaperForWriting(GameObject paper)
        // begin the method configure paper for writing block.
        {
            // run the following statement only when paper is missing.
            if (paper == null)
                // leave this method now without running the remaining statements.
                return;

            // a note is a fixed writing surface. xrgrabinteractable consumes
            // the same right trigger used by papernotecanvas, so remove grab
            // behaviour and its rigidbody from generated and upgraded desks.
            // store grab as the the xr grab interactable component attached to this object component on paper.
            XRGrabInteractable grab = paper.GetComponent<XRGrabInteractable>();
            // store body as the the rigidbody component attached to this object component on paper.
            Rigidbody body = paper.GetComponent<Rigidbody>();
            // run the following statement only when whether the application is running in play mode.
            if (Application.isPlaying)
            // begin the if statement block.
            {
                // run the following statement only when grab exists.
                if (grab != null) Destroy(grab);
                // run the following statement only when body exists.
                if (body != null) Destroy(body);
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // run the following statement only when grab exists.
                if (grab != null) DestroyImmediate(grab);
                // run the following statement only when body exists.
                if (body != null) DestroyImmediate(body);
            // finish the else clause block.
            }
        // finish the method configure paper for writing block.
        }

        // method: create primitive.
        // create a primitive shape with the requested parent, local position, scale, and material.
        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        // begin the method create primitive block.
        {
            // store item as game object: create primitive using type.
            GameObject item = GameObject.CreatePrimitive(type);
            // set item name to name.
            item.name = name;
            // parent item transform under parent using local coordinates.
            item.transform.SetParent(parent, false);
            // set item transform local position to position.
            item.transform.localPosition = position;
            // set item transform local scale to scale.
            item.transform.localScale = scale;
            // store renderer as the the renderer component attached to this object component on item.
            Renderer renderer = item.GetComponent<Renderer>();
            // run the following statement only when renderer exists.
            if (renderer != null)
                // set renderer shared material to material.
                renderer.sharedMaterial = material;
            // return item to the caller.
            return item;
        // finish the method create primitive block.
        }

        // method: add grabbable.
        // provide a compatibility helper for physical objects by adding a kinematic body and grab component.
        // the current paper setup removes grabbing instead.
        private static XRGrabInteractable AddGrabbable(GameObject item)
        // begin the method add grabbable block.
        {
            // store body as a newly attached add component<rigidbody> component on item.
            Rigidbody body = item.AddComponent<Rigidbody>();
            // set body is kinematic to true.
            body.isKinematic = true;
            // set body use gravity to false.
            body.useGravity = false;
            // store grab as a newly attached add component<xr grab interactable> component on item.
            XRGrabInteractable grab = item.AddComponent<XRGrabInteractable>();
            // set grab throw on detach to false.
            grab.throwOnDetach = false;
            // return grab to the caller.
            return grab;
        // finish the method add grabbable block.
        }

        // method: create runtime material.
        // create an unlit material with the supplied name and colour, using a fallback shader if required.
        private static Material CreateRuntimeMaterial(string name, Color colour)
        // begin the method create runtime material block.
        {
            // store shader as the child or resource the text "universal render pipeline/unlit" found in
            // shader.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            // run the following statement only when shader is missing.
            if (shader == null)
                // set shader to the child or resource the text "unlit/texture" found in shader.
                shader = Shader.Find("Unlit/Texture");
            // store material as a new material using shader.
            Material material = new Material(shader)
            // begin the object initializer expression values.
            {
                // set name to name.
                name = name,
                // set colour to colour.
                color = colour
            // finish the object initializer expression values.
            };
            // return material to the caller.
            return material;
        // finish the method create runtime material block.
        }

        // method: find original board.
        // locate the imported board renderer used to position the interactive replacement.
        private static Renderer FindOriginalBoard(Transform model)
        // begin the method find original board block.
        {
            // store renderers as the matching components below model.
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            // repeat this loop while i is less than the number of items in renderers. increase i by one after
            // each pass.
            for (int i = 0; i < renderers.Length; i++)
            // begin the for statement block.
            {
                // run the following statement only when is imported board transform using the entry at i in
                // renderers transform.
                if (IsImportedBoardTransform(renderers[i].transform))
                    // return the entry at i in renderers to the caller.
                    return renderers[i];
            // finish the for statement block.
            }
            // return no object to the caller.
            return null;
        // finish the method find original board block.
        }

        // method: is imported board transform.
        // recognise imported board-related transforms by their names.
        private static bool IsImportedBoardTransform(Transform candidate)
        // begin the method is imported board transform block.
        {
            // repeat this loop while current exists. set current to current parent after each pass.
            for (Transform current = candidate; current != null; current = current.parent)
            // begin the for statement block.
            {
                // store lower as current name: to lower invariant.
                string lower = current.name.ToLowerInvariant();
                // run the following statement only when whether lower contains the text "blackboard" or lower
                // equals the text "board" or lower: starts with using the text "board1" or lower: starts with
                // using the text "board2" or lower: starts with using the text "boards3".
                if (lower.Contains("blackboard")
                    // whether lower contains the text "blackboard" or lower equals the text "board".
                    || lower == "board"
                    // whether lower contains the text "blackboard" or lower equals the text "board" or lower:
                    // starts with using the text "board1".
                    || lower.StartsWith("board1")
                    // whether lower contains the text "blackboard" or lower equals the text "board" or lower:
                    // starts with using the text "board1" or lower: starts with using the text "board2".
                    || lower.StartsWith("board2")
                    // whether lower contains the text "blackboard" or lower equals the text "board" or lower:
                    // starts with using the text "board1" or lower: starts with using the text "board2" or
                    // lower: starts with using the text "boards3".
                    || lower.StartsWith("boards3"))
                    // return true to the caller.
                    return true;
            // finish the for statement block.
            }

            // return false to the caller.
            return false;
        // finish the method is imported board transform block.
        }

        // method: try get bounds.
        // combine usable renderer bounds below the requested transform.
        private static bool TryGetBounds(Transform root, out Bounds bounds)
        // begin the method try get bounds block.
        {
            // store renderers as the matching components below root.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            // set bounds to default.
            bounds = default;
            // store found as false.
            bool found = false;
            // repeat this loop while i is less than the number of items in renderers. increase i by one after
            // each pass.
            for (int i = 0; i < renderers.Length; i++)
            // begin the for statement block.
            {
                // disabled imported dust/light-ray/glow renderers must not
                // enlarge the classroom bounds used for runtime fitting.
                // run the following statement only when the entry at i in renderers is missing or not (whether
                // the entry at i in renderers is enabled).
                if (renderers[i] == null || !renderers[i].enabled)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // run the following statement only when not (found).
                if (!found)
                // begin the if statement block.
                {
                    // set bounds to the entry at i in renderers bounds.
                    bounds = renderers[i].bounds;
                    // set found to true.
                    found = true;
                // finish the if statement block.
                }
                // handle the alternative when the preceding condition is false.
                else
                // begin the else clause block.
                {
                    // bounds: encapsulate using the entry at i in renderers bounds.
                    bounds.Encapsulate(renderers[i].bounds);
                // finish the else clause block.
                }
            // finish the for statement block.
            }
            // return found to the caller.
            return found;
        // finish the method try get bounds block.
        }

        // method: try get desk top height.
        // estimate the tabletop height from imported desk geometry for fallback stationery placement.
        private static bool TryGetDeskTopHeight(Transform model, Bounds roomBounds, out float deskTopHeight)
        // begin the method try get desk top height block.
        {
            // set desk top height to 0.
            deskTopHeight = 0f;
            // store samples as 0.
            int samples = 0;
            // store children as the matching components below model.
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            // repeat this loop while i is less than the number of items in children. increase i by one after
            // each pass.
            for (int i = 0; i < children.Length; i++)
            // begin the for statement block.
            {
                // store lower as the entry at i in children name: to lower invariant.
                string lower = children[i].name.ToLowerInvariant();
                // store table name as lower equals the text "table" or lower: starts with using the text
                // "table." or lower: starts with using the text "table2".
                bool tableName = lower == "table" || lower.StartsWith("table.") || lower.StartsWith("table2");
                // run the following statement only when not (table name) or not (try get bounds using the
                // entry at i in children, the output value bounds).
                if (!tableName || !TryGetBounds(children[i], out Bounds bounds))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // ignore a mesh that is effectively the whole room rather
                // than a desk/table grouping.
                // run the following statement only when bounds size x is greater than room bounds size x
                // multiplied by 0.85 or bounds size z is greater than room bounds size z multiplied by 0.85.
                if (bounds.size.x > roomBounds.size.x * 0.85f || bounds.size.z > roomBounds.size.z * 0.85f)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // run the following statement only when bounds max y is at most room bounds min y plus 0.5 or
                // bounds max y is at least room bounds max y minus 0.15.
                if (bounds.max.y <= roomBounds.min.y + 0.5f || bounds.max.y >= roomBounds.max.y - 0.15f)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // set desk top height to the larger of desk top height and bounds max y.
                deskTopHeight = Mathf.Max(deskTopHeight, bounds.max.y);
                // increase samples by one.
                samples++;
            // finish the for statement block.
            }

            // return samples is greater than 0 to the caller.
            return samples > 0;
        // finish the method try get desk top height block.
        }

        // method: find child.
        // search descendants for an exact object name so setup can locate an existing scene object.
        private static Transform FindChild(Transform root, string childName)
        // begin the method find child block.
        {
            // run the following statement only when root is missing.
            if (root == null)
                // return no object to the caller.
                return null;
            // run the following statement only when root name equals child name.
            if (root.name == childName)
                // return root to the caller.
                return root;

            // repeat this loop while i is less than root child count. increase i by one after each pass.
            for (int i = 0; i < root.childCount; i++)
            // begin the for statement block.
            {
                // store found as find child using root: get child using i, child name.
                Transform found = FindChild(root.GetChild(i), childName);
                // run the following statement only when found exists.
                if (found != null)
                    // return found to the caller.
                    return found;
            // finish the for statement block.
            }
            // return no object to the caller.
            return null;
        // finish the method find child block.
        }
    // finish the class classroom asset runtime setup block.
    }
// finish the namespace block.
}
