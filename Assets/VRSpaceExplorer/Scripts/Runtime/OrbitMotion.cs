using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Rotates a planet root around an orbit centre and spins the visible mesh.
    /// One cheap Update per planet; no per-frame allocations or searches.
    /// </summary>
    public class OrbitMotion : MonoBehaviour
    {
        [Tooltip("The body this object revolves around (usually the Sun pivot).")]
        [SerializeField] private Transform orbitCenter;

        [Tooltip("Revolution speed in degrees per second.")]
        [SerializeField] private float orbitSpeedDegreesPerSecond = 4f;

        [Tooltip("The mesh that self-rotates. Can be the same transform.")]
        [SerializeField] private Transform spinTarget;

        [Tooltip("Self-rotation speed in degrees per second.")]
        [SerializeField] private float spinSpeedDegreesPerSecond = 12f;

        /// <summary>Used by the generator to wire the orbit centre.</summary>
        public void Configure(Transform center, float orbitSpeed, float spinSpeed, Transform spin)
        {
            orbitCenter = center;
            orbitSpeedDegreesPerSecond = orbitSpeed;
            spinSpeedDegreesPerSecond = spinSpeed;
            spinTarget = spin != null ? spin : transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (orbitCenter != null)
            {
                transform.RotateAround(orbitCenter.position, Vector3.up, orbitSpeedDegreesPerSecond * dt);
            }

            if (spinTarget != null)
            {
                spinTarget.Rotate(Vector3.up, spinSpeedDegreesPerSecond * dt, Space.Self);
            }
        }
    }
}
