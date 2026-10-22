using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRSpaceExplorer;

namespace VRSpaceExplorer.EditorTools
{
    /// <summary>
    /// One-click builder for the complete "VR Space Explorer" experience.
    /// Menu: Tools/VR Space Explorer/Generate Galaxy Scene.
    ///
    /// The class is split into partials by concern:
    ///   GalaxySceneGenerator.cs  - orchestration + saving (this file)
    ///   GeneratorAssets.cs       - materials, folders and TMP font handling
    ///   GeneratorEnvironment.cs  - stars, sun, planets, station, markers
    ///   GeneratorPlayer.cs       - desktop rig + optional XR rig
    ///   GeneratorUI.cs           - holographic canvas construction + wiring
    /// </summary>
    public static partial class GalaxySceneGenerator
    {
        /// <summary>Shared state passed between the build stages.</summary>
        internal sealed class BuildContext
        {
            public GameObject root;
            public GalaxyManager manager;
            public SpaceTravelController travel;
            public NavigationUI navigationUI;
            public TMP_FontAsset font;

            public readonly List<PlanetData> planetData = new List<PlanetData>();
            public readonly List<Transform> planetAnchors = new List<Transform>();
            public readonly List<Transform> arrivalPoints = new List<Transform>();
            public readonly List<PlanetView> planetViews = new List<PlanetView>();

            public Transform hubAnchor;
            public Transform hubArrival;
            public Transform sunTransform;

            public GameObject desktopRig;
            public Camera desktopCamera;
            public Canvas vrCanvas;
        }

        [MenuItem("Tools/VR Space Explorer/Generate Galaxy Scene")]
        public static void GenerateGalaxyScene()
        {
            BuildContext context = null;
            try
            {
                EnsureProjectFolders();

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                context = new BuildContext
                {
                    font = GetOrCreateFont()
                };

                BuildSystems(context);
                BuildEnvironment(context);
                BuildPlayerRig(context);
                BuildUserInterface(context);
                BuildOptionalXR(context);
                FinalizeAndWire(context);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeGameObject = context.root;
                EditorGUIUtility.PingObject(context.root);

                EditorUtility.DisplayDialog(
                    "VR Space Explorer",
                    "Galaxy scene generated and saved to:\n" + ScenePath +
                    "\n\nPress Play to explore. Desktop: right-mouse look, WASD move, R/F thrust, Esc cancel/help.",
                    "OK");

                Debug.Log($"[VR Space Explorer] Scene generated successfully with {context.planetData.Count} planets.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "VR Space Explorer - Generation Failed",
                    "The galaxy scene could not be generated:\n\n" + exception.Message +
                    "\n\nSee the Console for the full stack trace.",
                    "OK");
            }
        }

        /// <summary>Creates the persistent manager objects that own gameplay state.</summary>
        private static void BuildSystems(BuildContext context)
        {
            context.root = new GameObject(GalaxyConfig.SystemRootName);

            var systemsObject = new GameObject("Systems");
            systemsObject.transform.SetParent(context.root.transform, false);

            context.manager = systemsObject.AddComponent<GalaxyManager>();
            context.travel = systemsObject.AddComponent<SpaceTravelController>();
            context.travel.SetTravelSpeed(GalaxyConfig.DefaultTravelSpeed);

            // Scene-wide ambient lighting (saved with the scene).
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.13f, 0.22f);
            RenderSettings.fog = false;
        }

        /// <summary>Wires serialized references after every object exists.</summary>
        private static void FinalizeAndWire(BuildContext context)
        {
            context.manager.Configure(
                context.planetData,
                context.planetAnchors,
                context.arrivalPoints,
                context.hubAnchor,
                context.hubArrival,
                context.travel,
                context.desktopRig != null ? context.desktopRig.transform : null);

            context.travel.SetTraveler(
                context.desktopRig != null ? context.desktopRig.transform : null);
            context.manager.SetPlanetViews(context.planetViews);

            if (context.desktopRig != null)
            {
                Transform pivot = context.desktopRig.transform.Find("Camera Pivot");
                var playerController = context.desktopRig.GetComponent<SpacePlayerController>();
                if (playerController != null)
                {
                    playerController.Initialize(
                        context.desktopRig.transform,
                        pivot,
                        context.travel,
                        context.manager,
                        context.navigationUI);
                }
            }
        }
    }
}
