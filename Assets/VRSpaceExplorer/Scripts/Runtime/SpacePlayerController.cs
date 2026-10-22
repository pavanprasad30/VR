using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Desktop first-person rig: right-mouse-drag look, WASD + R/F translation,
    /// Shift boost. Movement is suspended while auto-travel is active so the
    /// two systems never fight over the rig transform. All positions are
    /// clamped to <see cref="GalaxyConfig.GalaxyBoundsRadius"/>.
    /// </summary>
    public class SpacePlayerController : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform rigRoot;
        [SerializeField] private Transform cameraPivot;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float boostMultiplier = 2.5f;
        [SerializeField] private float mouseSensitivity = 3.5f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 75f;

        [Header("References")]
        [SerializeField] private SpaceTravelController travelController;
        [SerializeField] private GalaxyManager galaxyManager;
        [SerializeField] private NavigationUI navigationUI;

        private float yaw;
        private float pitch;

        /// <summary>Wired by the scene generator.</summary>
        public void Initialize(Transform rig, Transform pivot, SpaceTravelController travel, GalaxyManager manager, NavigationUI ui)
        {
            rigRoot = rig;
            cameraPivot = pivot;
            travelController = travel;
            galaxyManager = manager;
            navigationUI = ui;

            yaw = rigRoot != null ? rigRoot.eulerAngles.y : 0f;
            pitch = cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleSystemKeys();
        }

        private void HandleLook()
        {
            // Hold the right mouse button to look around (keeps the left button free for UI/planet clicks).
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            if (rigRoot != null)
            {
                rigRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            if (rigRoot == null)
            {
                return;
            }

            // Auto-travel owns the rig while a trip is in progress.
            if (travelController != null && travelController.IsTravelling)
            {
                return;
            }

            Vector3 input = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                0f,
                Input.GetAxisRaw("Vertical"));

            // R / F for vertical thrust to avoid Space conflicting with focused UI buttons.
            if (Input.GetKey(KeyCode.R))
            {
                input.y += 1f;
            }
            if (Input.GetKey(KeyCode.F))
            {
                input.y -= 1f;
            }

            if (input.sqrMagnitude < 0.0001f)
            {
                return;
            }

            input = Vector3.ClampMagnitude(input, 1f);
            float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                ? moveSpeed * boostMultiplier
                : moveSpeed;

            Transform reference = cameraPivot != null ? cameraPivot : rigRoot;
            Vector3 forward = reference.forward;
            Vector3 right = reference.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = (forward * input.z + right * input.x + Vector3.up * input.y) * (speed * Time.deltaTime);
            Vector3 nextPosition = rigRoot.position + move;

            if (galaxyManager != null)
            {
                nextPosition = galaxyManager.ClampToBounds(nextPosition);
            }

            rigRoot.position = nextPosition;
        }

        private void HandleSystemKeys()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            // Escape cancels an active trip first; otherwise it toggles help.
            if (travelController != null && travelController.IsTravelling)
            {
                travelController.CancelTravel();
            }
            else if (navigationUI != null)
            {
                navigationUI.ToggleHelp();
            }
        }
    }
}
