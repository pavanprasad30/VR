using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Drives every HUD control on both the desktop (screen-space) and
    /// VR (world-space) canvases from a single source of truth:
    /// <see cref="GalaxyManager"/> and <see cref="SpaceTravelController"/>.
    /// All event subscriptions are paired in OnEnable/OnDisable so listeners
    /// are never duplicated.
    /// </summary>
    public class NavigationUI : MonoBehaviour
    {
        [SerializeField] private UIReferences desktopUI;
        [SerializeField] private UIReferences vrUI;
        [SerializeField] private GalaxyManager manager;
        [SerializeField] private SpaceTravelController travel;

        [Header("Styling")]
        [SerializeField] private Color normalRowColor = new Color(0.05f, 0.12f, 0.2f, 0.85f);
        [SerializeField] private Color selectedRowColor = new Color(0.12f, 0.45f, 0.65f, 0.95f);

        private Coroutine toastRoutine;
        private float distanceRefreshTimer;
        private Camera desktopCamera;

        /// <summary>Wired by the scene generator.</summary>
        public void Initialize(GalaxyManager galaxyManager, SpaceTravelController travelController, UIReferences desktop, UIReferences vr)
        {
            manager = galaxyManager;
            travel = travelController;
            desktopUI = desktop;
            vrUI = vr;
        }

        private void OnEnable()
        {
            if (manager != null)
            {
                manager.SelectionChanged += HandleSelectionChanged;
                manager.LocationChanged += HandleLocationChanged;
                manager.StatusMessage += HandleStatus;
                manager.ToastMessage += HandleToast;
            }

            if (travel != null)
            {
                travel.ProgressChanged += HandleProgress;
                travel.StateChanged += HandleTravelStateChanged;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.SelectionChanged -= HandleSelectionChanged;
                manager.LocationChanged -= HandleLocationChanged;
                manager.StatusMessage -= HandleStatus;
                manager.ToastMessage -= HandleToast;
            }

            if (travel != null)
            {
                travel.ProgressChanged -= HandleProgress;
                travel.StateChanged -= HandleTravelStateChanged;
            }
        }

        private void Start()
        {
            desktopCamera = Camera.main;
            RefreshAll();
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        private void HandleSelectionChanged(int index)
        {
            RefreshSelection(index);
            RefreshDestinationPanel(index);
            RefreshButtonStates();
            RefreshDistances();
            UpdateOffscreenArrow();
        }

        private void HandleLocationChanged()
        {
            RefreshCurrentLocation();
            RefreshDistances();
            RefreshButtonStates();
        }

        private void HandleStatus(string message)
        {
            ForEachUI(ui =>
            {
                if (ui.statusText != null)
                {
                    ui.statusText.text = message;
                }
            });
        }

        private void HandleToast(string message)
        {
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }
            toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            ForEachUI(ui =>
            {
                if (ui.toastRoot != null)
                {
                    ui.toastRoot.SetActive(true);
                }
                if (ui.toastText != null)
                {
                    ui.toastText.text = message;
                }
            });

            yield return new WaitForSeconds(3.5f);

            ForEachUI(ui =>
            {
                if (ui.toastRoot != null)
                {
                    ui.toastRoot.SetActive(false);
                }
            });
            toastRoutine = null;
        }

        private void HandleProgress(float progress01)
        {
            ForEachUI(ui =>
            {
                if (ui.progressFill != null)
                {
                    ui.progressFill.fillAmount = progress01;
                }
                if (ui.progressPercentText != null)
                {
                    ui.progressPercentText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
                }
            });
        }

        private void HandleTravelStateChanged(SpaceTravelController.TravelState state)
        {
            RefreshButtonStates();
            if (state == SpaceTravelController.TravelState.Idle)
            {
                HandleProgress(0f);
                RefreshDistances();
            }
        }

        // ------------------------------------------------------------------
        // Full refresh (startup / regeneration)
        // ------------------------------------------------------------------

        public void RefreshAll()
        {
            if (manager == null)
            {
                return;
            }

            int selected = manager.SelectedIndex;
            RefreshRows();
            RefreshSelection(selected);
            RefreshDestinationPanel(selected);
            RefreshCurrentLocation();
            RefreshDistances();
            RefreshButtonStates();
            HandleProgress(0f);

            ForEachUI(ui =>
            {
                if (ui.helpPanel != null)
                {
                    ui.helpPanel.SetActive(false);
                }
                if (ui.toastRoot != null)
                {
                    ui.toastRoot.SetActive(false);
                }
                if (ui.offscreenArrow != null)
                {
                    ui.offscreenArrow.gameObject.SetActive(false);
                }
            });
        }

        private void RefreshRows()
        {
            ForEachUI(ui =>
            {
                if (ui.rows == null)
                {
                    return;
                }

                int count = Mathf.Min(ui.rows.Length, manager.PlanetCount);
                for (int i = 0; i < ui.rows.Length; i++)
                {
                    bool active = i < count;
                    if (ui.rows[i] != null && ui.rows[i].root != null)
                    {
                        ui.rows[i].root.gameObject.SetActive(active);
                    }

                    if (!active)
                    {
                        continue;
                    }

                    PlanetData data = manager.Planets[i];
                    if (ui.rows[i].nameText != null)
                    {
                        ui.rows[i].nameText.text = data.planetName;
                    }
                    if (ui.rows[i].descriptionText != null)
                    {
                        ui.rows[i].descriptionText.text = data.description;
                    }
                    if (ui.rows[i].colorDot != null)
                    {
                        ui.rows[i].colorDot.color = data.surfaceColor;
                    }
                }
            });
        }

        private void RefreshSelection(int selected)
        {
            ForEachUI(ui =>
            {
                if (ui.rows == null)
                {
                    return;
                }

                for (int i = 0; i < ui.rows.Length; i++)
                {
                    if (ui.rows[i] == null || ui.rows[i].rowBackground == null)
                    {
                        continue;
                    }

                    ui.rows[i].rowBackground.color = i == selected ? selectedRowColor : normalRowColor;
                }
            });
        }

        private void RefreshDestinationPanel(int selected)
        {
            ForEachUI(ui =>
            {
                if (selected >= 0 && selected < manager.PlanetCount)
                {
                    PlanetData data = manager.Planets[selected];
                    if (ui.selectedNameText != null)
                    {
                        ui.selectedNameText.text = data.planetName;
                    }
                    if (ui.selectedDescriptionText != null)
                    {
                        ui.selectedDescriptionText.text = data.description;
                    }
                }
                else
                {
                    if (ui.selectedNameText != null)
                    {
                        ui.selectedNameText.text = "No destination selected";
                    }
                    if (ui.selectedDescriptionText != null)
                    {
                        ui.selectedDescriptionText.text = "Click a planet in space or choose one from the destination list.";
                    }
                }
            });
        }

        private void RefreshCurrentLocation()
        {
            ForEachUI(ui =>
            {
                if (ui.currentLocationText != null)
                {
                    ui.currentLocationText.text = manager.GetCurrentLocationName();
                }
            });
        }

        private void RefreshDistances()
        {
            ForEachUI(ui =>
            {
                if (ui.rows == null)
                {
                    return;
                }

                for (int i = 0; i < ui.rows.Length && i < manager.PlanetCount; i++)
                {
                    if (ui.rows[i] == null || ui.rows[i].distanceText == null)
                    {
                        continue;
                    }

                    float distance = manager.GetDistanceToPlanet(i);
                    ui.rows[i].distanceText.text = distance >= 0f ? $"{distance:F0} LU" : "--";
                }
            });
        }

        private void RefreshButtonStates()
        {
            bool travelling = travel != null && travel.IsTravelling;
            bool hasSelection = manager != null && manager.SelectedIndex != GalaxyConfig.NoSelection;

            ForEachUI(ui =>
            {
                if (ui.beginTravelButton != null)
                {
                    ui.beginTravelButton.interactable = hasSelection && !travelling;
                }
                if (ui.cancelTravelButton != null)
                {
                    ui.cancelTravelButton.interactable = travelling;
                }
                if (ui.returnToHubButton != null)
                {
                    ui.returnToHubButton.interactable = !travelling;
                }
            });
        }

        // ------------------------------------------------------------------
        // Help panel
        // ------------------------------------------------------------------

        public void ToggleHelp()
        {
            ForEachUI(ui =>
            {
                if (ui.helpPanel != null)
                {
                    ui.helpPanel.SetActive(!ui.helpPanel.activeSelf);
                }
            });
        }

        public void CloseHelp()
        {
            ForEachUI(ui =>
            {
                if (ui.helpPanel != null)
                {
                    ui.helpPanel.SetActive(false);
                }
            });
        }

        // ------------------------------------------------------------------
        // Per-frame HUD: minimap, distances while moving, off-screen marker
        // ------------------------------------------------------------------

        private void Update()
        {
            if (manager == null)
            {
                return;
            }

            // Throttle distance text updates while the rig is moving.
            distanceRefreshTimer -= Time.deltaTime;
            if (distanceRefreshTimer <= 0f)
            {
                distanceRefreshTimer = 0.25f;
                RefreshDistances();
            }

            UpdateMinimap();
            UpdateOffscreenArrow();
        }

        private void UpdateMinimap()
        {
            ForEachUI(ui =>
            {
                if (ui.minimapRect == null || manager.PlayerRig == null)
                {
                    return;
                }

                float mapSize = Mathf.Min(ui.minimapRect.rect.width, ui.minimapRect.rect.height);
                float scale = (mapSize * 0.46f) / GalaxyConfig.GalaxyBoundsRadius;

                if (ui.playerDot != null)
                {
                    Vector3 p = manager.PlayerRig.position;
                    ui.playerDot.anchoredPosition = new Vector2(p.x * scale, p.z * scale);
                }

                if (ui.planetDots != null)
                {
                    for (int i = 0; i < ui.planetDots.Length; i++)
                    {
                        Transform anchor = manager.GetPlanetAnchor(i);
                        if (ui.planetDots[i] == null || anchor == null)
                        {
                            continue;
                        }

                        Vector3 p = anchor.position;
                        ui.planetDots[i].anchoredPosition = new Vector2(p.x * scale, p.z * scale);
                    }
                }

                if (ui.hubDot != null && manager.HubAnchor != null)
                {
                    Vector3 p = manager.HubAnchor.position;
                    ui.hubDot.anchoredPosition = new Vector2(p.x * scale, p.z * scale);
                }
            });
        }

        private void UpdateOffscreenArrow()
        {
            if (desktopCamera == null)
            {
                desktopCamera = Camera.main;
            }

            ForEachUI(ui =>
            {
                // The directional edge arrow is only useful on the desktop canvas.
                if (ui != desktopUI || ui.offscreenArrow == null || desktopCamera == null || manager == null)
                {
                    return;
                }

                int selected = manager.SelectedIndex;
                Transform target = selected >= 0 ? manager.GetPlanetAnchor(selected) : null;
                if (target == null)
                {
                    ui.offscreenArrow.gameObject.SetActive(false);
                    return;
                }

                Vector3 viewport = desktopCamera.WorldToViewportPoint(target.position);
                bool behind = viewport.z <= 0f;
                bool outside = viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;

                if (!behind && !outside)
                {
                    ui.offscreenArrow.gameObject.SetActive(false);
                    return;
                }

                ui.offscreenArrow.gameObject.SetActive(true);
                Vector2 direction = behind
                    ? new Vector2(-viewport.x + 0.5f, -viewport.y + 0.5f).normalized
                    : new Vector2(viewport.x - 0.5f, viewport.y - 0.5f).normalized;

                RectTransform arrowRect = ui.offscreenArrow.rectTransform;
                Vector2 anchor = new Vector2(
                    Mathf.Clamp(viewport.x, 0.08f, 0.92f),
                    Mathf.Clamp(viewport.y, 0.1f, 0.9f));

                arrowRect.anchorMin = anchor;
                arrowRect.anchorMax = anchor;
                arrowRect.anchoredPosition = Vector2.zero;
                arrowRect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            });
        }

        /// <summary>Run an action against every existing canvas (desktop + VR).</summary>
        private void ForEachUI(System.Action<UIReferences> action)
        {
            if (desktopUI != null)
            {
                action(desktopUI);
            }
            if (vrUI != null)
            {
                action(vrUI);
            }
        }
    }
}
