using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Central brain of the experience: planet registry, selection state,
    /// current location tracking and travel requests. All UI reacts to the
    /// C# events declared here, keeping the flow decoupled and testable.
    /// </summary>
    public class GalaxyManager : MonoBehaviour
    {
        [Header("Destinations")]
        [Tooltip("All explorable planets, ordered as shown in the UI.")]
        [SerializeField] private List<PlanetData> planets = new List<PlanetData>();

        [Tooltip("Planet root transforms, one per entry in planets.")]
        [SerializeField] private List<Transform> planetAnchors = new List<Transform>();

        [Tooltip("Arrival points near each planet (optional but generated).")]
        [SerializeField] private List<Transform> planetArrivalPoints = new List<Transform>();

        [Tooltip("Hub anchor and its arrival point.")]
        [SerializeField] private Transform hubAnchor;
        [SerializeField] private Transform hubArrivalPoint;

        [Tooltip("3D planet views, one per planet. Drives selection markers.")]
        [SerializeField] private List<PlanetView> planetViews = new List<PlanetView>();

        [Header("Systems")]
        [SerializeField] private SpaceTravelController travelController;
        [SerializeField] private Transform playerRig;

        // ----- Runtime state -----
        private int selectedIndex = GalaxyConfig.NoSelection;
        private int currentPlanetIndex = GalaxyConfig.HubLocationIndex;

        // ----- Events (subscribed by NavigationUI in OnEnable/OnDisable) -----
        public event Action<int> SelectionChanged;
        public event Action LocationChanged;
        public event Action<string> StatusMessage;
        public event Action<string> ToastMessage;

        public IReadOnlyList<PlanetData> Planets => planets;
        public int PlanetCount => planets.Count;
        public int SelectedIndex => selectedIndex;
        public int CurrentPlanetIndex => currentPlanetIndex;
        public bool IsAtHub => currentPlanetIndex == GalaxyConfig.HubLocationIndex;
        public SpaceTravelController Travel => travelController;
        public Transform PlayerRig => playerRig;
        public Transform HubAnchor => hubAnchor;

        /// <summary>Called by the generator once after wiring the scene.</summary>
        public void Configure(
            List<PlanetData> data,
            List<Transform> anchors,
            List<Transform> arrivalPoints,
            Transform hub,
            Transform hubArrival,
            SpaceTravelController travel,
            Transform rig)
        {
            planets = data ?? new List<PlanetData>();
            planetAnchors = anchors ?? new List<Transform>();
            planetArrivalPoints = arrivalPoints ?? new List<Transform>();
            hubAnchor = hub;
            hubArrivalPoint = hubArrival;
            travelController = travel;
            playerRig = rig;
            selectedIndex = GalaxyConfig.NoSelection;
            currentPlanetIndex = GalaxyConfig.HubLocationIndex;
        }

        /// <summary>Registers the 3D planet views so markers track the selection.</summary>
        public void SetPlanetViews(List<PlanetView> views)
        {
            planetViews = views ?? new List<PlanetView>();
        }

        private void OnEnable()
        {
            if (travelController != null)
            {
                travelController.TravelFinished += HandleTravelFinished;
            }
        }

        private void OnDisable()
        {
            if (travelController != null)
            {
                travelController.TravelFinished -= HandleTravelFinished;
            }
        }

        // ----- Selection API (used by UI buttons and 3D planet clicks) -----

        /// <summary>Select a planet by index. Safe to call repeatedly.</summary>
        public void SelectPlanet(int index)
        {
            if (index < 0 || index >= planets.Count)
            {
                StatusMessage?.Invoke("That destination could not be found.");
                return;
            }

            selectedIndex = index;
            UpdatePlanetMarkers(index);
            PlanetData data = planets[index];
            SelectionChanged?.Invoke(index);
            StatusMessage?.Invoke($"Destination selected: {data.planetName}");
        }

        private void UpdatePlanetMarkers(int selected)
        {
            for (int i = 0; i < planetViews.Count; i++)
            {
                if (planetViews[i] != null)
                {
                    planetViews[i].SetSelected(i == selected);
                }
            }
        }

        /// <summary>Begin travelling to the currently selected planet.</summary>
        public void BeginTravel()
        {
            if (selectedIndex == GalaxyConfig.NoSelection)
            {
                StatusMessage?.Invoke("Select a destination before beginning travel.");
                return;
            }

            if (travelController == null)
            {
                Debug.LogError("[VR Space Explorer] SpaceTravelController is not assigned.");
                return;
            }

            if (travelController.IsTravelling)
            {
                StatusMessage?.Invoke("Already travelling. Cancel the current trip first.");
                return;
            }

            Transform arrival = GetArrivalPoint(selectedIndex);
            PlanetData data = planets[selectedIndex];
            travelController.StartTravel(arrival, data.planetName, selectedIndex);
            StatusMessage?.Invoke($"Travelling to {data.planetName}...");
        }

        /// <summary>Travel back to the central space station.</summary>
        public void ReturnToHub()
        {
            if (travelController == null)
            {
                Debug.LogError("[VR Space Explorer] SpaceTravelController is not assigned.");
                return;
            }

            if (travelController.IsTravelling)
            {
                StatusMessage?.Invoke("Cancel the current trip before returning to the hub.");
                return;
            }

            Transform arrival = hubArrivalPoint != null ? hubArrivalPoint : hubAnchor;
            travelController.StartTravel(arrival, GalaxyConfig.HubName, GalaxyConfig.HubLocationIndex);
            StatusMessage?.Invoke("Returning to the Central Space Station...");
        }

        private void HandleTravelFinished(bool completed, string destinationLabel, int destinationIndex)
        {
            if (!completed)
            {
                StatusMessage?.Invoke($"Travel to {destinationLabel} cancelled. You are drifting in deep space.");
                return;
            }

            currentPlanetIndex = destinationIndex;
            LocationChanged?.Invoke();
            ToastMessage?.Invoke($"Arrived at {destinationLabel}");
            StatusMessage?.Invoke($"Docked at {destinationLabel}.");
        }

        // ----- Queries used by the HUD -----

        /// <summary>Distance from the player rig to a planet (world units).</summary>
        public float GetDistanceToPlanet(int index)
        {
            if (index < 0 || index >= planetAnchors.Count || planetAnchors[index] == null || playerRig == null)
            {
                return -1f;
            }

            return Vector3.Distance(playerRig.position, planetAnchors[index].position);
        }

        /// <summary>Distance from the player rig to the hub.</summary>
        public float GetDistanceToHub()
        {
            Transform target = hubAnchor != null ? hubAnchor : null;
            if (target == null || playerRig == null)
            {
                return -1f;
            }

            return Vector3.Distance(playerRig.position, target.position);
        }

        /// <summary>Human readable name of the current location.</summary>
        public string GetCurrentLocationName()
        {
            if (currentPlanetIndex == GalaxyConfig.HubLocationIndex)
            {
                return GalaxyConfig.HubName;
            }

            if (currentPlanetIndex >= 0 && currentPlanetIndex < planets.Count)
            {
                return planets[currentPlanetIndex].planetName;
            }

            return "Deep Space";
        }

        /// <summary>Planet anchor transform for an index.</summary>
        public Transform GetPlanetAnchor(int index)
        {
            if (index < 0 || index >= planetAnchors.Count)
            {
                return null;
            }

            return planetAnchors[index];
        }

        private Transform GetArrivalPoint(int index)
        {
            if (index >= 0 && index < planetArrivalPoints.Count && planetArrivalPoints[index] != null)
            {
                return planetArrivalPoints[index];
            }

            return planetAnchors[index];
        }

        /// <summary>
        /// Clamps a world position inside the navigation bounds so the player
        /// can never drift infinitely far from the galaxy.
        /// </summary>
        public Vector3 ClampToBounds(Vector3 position)
        {
            Vector3 flat = new Vector3(position.x, 0f, position.z);
            if (flat.magnitude > GalaxyConfig.GalaxyBoundsRadius)
            {
                flat = flat.normalized * GalaxyConfig.GalaxyBoundsRadius;
            }

            position.x = flat.x;
            position.z = flat.z;
            position.y = Mathf.Clamp(position.y, GalaxyConfig.MinHeight, GalaxyConfig.MaxHeight);
            return position;
        }
    }
}
