using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Helper placed on every planet row button. It exists so the scene
    /// generator can wire a persistent (serialized) onClick event with an
    /// integer planet index, which UnityEvent&lt;int&gt; cannot do directly.
    /// </summary>
    public class PlanetSelectionButton : MonoBehaviour
    {
        [SerializeField] private int planetIndex = -1;
        [SerializeField] private GalaxyManager manager;

        /// <summary>Configured by <see cref="GalaxySceneGenerator"/>.</summary>
        public void Configure(GalaxyManager galaxyManager, int index)
        {
            manager = galaxyManager;
            planetIndex = index;
        }

        /// <summary>Persistent button callback (zero arguments).</summary>
        public void SelectPressed()
        {
            if (manager != null && planetIndex >= 0)
            {
                manager.SelectPlanet(planetIndex);
            }
            else
            {
                Debug.LogWarning($"[VR Space Explorer] PlanetSelectionButton is not configured correctly on '{name}'.");
            }
        }
    }
}
