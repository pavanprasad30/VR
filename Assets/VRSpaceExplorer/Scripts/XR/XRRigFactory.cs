#if VSE_XR_AVAILABLE && VSE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRSpaceExplorer.XR
{
    /// <summary>
    /// Builds a complete, comfort-oriented XR Origin rig entirely from code:
    /// head tracking, two ray-interactor controllers, UI input module support
    /// and smooth/snap locomotion. Invoked through reflection by the scene
    /// generator so the Editor code never takes a hard XR package dependency.
    /// </summary>
    public static class XRRigFactory
    {
        /// <summary>Reflection-friendly type name used by the scene generator.</summary>
        public const string FactoryTypeName = "VRSpaceExplorer.XR.XRRigFactory, VRSpaceExplorer.XR";

        public const string BootstrapTypeName = "VRSpaceExplorer.XR.XRRigBootstrap, VRSpaceExplorer.XR";

        /// <summary>Builds the rig at the world origin. The generator positions it afterwards.</summary>
        public static GameObject BuildRig()
        {
            var rig = new GameObject("XR Origin Rig");

            var interactionManager = rig.AddComponent<XRInteractionManager>();
            var xrOrigin = rig.AddComponent<XROrigin>();
            var inputActionManager = rig.AddComponent<InputActionManager>();
            var characterController = rig.AddComponent<CharacterController>();
            characterController.center = new Vector3(0f, 1f, 0f);
            characterController.height = 1.8f;
            characterController.radius = 0.25f;
            characterController.skinWidth = 0.02f;
            rig.AddComponent<CharacterControllerDriver>();

            // Camera offset (room-scale pivot).
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);

            // Head camera.
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(offset.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.016f, 0.04f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2000f;
            cameraObject.AddComponent<AudioListener>();
            var poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
            poseDriver.poseSource = TrackedPoseDriver.TrackedPose.Head;

            // Shared input actions for both hands.
            InputActionAsset actionAsset = CreateInputActions(out InputAction leftMove, out InputAction rightTurn);
            actionAsset.Enable();
            inputActionManager.actionAssets.Add(actionAsset);

            BuildController("Left Hand Controller", XRNodeRole.LeftHand, offset.transform, interactionManager, actionAsset, camera.transform);
            BuildController("Right Hand Controller", XRNodeRole.RightHand, offset.transform, interactionManager, actionAsset, camera.transform);

            xrOrigin.CameraOffset = offset.transform;
            xrOrigin.Camera = camera;

            var locomotion = rig.AddComponent<XRComfortLocomotion>();
            locomotion.Configure(characterController, camera.transform, leftMove, rightTurn);

            return rig;
        }

        /// <summary>
        /// Adds a TrackedDeviceGraphicRaycaster to a world-space canvas so
        /// controller rays can interact with uGUI. Reflection keeps the exact
        /// namespace resilient across XRI minor versions.
        /// </summary>
        public static bool ConfigureWorldCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            Type raycasterType =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit")
                ?? Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");

            if (raycasterType == null || canvas.GetComponent(raycasterType) != null)
            {
                return raycasterType != null;
            }

            canvas.gameObject.AddComponent(raycasterType);
            return true;
        }

        /// <summary>Swaps the standalone event module for the XR UI input module.</summary>
        public static void InstallXRUIInputModule()
        {
            // FindFirstObjectByType is the non-deprecated lookup in Unity 6.
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                return;
            }

            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                UnityEngine.Object.Destroy(standalone);
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private static void BuildController(
            string controllerName,
            XRNodeRole hand,
            Transform parent,
            XRInteractionManager manager,
            InputActionAsset actionAsset,
            Transform headTransform)
        {
            string prefix = hand == XRNodeRole.LeftHand ? "Left" : "Right";
            var controllerObject = new GameObject(controllerName);
            controllerObject.transform.SetParent(parent, false);

            var controller = controllerObject.AddComponent<ActionBasedController>();
            controller.positionAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_Position", true));
            controller.rotationAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_Rotation", true));
            controller.trackingStateAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_TrackingState", true));
            controller.selectAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_Trigger", true));
            controller.activateAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_Trigger", true));
            controller.uiPressAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_Trigger", true));
            controller.gripAction = new InputActionProperty(actionAsset.FindAction($"{prefix}_Grip", true));

            // Simple stylised controller visual (no art assets required).
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Controller Visual";
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(controllerObject.transform, false);
            visual.transform.localScale = new Vector3(0.02f, 0.08f, 0.02f);
            visual.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            var visualRenderer = visual.GetComponent<MeshRenderer>();
            visualRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = hand == XRNodeRole.LeftHand
                    ? new Color(0.2f, 0.8f, 1f)
                    : new Color(0.6f, 0.4f, 1f)
            };

            var attach = new GameObject("Attach Transform");
            attach.transform.SetParent(controllerObject.transform, false);

            var lineRenderer = controllerObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0.1f, 0.9f, 1f, 0.8f);
            lineRenderer.endColor = new Color(0.5f, 0.3f, 1f, 0.2f);

            var rayInteractor = controllerObject.AddComponent<XRRayInteractor>();
            rayInteractor.attachTransform = attach.transform;
            rayInteractor.interactionManager = manager;

            // RequireComponent(LineRenderer) makes the visual resolve the
            // line automatically; assigning it explicitly would couple us
            // to version-specific property names.
            controllerObject.AddComponent<XRInteractorLineVisual>();
        }

        private static InputActionAsset CreateInputActions(out InputAction leftMove, out InputAction rightTurn)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "VR Space Explorer XR Actions";
            InputActionMap map = asset.AddActionMap("XR Controls");

            AddVector3(map, "Left_Position", "<XRController>{LeftHand}/pointerPosition");
            AddQuaternion(map, "Left_Rotation", "<XRController>{LeftHand}/pointerRotation");
            AddInteger(map, "Left_TrackingState", "<XRController>{LeftHand}/trackingState");
            AddButton(map, "Left_Trigger", "<XRController>{LeftHand}/triggerPressed");
            AddButton(map, "Left_Grip", "<XRController>{LeftHand}/gripPressed");
            leftMove = AddVector2(map, "Left_Thumbstick", "<XRController>{LeftHand}/thumbstick");

            AddVector3(map, "Right_Position", "<XRController>{RightHand}/pointerPosition");
            AddQuaternion(map, "Right_Rotation", "<XRController>{RightHand}/pointerRotation");
            AddInteger(map, "Right_TrackingState", "<XRController>{RightHand}/trackingState");
            AddButton(map, "Right_Trigger", "<XRController>{RightHand}/triggerPressed");
            AddButton(map, "Right_Grip", "<XRController>{RightHand}/gripPressed");
            rightTurn = AddVector2(map, "Right_Thumbstick", "<XRController>{RightHand}/thumbstick");

            return asset;
        }

        private static InputAction AddVector3(InputActionMap map, string name, string binding)
        {
            InputAction action = map.AddAction(name, InputActionType.Value, expectedControlType: "Vector3");
            action.AddBinding(binding);
            return action;
        }

        private static InputAction AddQuaternion(InputActionMap map, string name, string binding)
        {
            InputAction action = map.AddAction(name, InputActionType.Value, expectedControlType: "Quaternion");
            action.AddBinding(binding);
            return action;
        }

        private static InputAction AddInteger(InputActionMap map, string name, string binding)
        {
            InputAction action = map.AddAction(name, InputActionType.Value, expectedControlType: "Integer");
            action.AddBinding(binding);
            return action;
        }

        private static InputAction AddButton(InputActionMap map, string name, string binding)
        {
            InputAction action = map.AddAction(name, InputActionType.Button);
            action.AddBinding(binding);
            return action;
        }

        private static InputAction AddVector2(InputActionMap map, string name, string binding)
        {
            InputAction action = map.AddAction(name, InputActionType.Value, expectedControlType: "Vector2");
            action.AddBinding(binding);
            return action;
        }
    }
}
#endif
