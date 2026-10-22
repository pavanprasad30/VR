using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Simple scale/intensity pulse for the selected-destination marker.
    /// Attached only to the active marker so the per-frame cost is minimal.
    /// </summary>
    public class MarkerPulse : MonoBehaviour
    {
        [Tooltip("Pulses per second.")]
        [SerializeField] private float pulseSpeed = 2.5f;

        [Tooltip("Scale multiplier at the peak of the pulse.")]
        [SerializeField] private float peakScale = 1.35f;

        private Vector3 baseScale;
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
            baseScale = cachedTransform.localScale;
        }

        private void Update()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
            float multiplier = Mathf.Lerp(1f, peakScale, wave);
            cachedTransform.localScale = baseScale * multiplier;
        }
    }
}
