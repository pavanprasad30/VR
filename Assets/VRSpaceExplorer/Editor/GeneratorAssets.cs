using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace VRSpaceExplorer.EditorTools
{
    /// <summary>
    /// AssetDatabase helpers: folder creation, reusable procedural materials
    /// and a guaranteed TextMeshPro font asset so the generator never depends
    /// on a manual TMP Essential Resources import.
    /// </summary>
    public static partial class GalaxySceneGenerator
    {
        public const string GeneratedRoot = "Assets/VRSpaceExplorer/Generated";
        public const string MaterialFolder = GeneratedRoot + "/Materials";
        public const string ResourceFolder = GeneratedRoot + "/Resources";
        public const string SceneFolder = "Assets/Scenes";
        public const string ScenePath = SceneFolder + "/GalaxyExplorer.unity";
        private const string FallbackFontPath = ResourceFolder + "/VRSpaceExplorerFallbackFont.asset";

        public static void EnsureProjectFolders()
        {
            EnsureFolder("Assets");
            EnsureFolder("Assets/VRSpaceExplorer");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ResourceFolder);
            EnsureFolder(SceneFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Loads an existing material asset or creates and saves a new one.</summary>
        public static Material GetOrCreateMaterial(string assetName, string shaderName, Action<Material> configure = null)
        {
            string path = $"{MaterialFolder}/{assetName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    throw new InvalidOperationException($"Required shader '{shaderName}' was not found in this Unity build.");
                }

                material = new Material(shader) { name = assetName };
                configure?.Invoke(material);
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
        }

        /// <summary>
        /// Returns the TMP default font when TMP Essential Resources are
        /// imported, otherwise creates and persists a dynamic SDF font asset
        /// from the built-in Arial so text never renders blank.
        /// </summary>
        public static TMP_FontAsset GetOrCreateFont()
        {
            TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null)
            {
                return defaultFont;
            }

            TMP_FontAsset savedFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            if (savedFallback != null)
            {
                return savedFallback;
            }

            Font systemFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (systemFont == null)
            {
                systemFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (systemFont == null)
            {
                throw new InvalidOperationException(
                    "No built-in font could be loaded. Import TextMeshPro Essential Resources via Window > TextMeshPro > Import TMP Essential Resources.");
            }

            TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(systemFont);
            fallback.name = "VRSpaceExplorer Fallback Font";

            AssetDatabase.CreateAsset(fallback, FallbackFontPath);
            if (fallback.material != null)
            {
                AssetDatabase.AddObjectToAsset(fallback.material, FallbackFontPath);
            }

            Debug.LogWarning("[VR Space Explorer] TMP Essential Resources not found - created a dynamic fallback font. " +
                             "For the default font run Window > TextMeshPro > Import TMP Essential Resources.");
            return fallback;
        }
    }
}
