using UnityEngine;
using UnityEngine.EventSystems;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Desktop mouse selection for 3D planets. Clicks over uGUI elements are
    /// ignored so buttons and planets never compete for the same click.
    /// </summary>
    public class PlanetSelector : MonoBehaviour
    {
        [Tooltip("Camera used for the selection ray (the desktop camera).")]
        [SerializeField] private Camera selectionCamera;

        [SerializeField] private GalaxyManager manager;

        /// <summary>Configured by the scene generator.</summary>
        public void Initialize(Camera camera, GalaxyManager galaxyManager)
        {
            selectionCamera = camera;
            manager = galaxyManager;
        }

        private void Update()
        {
            if (selectionCamera == null || manager == null)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            // Never steal clicks intended for the canvas.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = selectionCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            PlanetView view = hit.collider.GetComponentInParent<PlanetView>();
            if (view != null && view.PlanetIndex >= 0)
            {
                manager.SelectPlanet(view.PlanetIndex);
            }
        }
    }
}
