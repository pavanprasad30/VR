#if VSE_XR_AVAILABLE && VSE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRSpaceExplorer.XR
{
    /// <summary>
    /// Comfort-first locomotion for XR: smooth translation on the left
    /// thumbstick and snap turns on the right thumbstick. The rig root is
    /// moved (never the headset) and movement is clamped to the galaxy
    /// navigation bounds. Written against the Input System directly so it
    /// does not depend on a specific XRI locomotion version.
    /// </summary>
    public class XRComfortLocomotion : MonoBehaviour
    {
        // Must stay in sync with GalaxyConfig.GalaxyBoundsRadius.
        private const float GalaxyBoundsRadius = 270f;
        private const float MoveSpeed = 12f;
        private const float SnapAngle = 30f;
        private const float SnapDeadzone = 0.85f;

        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraHead;
        [SerializeField] private InputAction moveAction;
        [SerializeField] private InputAction turnAction;

        private bool snapCooldown;

        public void Configure(CharacterController controller, Transform head, InputAction move, InputAction turn)
        {
            characterController = controller;
            cameraHead = head;
            moveAction = move;
            turnAction = turn;
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            turnAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            turnAction?.Disable();
        }

        private void Update()
        {
            HandleMove();
            HandleSnapTurn();
        }

        private void HandleMove()
        {
            if (characterController == null || cameraHead == null || moveAction == null)
            {
                return;
            }

            Vector2 stick = moveAction.ReadValue<Vector2>();
            if (stick.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector3 forward = cameraHead.forward;
            Vector3 right = cameraHead.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 motion = (forward * stick.y + right * stick.x) * (MoveSpeed * Time.deltaTime);
            characterController.Move(motion);

            // Clamp the rig to the galaxy bounds.
            Vector3 position = transform.position;
            Vector3 flat = new Vector3(position.x, 0f, position.z);
            if (flat.magnitude > GalaxyBoundsRadius)
            {
                flat = flat.normalized * GalaxyBoundsRadius;
                transform.position = new Vector3(flat.x, position.y, flat.z);
            }
        }

        private void HandleSnapTurn()
        {
            if (turnAction == null)
            {
                return;
            }

            float x = turnAction.ReadValue<Vector2>().x;
            if (snapCooldown)
            {
                if (Mathf.Abs(x) < 0.35f)
                {
                    snapCooldown = false;
                }
                return;
            }

            if (Mathf.Abs(x) < SnapDeadzone)
            {
                return;
            }

            transform.Rotate(Vector3.up, x > 0f ? SnapAngle : -SnapAngle, Space.Self);
            snapCooldown = true;
        }
    }
}
#endif
