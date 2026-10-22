using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRSpaceExplorer;

namespace VRSpaceExplorer.EditorTools
{
    /// <summary>Builds the desktop player rig and, when available, the optional XR rig.</summary>
    public static partial class GalaxySceneGenerator
    {
        public static void BuildPlayerRig(BuildContext context)
        {
            Vector3 startPosition = context.hubArrival.position + new Vector3(0f, 2f, -6f);
            startPosition = context.manager.ClampToBounds(startPosition);

            var rig = new GameObject("Player Rig (Desktop)");
            rig.transform.SetParent(context.root.transform, false);
            rig.transform.position = startPosition;
            rig.transform.rotation = Quaternion.LookRotation((Vector3.zero - startPosition).normalized, Vector3.up);
            context.desktopRig = rig;

            var pivot = new GameObject("Camera Pivot");
            pivot.transform.SetParent(rig.transform, false);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = GalaxyConfig.MainCameraTag;
            cameraObject.transform.SetParent(pivot.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.016f, 0.04f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;
            camera.fieldOfView = 65f;
            cameraObject.AddComponent<AudioListener>();
            context.desktopCamera = camera;

            var selector = cameraObject.AddComponent<PlanetSelector>();
            selector.Initialize(camera, context.manager);

            rig.AddComponent<SpacePlayerController>();
        }

        /// <summary>
        /// Loads the XR assembly through pure reflection. If the XR packages
        /// are absent the type lookup fails gracefully and desktop play is
        /// completely unaffected.
        /// </summary>
        public static void BuildOptionalXR(BuildContext context)
        {
            Type factoryType = Type.GetType("VRSpaceExplorer.XR.XRRigFactory, VRSpaceExplorer.XR");
            if (factoryType == null)
            {
                Debug.Log("[VR Space Explorer] XR packages not detected - desktop-only scene generated. " +
                          "Install/restore the OpenXR and XR Interaction Toolkit packages to enable VR.");
                return;
            }

            try
            {
                MethodInfo buildMethod = factoryType.GetMethod("BuildRig", BindingFlags.Public | BindingFlags.Static);
                if (buildMethod == null)
                {
                    throw new MissingMethodException(factoryType.FullName, "BuildRig");
                }

                var xrRig = (GameObject)buildMethod.Invoke(null, null);
                xrRig.name = "Player Rig (XR)";
                xrRig.transform.SetParent(context.root.transform, false);
                xrRig.transform.position = context.desktopRig.transform.position;
                xrRig.transform.rotation = context.desktopRig.transform.rotation;
                xrRig.SetActive(false);

                if (context.vrCanvas != null)
                {
                    MethodInfo canvasMethod = factoryType.GetMethod("ConfigureWorldCanvas");
                    canvasMethod?.Invoke(null, new object[] { context.vrCanvas });
                }

                Type bootstrapType = Type.GetType("VRSpaceExplorer.XR.XRRigBootstrap, VRSpaceExplorer.XR");
                if (bootstrapType == null)
                {
                    Debug.LogWarning("[VR Space Explorer] XRRigBootstrap type missing - XR rig will stay inactive.");
                    return;
                }

                var bootstrapObject = new GameObject("XR Bootstrap");
                bootstrapObject.transform.SetParent(context.root.transform, false);
                Component bootstrap = bootstrapObject.AddComponent(bootstrapType);
                MethodInfo configure = bootstrapType.GetMethod("Configure");
                configure?.Invoke(bootstrap, new object[] { xrRig, context.desktopRig, context.vrCanvas });

                Debug.Log("[VR Space Explorer] XR rig generated. It activates automatically at runtime when an XR loader is present.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[VR Space Explorer] XR rig generation failed, continuing with desktop rig: {exception.Message}");
            }
        }
    }
}
