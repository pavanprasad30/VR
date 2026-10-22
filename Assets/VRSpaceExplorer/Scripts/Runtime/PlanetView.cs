using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Runtime view attached to every generated planet root.
    /// Handles selection highlighting through MaterialPropertyBlocks so the
    /// shared planet materials are never instantiated or permanently mutated.
    /// </summary>
    public class PlanetView : MonoBehaviour
    {
        [SerializeField] private int planetIndex = -1;
        [SerializeField] private PlanetData data;
        [SerializeField] private Renderer[] highlightedRenderers;
        [SerializeField] private GameObject selectionMarker;
        [SerializeField] private Light markerLight;

        private MaterialPropertyBlock propertyBlock;
        private Color baseEmission;
        private bool selected;

        /// <summary>Index into <see cref="GalaxyManager.Planets"/>.</summary>
        public int PlanetIndex => planetIndex;

        /// <summary>The data describing this planet.</summary>
        public PlanetData Data => data;

        /// <summary>Called by the scene generator after construction.</summary>
        public void Initialize(int index, PlanetData planetData, Renderer[] glowRenderers, GameObject marker, Light light = null)
        {
            planetIndex = index;
            data = planetData;
            highlightedRenderers = glowRenderers;
            selectionMarker = marker;
            markerLight = light;
            baseEmission = planetData != null ? planetData.emissionColor * planetData.emissionStrength : Color.black;
            propertyBlock = new MaterialPropertyBlock();

            SetSelected(false);
        }

        /// <summary>Toggle the glowing destination marker and emission boost.</summary>
        public void SetSelected(bool isSelected)
        {
            selected = isSelected;

            if (selectionMarker != null)
            {
                selectionMarker.SetActive(isSelected);
            }

            if (markerLight != null)
            {
                markerLight.enabled = isSelected;
            }

            if (highlightedRenderers == null)
            {
                return;
            }

            foreach (Renderer r in highlightedRenderers)
            {
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(propertyBlock);
                Color glow = isSelected ? baseEmission * 3.2f : baseEmission;
                propertyBlock.SetColor(ShaderUtilities.EmissionColorId, glow);
                r.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>Current selection state (used by tests/debugging).</summary>
        public bool IsSelected => selected;
    }

    /// <summary>Cached shader property identifiers to avoid string lookups at runtime.</summary>
    internal static class ShaderUtilities
    {
        public static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        public static readonly int BaseColorId = Shader.PropertyToID("_Color");
    }
}
