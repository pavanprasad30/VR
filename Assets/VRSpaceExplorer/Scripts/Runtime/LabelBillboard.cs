using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Makes a world-space label (TextMeshPro 3D) face the active camera.
    /// Caches the camera lookup; zero allocations per frame.
    /// </summary>
    public class LabelBillboard : MonoBehaviour
    {
        [Tooltip("Keep the label upright instead of copying the camera pitch/roll.")]
        [SerializeField] private bool keepUpright = true;

        private Transform cachedCamera;

        private void LateUpdate()
        {
            if (cachedCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    cachedCamera = cam.transform;
                }
            }

            if (cachedCamera == null)
            {
                return;
            }

            if (keepUpright)
            {
                // Face the camera but remain vertically upright.
                Vector3 direction = cachedCamera.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
            else
            {
                transform.rotation = cachedCamera.rotation;
            }
        }
    }
}
