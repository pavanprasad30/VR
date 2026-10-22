#if VSE_XR_AVAILABLE && VSE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.XR.Management;

namespace VRSpaceExplorer.XR
{
    /// <summary>
    /// Activates the XR rig only when an XR loader is actually running.
    /// On a desktop without a headset the XR rig is removed and the normal
    /// desktop rig keeps working, so one scene supports both modes.
    /// </summary>
    public class XRRigBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject xrRig;
        [SerializeField] private GameObject desktopRig;
        [SerializeField] private Canvas worldCanvas;

        public void Configure(GameObject xr, GameObject desktop, Canvas vrCanvas)
        {
            xrRig = xr;
            desktopRig = desktop;
            worldCanvas = vrCanvas;
        }

        private void Awake()
        {
            if (IsXRLoaderActive())
            {
                EnableXRMode();
            }
            else
            {
                EnableDesktopMode();
            }
        }

        private static bool IsXRLoaderActive()
        {
            try
            {
                return XRGeneralSettings.Instance != null
                    && XRGeneralSettings.Instance.Manager != null
                    && XRGeneralSettings.Instance.Manager.activeLoader != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void EnableXRMode()
        {
            if (xrRig != null)
            {
                xrRig.SetActive(true);
            }
            if (desktopRig != null)
            {
                desktopRig.SetActive(false);
            }

            XRRigFactory.InstallXRUIInputModule();
            if (worldCanvas != null)
            {
                XRRigFactory.ConfigureWorldCanvas(worldCanvas);
            }

            // Screen-space overlay canvases do not render in a headset.
            GameObject desktopCanvas = GameObject.Find("Desktop Canvas (Screen Space)");
            if (desktopCanvas != null)
            {
                desktopCanvas.SetActive(false);
            }

            RedirectTravelController();
            Debug.Log("[VR Space Explorer] XR headset detected - XR Origin rig activated.");
        }

        private void EnableDesktopMode()
        {
            if (xrRig != null)
            {
                Destroy(xrRig);
            }
            if (desktopRig != null)
            {
                desktopRig.SetActive(true);
            }

            Debug.Log("[VR Space Explorer] No XR headset detected - desktop controls active.");
        }

        /// <summary>
        /// Points the shared travel controller at the XR rig root. Done via
        /// reflection so this assembly does not reference Assembly-CSharp.
        /// </summary>
        private void RedirectTravelController()
        {
            if (xrRig == null)
            {
                return;
            }

            Type travelType = Type.GetType("VRSpaceExplorer.SpaceTravelController, Assembly-CSharp");
            if (travelType == null)
            {
                return;
            }

            UnityEngine.Object travel = FindObjectOfType(travelType);
            if (travel is Component travelComponent)
            {
                travelComponent.SendMessage("SetTraveler", xrRig.transform, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
#endif
