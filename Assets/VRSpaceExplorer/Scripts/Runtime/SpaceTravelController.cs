using System;
using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Executes smooth, cancellable point-to-point travel of the player rig.
    /// Works identically for the desktop rig and an XR Origin because it only
    /// ever moves the rig root transform, never the tracked camera directly.
    /// </summary>
    public class SpaceTravelController : MonoBehaviour
    {
        public enum TravelState
        {
            Idle,
            Travelling,
            Cancelled
        }

        [Tooltip("Rig root that is moved during travel (desktop rig or XR Origin).")]
        [SerializeField] private Transform traveler;

        [Tooltip("Travel speed in world units per second. Configurable in the Inspector.")]
        [Min(1f)]
        [SerializeField] private float travelSpeed = GalaxyConfig.DefaultTravelSpeed;

        [Tooltip("Height of the parabolic arc applied during travel.")]
        [SerializeField] private float arcHeight = 10f;

        [Tooltip("Minimum trip duration in seconds so very short hops stay visible.")]
        [SerializeField] private float minimumDuration = 1.2f;

        private TravelState state = TravelState.Idle;
        private Transform target;
        private Vector3 startPosition;
        private float duration;
        private float elapsed;
        private string destinationLabel = string.Empty;
        private int destinationIndex = GalaxyConfig.NoSelection;

        /// <summary>Raised every frame while travelling with normalized 0..1 progress.</summary>
        public event Action<float> ProgressChanged;

        /// <summary>Raised when a trip completes (true) or is cancelled (false).</summary>
        public event Action<bool, string, int> TravelFinished;

        /// <summary>Raised whenever Idle/Travelling changes so the UI can refresh buttons.</summary>
        public event Action<TravelState> StateChanged;

        public bool IsTravelling => state == TravelState.Travelling;
        public TravelState State => state;
        public Transform CurrentTarget => target;
        public string CurrentDestinationLabel => destinationLabel;
        public float TravelSpeed => travelSpeed;

        /// <summary>Wired by the scene generator; also used by the XR bootstrap.</summary>
        public void SetTraveler(Transform rig)
        {
            traveler = rig;
        }

        /// <summary>Runtime speed override (the HUD could expose a slider later).</summary>
        public void SetTravelSpeed(float unitsPerSecond)
        {
            travelSpeed = Mathf.Max(1f, unitsPerSecond);
        }

        /// <summary>Start a trip to a target transform. Handles moving targets (orbiting planets).</summary>
        public void StartTravel(Transform destination, string label, int planetIndex)
        {
            if (destination == null)
            {
                Debug.LogError("[VR Space Explorer] Travel destination is null.");
                return;
            }

            if (traveler == null)
            {
                Debug.LogError("[VR Space Explorer] Player rig is not assigned to SpaceTravelController.");
                return;
            }

            target = destination;
            destinationLabel = label ?? "Unknown Destination";
            destinationIndex = planetIndex;
            startPosition = traveler.position;

            float distance = Vector3.Distance(startPosition, target.position);
            duration = Mathf.Max(minimumDuration, distance / Mathf.Max(1f, travelSpeed));
            elapsed = 0f;

            state = TravelState.Travelling;
            StateChanged?.Invoke(state);
        }

        /// <summary>Safe cancellation: the rig simply stops where it is.</summary>
        public void CancelTravel()
        {
            if (state != TravelState.Travelling)
            {
                return;
            }

            state = TravelState.Cancelled;
            StateChanged?.Invoke(state);
            state = TravelState.Idle;
            target = null;
            StateChanged?.Invoke(state);

            ProgressChanged?.Invoke(0f);
            TravelFinished?.Invoke(false, destinationLabel, destinationIndex);
        }

        private void Update()
        {
            if (state != TravelState.Travelling || target == null || traveler == null)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float easedT = SmoothStep(rawT);

            // Lerp toward the target's LIVE position so orbiting planets never get missed.
            Vector3 endPosition = target.position;
            Vector3 position = Vector3.Lerp(startPosition, endPosition, easedT);

            // Parabolic vertical arc, fading in and out.
            float arc = Mathf.Sin(Mathf.PI * rawT) * arcHeight;
            position.y += arc;

            traveler.position = position;
            ProgressChanged?.Invoke(rawT);

            if (rawT >= 1f)
            {
                traveler.position = endPosition;
                state = TravelState.Idle;
                StateChanged?.Invoke(state);
                ProgressChanged?.Invoke(1f);
                TravelFinished?.Invoke(true, destinationLabel, destinationIndex);
                target = null;
            }
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
