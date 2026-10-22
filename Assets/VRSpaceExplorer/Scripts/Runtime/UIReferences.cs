using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRSpaceExplorer
{
    /// <summary>
    /// One row in the destination list. Stored as a serializable class so the
    /// scene generator can build rows dynamically and NavigationUI can refresh
    /// them without any hierarchy searches.
    /// </summary>
    [Serializable]
    public class PlanetRow
    {
        public RectTransform root;
        public Image rowBackground;
        public Image colorDot;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI distanceText;
        public Button selectButton;
    }

    /// <summary>
    /// All controls of one canvas. The generator produces two instances:
    /// a screen-space desktop canvas and a world-space VR canvas.
    /// </summary>
    [Serializable]
    public class UIReferences
    {
        public Canvas canvas;
        public RectTransform panelRoot;

        [Header("Header")]
        public TextMeshProUGUI titleText;

        [Header("Destination list")]
        public PlanetRow[] rows;

        [Header("Selection / status panel")]
        public TextMeshProUGUI selectedNameText;
        public TextMeshProUGUI selectedDescriptionText;
        public TextMeshProUGUI currentLocationText;
        public TextMeshProUGUI statusText;

        [Header("Travel progress")]
        public Image progressFill;
        public TextMeshProUGUI progressPercentText;
        public Button beginTravelButton;
        public Button cancelTravelButton;
        public Button returnToHubButton;
        public Button helpButton;

        [Header("Help")]
        public GameObject helpPanel;
        public Button closeHelpButton;

        [Header("Notifications")]
        public GameObject toastRoot;
        public TextMeshProUGUI toastText;

        [Header("Minimap")]
        public RectTransform minimapRect;
        public RectTransform hubDot;
        public RectTransform playerDot;
        public RectTransform[] planetDots;
        public Image offscreenArrow;
    }
}
