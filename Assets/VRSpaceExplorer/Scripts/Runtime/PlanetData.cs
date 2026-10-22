using System;
using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Immutable-ish description of a single planet destination.
    /// Plain [Serializable] class so it can be edited in the Inspector and
    /// created procedurally by <see cref="GalaxySceneGenerator"/>.
    /// </summary>
    [Serializable]
    public class PlanetData
    {
        [Tooltip("Display name shown on the marker, label and UI.")]
        public string planetName = "New Planet";

        [TextArea(2, 4)]
        [Tooltip("Short flavour description shown in the destination list.")]
        public string description = "An unexplored world.";

        [Tooltip("Radius of the planet mesh in world units.")]
        [Min(0.1f)] public float radius = 2f;

        [Tooltip("Distance from the sun / orbit centre.")]
        [Min(1f)] public float orbitRadius = 40f;

        [Tooltip("Orbital revolution speed in degrees per second.")]
        public float orbitSpeed = 4f;

        [Tooltip("Self-rotation speed in degrees per second.")]
        public float spinSpeed = 12f;

        [Tooltip("Base surface colour.")]
        public Color surfaceColor = Color.gray;

        [Tooltip("Emissive glow colour used for the atmosphere and marker.")]
        public Color emissionColor = Color.cyan;

        [Tooltip("Emission intensity multiplier.")]
        [Min(0f)] public float emissionStrength = 0.35f;

        [Header("Rings (Saturn style)")]
        [Tooltip("When true an annulus ring mesh is generated around the planet.")]
        public bool hasRings = false;

        [Tooltip("Inner radius of the ring measured from the planet centre.")]
        public float ringInnerRadius = 3f;

        [Tooltip("Outer radius of the ring measured from the planet centre.")]
        public float ringOuterRadius = 5f;

        [Tooltip("Tint colour of the ring.")]
        public Color ringColor = new Color(0.85f, 0.72f, 0.5f, 1f);

        /// <summary>Snapshot of the planet's current world position (updated by manager when needed).</summary>
        public Vector3 CurrentWorldPosition { get; set; }
    }
}
