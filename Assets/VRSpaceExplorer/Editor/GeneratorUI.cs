using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using VRSpaceExplorer;

namespace VRSpaceExplorer.EditorTools
{
    /// <summary>
    /// Constructs the desktop screen-space canvas and the VR world-space
    /// canvas from the same panel builder, then wires every button as a
    /// persistent (serialized) event so the scene works without runtime
    /// AddListener calls.
    /// </summary>
    public static partial class GalaxySceneGenerator
    {
        private static readonly Color PanelColor = new Color(0.03f, 0.07f, 0.13f, 0.88f);
        private static readonly Color SubPanelColor = new Color(0.05f, 0.12f, 0.2f, 0.92f);
        private static readonly Color AccentCyan = new Color(0.15f, 0.85f, 1f);
        private static readonly Color AccentPurple = new Color(0.55f, 0.35f, 1f);
        private static readonly Color TextWhite = new Color(0.92f, 0.97f, 1f);

        public static void BuildUserInterface(BuildContext context)
        {
            var uiRoot = new GameObject("UI");
            uiRoot.transform.SetParent(context.root.transform, false);

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(uiRoot.transform, false);
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Canvas desktopCanvas = CreateCanvas("Desktop Canvas (Screen Space)", uiRoot.transform, true, context.desktopCamera);
            UIReferences desktop = BuildPanel(desktopCanvas, context, 1f, 28);

            Canvas vrCanvas = CreateCanvas("VR Canvas (World Space)", uiRoot.transform, false, context.desktopCamera);
            PositionVRCanvas(vrCanvas, context);
            UIReferences vr = BuildPanel(vrCanvas, context, 0.62f, 16);
            context.vrCanvas = vrCanvas;

            context.navigationUI = uiRoot.AddComponent<NavigationUI>();
            context.navigationUI.Initialize(context.manager, context.travel, desktop, vr);

            WireButtons(context, desktop);
            WireButtons(context, vr);
        }

        private static Canvas CreateCanvas(string name, Transform parent, bool screenSpace, Camera eventCamera)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = screenSpace ? RenderMode.ScreenSpaceOverlay : RenderMode.WorldSpace;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = go.AddComponent<GraphicRaycaster>();
            if (!screenSpace && eventCamera != null)
            {
                raycaster.eventCamera = eventCamera;
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1000, 560);
                go.transform.localScale = Vector3.one * 0.0015f;
            }

            return canvas;
        }

        private static void PositionVRCanvas(Canvas canvas, BuildContext context)
        {
            if (context.desktopRig == null)
            {
                return;
            }

            Transform rig = context.desktopRig.transform;
            canvas.transform.position = rig.position + rig.forward * 2.3f + Vector3.up * 1.15f;
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - rig.position, Vector3.up);
        }

        private static UIReferences BuildPanel(Canvas canvas, BuildContext context, float scale, float margin)
        {
            var refs = new UIReferences { canvas = canvas };
            Transform parent = canvas.transform;
            TMP_FontAsset font = context.font;
            int planetCount = context.planetData.Count;

            // Root panel.
            RectTransform panel = CreateRect("Hologram Panel", parent);
            Stretch(panel, margin);
            AddImage(panel.gameObject, PanelColor);
            AddOutline(panel.gameObject, AccentCyan, 2f);
            refs.panelRoot = panel;

            // Header.
            RectTransform header = CreateRect("Header", panel);
            SetAnchors(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -95f * scale), new Vector2(0f, 0f));
            refs.titleText = CreateText("Title", header, font, "VR SPACE EXPLORER", 44f * scale, AccentCyan, TextAlignmentOptions.Left);
            StretchRect(refs.titleText.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -2f));
            TextMeshProUGUI subtitle = CreateText("Subtitle", header, font, "Galactic Navigation System  -  select a world and chart your course", 18f * scale, TextWhite, TextAlignmentOptions.Left);
            SetAnchors(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 2f), new Vector2(-10f, 34f * scale));

            // Body split.
            RectTransform leftPanel = CreateRect("Destination List Panel", panel);
            SetAnchors(leftPanel, new Vector2(0f, 0f), new Vector2(0.46f, 0.9f), new Vector2(0f, 0f), Vector2.zero);
            AddImage(leftPanel.gameObject, SubPanelColor);
            AddOutline(leftPanel.gameObject, AccentPurple, 1.5f);

            RectTransform rightPanel = CreateRect("Navigation Panel", panel);
            SetAnchors(rightPanel, new Vector2(0.48f, 0f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
            AddImage(rightPanel.gameObject, SubPanelColor);
            AddOutline(rightPanel.gameObject, AccentCyan, 1.5f);

            BuildDestinationList(leftPanel, context, font, scale, refs, planetCount);
            BuildStatusPanel(rightPanel, context, font, scale, refs, planetCount);
            BuildHelpAndNotifications(panel, font, scale, refs);
            return refs;
        }

        private static void BuildDestinationList(RectTransform host, BuildContext context, TMP_FontAsset font, float scale, UIReferences refs, int planetCount)
        {
            TextMeshProUGUI heading = CreateText("List Heading", host, font, "DESTINATIONS", 24f * scale, AccentCyan, TextAlignmentOptions.Left);
            SetAnchors(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -44f * scale), new Vector2(-14f, -6f));

            RectTransform scrollRect = CreateRect("Scroll View", host);
            SetAnchors(scrollRect, new Vector2(0f, 0f), new Vector2(1f, 0.92f), new Vector2(10f, 8f), new Vector2(-10f, -8f));
            AddImage(scrollRect.gameObject, new Color(0.02f, 0.05f, 0.1f, 0.8f));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;

            RectTransform viewport = CreateRect("Viewport", scrollRect);
            Stretch(viewport, 2f);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport.gameObject, new Color(0f, 0f, 0f, 0.05f));
            scroll.viewport = viewport;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 10f * scale;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            refs.rows = new PlanetRow[planetCount];
            for (int i = 0; i < planetCount; i++)
            {
                PlanetData data = context.planetData[i];
                RectTransform row = CreateRect($"Row_{i:D2}_{data.planetName}", content);
                AddImage(row.gameObject, new Color(0.05f, 0.12f, 0.2f, 0.85f));
                var layoutElement = row.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 96f * scale;
                layoutElement.minHeight = 80f * scale;

                Image dot = CreateImageObject("Color Dot", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f * scale, 26f * scale), new Vector2(14f, 0f));
                dot.color = data.surfaceColor;

                TextMeshProUGUI name = CreateText("Planet Name", row, font, data.planetName, 21f * scale, TextWhite, TextAlignmentOptions.Left);
                SetAnchors(name.rectTransform, new Vector2(0.07f, 0.55f), new Vector2(0.72f, 1f), new Vector2(0f, 0f), new Vector2(0f, -2f));

                TextMeshProUGUI desc = CreateText("Description", row, font, data.description, 14f * scale, new Color(0.72f, 0.82f, 0.92f), TextAlignmentOptions.Left);
                SetAnchors(desc.rectTransform, new Vector2(0.07f, 0f), new Vector2(0.72f, 0.5f), new Vector2(0f, 4f), new Vector2(0f, 0f));

                TextMeshProUGUI distance = CreateText("Distance", row, font, "-- LU", 15f * scale, AccentCyan, TextAlignmentOptions.Right);
                SetAnchors(distance.rectTransform, new Vector2(0.72f, 0.55f), new Vector2(0.98f, 1f), new Vector2(0f, 0f), new Vector2(-6f, -2f));

                Button selectButton = CreateButton("Select Destination", row, font, 15f * scale, AccentCyan);
                SetAnchors(selectButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.02f), new Vector2(0.98f, 0.48f), Vector2.zero, new Vector2(-6f, 0f));

                refs.rows[i] = new PlanetRow
                {
                    root = row,
                    rowBackground = row.GetComponent<Image>(),
                    colorDot = dot,
                    nameText = name,
                    descriptionText = desc,
                    distanceText = distance,
                    selectButton = selectButton
                };
            }
        }

        private static void BuildStatusPanel(RectTransform host, BuildContext context, TMP_FontAsset font, float scale, UIReferences refs, int planetCount)
        {
            float y = 0f;
            float sectionGap = 12f * scale;

            // Selected destination.
            RectTransform selectionBlock = CreateRect("Selected Destination Block", host);
            SetAnchors(selectionBlock, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -(190f * scale)), new Vector2(-12f, -10f));
            AddImage(selectionBlock.gameObject, new Color(0.04f, 0.1f, 0.18f, 0.9f));
            refs.selectedNameText = CreateText("Selected Name", selectionBlock, font, "No destination selected", 24f * scale, AccentPurple, TextAlignmentOptions.Left);
            SetAnchors(refs.selectedNameText.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -6f));
            refs.selectedDescriptionText = CreateText("Selected Description", selectionBlock, font, "Click a planet in space or choose one from the destination list.", 15f * scale, TextWhite, TextAlignmentOptions.Left);
            SetAnchors(refs.selectedDescriptionText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.6f), new Vector2(12f, 8f), new Vector2(-12f, 0f));
            y += 190f * scale + sectionGap;

            // Current location.
            refs.currentLocationText = CreateInfoLine(host, font, scale, "CURRENT LOCATION", GalaxyConfig.HubName, ref y);

            // Status.
            refs.statusText = CreateInfoLine(host, font, scale, "NAVIGATION STATUS", "Systems online. Awaiting destination.", ref y);

            // Progress bar.
            RectTransform progressBlock = CreateRect("Progress Block", host);
            SetAnchors(progressBlock, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -(y + 84f * scale)), new Vector2(-12f, -y));
            AddImage(progressBlock.gameObject, new Color(0.04f, 0.1f, 0.18f, 0.9f));
            TextMeshProUGUI progressLabel = CreateText("Progress Label", progressBlock, font, "TRAVEL PROGRESS", 14f * scale, AccentCyan, TextAlignmentOptions.Left);
            SetAnchors(progressLabel.rectTransform, new Vector2(0f, 0.55f), new Vector2(0.7f, 1f), new Vector2(12f, 0f), new Vector2(0f, -4f));
            refs.progressPercentText = CreateText("Percent", progressBlock, font, "0%", 16f * scale, TextWhite, TextAlignmentOptions.Right);
            SetAnchors(refs.progressPercentText.rectTransform, new Vector2(0.7f, 0.55f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-12f, -4f));
            Image barBackground = CreateImageObject("Bar Background", progressBlock, new Vector2(0f, 0f), new Vector2(1f, 0.4f), Vector2.zero, new Vector2(12f, 10f));
            barBackground.color = new Color(0.08f, 0.16f, 0.26f, 1f);
            RectTransform fillRect = CreateRect("Progress Fill", barBackground.transform);
            Stretch(fillRect, 1f);
            refs.progressFill = fillRect.gameObject.AddComponent<Image>();
            refs.progressFill.color = AccentCyan;
            refs.progressFill.type = Image.Type.Filled;
            refs.progressFill.fillMethod = Image.FillMethod.HorizontalFill;
            refs.progressFill.fillAmount = 0f;
            y += 84f * scale + sectionGap;

            // Buttons.
            refs.beginTravelButton = CreateButton("Begin Travel", host, font, 15f * scale, AccentCyan);
            refs.cancelTravelButton = CreateButton("Cancel Travel", host, font, 15f * scale, new Color(1f, 0.45f, 0.35f));
            refs.returnToHubButton = CreateButton("Return to Hub", host, font, 15f * scale, AccentPurple);
            refs.helpButton = CreateButton("Help", host, font, 15f * scale, new Color(0.7f, 0.8f, 0.95f));
            Button[] buttons = { refs.beginTravelButton, refs.cancelTravelButton, refs.returnToHubButton, refs.helpButton };
            float buttonHeight = 52f * scale;
            for (int i = 0; i < buttons.Length; i++)
            {
                float xMin = i / 4f;
                float xMax = (i + 1) / 4f;
                SetAnchors(buttons[i].GetComponent<RectTransform>(), new Vector2(xMin, 1f), new Vector2(xMax, 1f),
                    new Vector2(12f + i * 4f, -(y + buttonHeight)), new Vector2(-(12f + (3 - i) * 4f), -y));
            }
            y += buttonHeight + sectionGap;

            BuildMinimap(host, context, font, scale, refs, planetCount, ref y);
        }

        private static TextMeshProUGUI CreateInfoLine(RectTransform host, TMP_FontAsset font, float scale, string label, string value, ref float y)
        {
            RectTransform block = CreateRect(label + " Block", host);
            SetAnchors(block, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -(y + 64f * scale)), new Vector2(-12f, -y));
            AddImage(block.gameObject, new Color(0.04f, 0.1f, 0.18f, 0.9f));
            TextMeshProUGUI labelText = CreateText("Label", block, font, label, 13f * scale, AccentCyan, TextAlignmentOptions.Left);
            SetAnchors(labelText.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -2f));
            TextMeshProUGUI valueText = CreateText("Value", block, font, value, 18f * scale, TextWhite, TextAlignmentOptions.Left);
            SetAnchors(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(12f, 4f), new Vector2(-12f, 0f));
            y += 64f * scale + 10f * scale;
            return valueText;
        }

        private static void BuildMinimap(RectTransform host, BuildContext context, TMP_FontAsset font, float scale, UIReferences refs, int planetCount, ref float y)
        {
            float mapSize = 260f * scale;
            RectTransform mapBlock = CreateRect("Minimap Block", host);
            SetAnchors(mapBlock, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-mapSize * 0.5f - 12f, -(y + mapSize + 40f * scale)),
                new Vector2(mapSize * 0.5f + 12f, -y));
            AddImage(mapBlock.gameObject, new Color(0.02f, 0.06f, 0.12f, 0.95f));
            AddOutline(mapBlock.gameObject, AccentCyan, 1.5f);

            TextMeshProUGUI mapLabel = CreateText("Minimap Label", mapBlock, font, "MINIMAP", 14f * scale, AccentCyan, TextAlignmentOptions.Center);
            SetAnchors(mapLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f * scale), new Vector2(0f, -2f));

            RectTransform map = CreateRect("Minimap Surface", mapBlock);
            SetAnchors(map, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-mapSize * 0.5f, 8f), new Vector2(mapSize * 0.5f, 8f + mapSize));
            AddImage(map.gameObject, new Color(0.03f, 0.09f, 0.16f, 1f));
            refs.minimapRect = map;

            refs.hubDot = CreateDot(map, new Vector2(12f, 12f), Color.white);
            refs.playerDot = CreateDot(map, new Vector2(16f, 16f), AccentCyan);
            refs.playerDot.localEulerAngles = new Vector3(0f, 0f, 45f);

            refs.planetDots = new RectTransform[planetCount];
            for (int i = 0; i < planetCount; i++)
            {
                refs.planetDots[i] = CreateDot(map, new Vector2(11f, 11f), context.planetData[i].surfaceColor);
            }

            // Off-screen target arrow (desktop canvas only, updated by NavigationUI).
            TextMeshProUGUI arrow = CreateText("Offscreen Arrow", host, font, ">", 38f * scale, AccentCyan, TextAlignmentOptions.Center);
            RectTransform arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(48f, 48f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrow.gameObject.SetActive(false);
            refs.offscreenArrow = arrow;

            y += mapSize + 40f * scale;
        }

        private static void BuildHelpAndNotifications(RectTransform panel, TMP_FontAsset font, float scale, UIReferences refs)
        {
            // Toast.
            RectTransform toast = CreateRect("Arrival Toast", panel);
            SetAnchors(toast, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-360f, -150f * scale), new Vector2(360f, -100f * scale));
            AddImage(toast.gameObject, new Color(0.05f, 0.25f, 0.35f, 0.95f));
            AddOutline(toast.gameObject, AccentCyan, 2f);
            TextMeshProUGUI toastText = CreateText("Toast Text", toast, font, string.Empty, 22f * scale, TextWhite, TextAlignmentOptions.Center);
            Stretch(toastText.rectTransform, 6f);
            refs.toastRoot = toast.gameObject;
            refs.toastText = toastText;
            toast.gameObject.SetActive(false);

            // Help overlay.
            RectTransform help = CreateRect("Help Panel", panel);
            Stretch(help, 40f * scale);
            AddImage(help.gameObject, new Color(0.02f, 0.05f, 0.1f, 0.97f));
            AddOutline(help.gameObject, AccentPurple, 2f);

            TextMeshProUGUI helpTitle = CreateText("Help Title", help, font, "FLIGHT MANUAL", 36f * scale, AccentCyan, TextAlignmentOptions.Center);
            SetAnchors(helpTitle.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.98f), new Vector2(20f, 0f), new Vector2(-20f, 0f));

            string instructions =
                "DESKTOP CONTROLS\n" +
                "Left Mouse - click a planet or a UI button\n" +
                "Hold Right Mouse - look around\n" +
                "W A S D - thrust forward / strafe\n" +
                "R / F - thrust up / down\n" +
                "Left Shift - boost thrust\n" +
                "Escape - cancel travel or toggle this help\n\n" +
                "NAVIGATION\n" +
                "1. Pick a planet from the list or click it directly in space.\n" +
                "2. Press BEGIN TRAVEL to fly to the selected destination.\n" +
                "3. CANCEL TRAVEL stops the ship safely at its current position.\n" +
                "4. RETURN TO HUB flies back to the Central Space Station.\n\n" +
                "VR MODE\n" +
                "Connect an OpenXR headset and enable it in XR Plug-in Management.\n" +
                "Use either controller ray to point at the world-space panel and " +
                "pull the trigger to select. Left stick moves, right stick snap-turns.";

            TextMeshProUGUI body = CreateText("Help Body", help, font, instructions, 18f * scale, TextWhite, TextAlignmentOptions.Left);
            SetAnchors(body.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero);

            Button close = CreateButton("Close Help", help, font, 18f * scale, AccentCyan);
            SetAnchors(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-130f, 20f), new Vector2(130f, 76f));
            refs.closeHelpButton = close;
            refs.helpPanel = help.gameObject;
            help.gameObject.SetActive(false);
        }

        private static void WireButtons(BuildContext context, UIReferences ui)
        {
            if (ui.rows != null)
            {
                for (int i = 0; i < ui.rows.Length; i++)
                {
                    Button button = ui.rows[i].selectButton;
                    var helper = button.gameObject.AddComponent<PlanetSelectionButton>();
                    helper.Configure(context.manager, i);
                    UnityEventTools.AddPersistentListener(button.onClick, helper.SelectPressed);
                    button.navigation = new Navigation { mode = Navigation.Mode.None };
                }
            }

            if (ui.beginTravelButton != null)
            {
                UnityEventTools.AddPersistentListener(ui.beginTravelButton.onClick, context.manager.BeginTravel);
            }
            if (ui.cancelTravelButton != null)
            {
                UnityEventTools.AddPersistentListener(ui.cancelTravelButton.onClick, context.travel.CancelTravel);
            }
            if (ui.returnToHubButton != null)
            {
                UnityEventTools.AddPersistentListener(ui.returnToHubButton.onClick, context.manager.ReturnToHub);
            }
            if (ui.helpButton != null)
            {
                UnityEventTools.AddPersistentListener(ui.helpButton.onClick, context.navigationUI.ToggleHelp);
            }
            if (ui.closeHelpButton != null)
            {
                UnityEventTools.AddPersistentListener(ui.closeHelpButton.onClick, context.navigationUI.CloseHelp);
            }
        }

        // ----- Small UI construction utilities -----

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void StretchRect(RectTransform rect, Vector2 insetMin, Vector2 insetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = insetMin;
            rect.offsetMax = insetMax;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddOutline(GameObject go, Color color, float width)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, -width);
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateButton(string label, Transform parent, TMP_FontAsset font, float fontSize, Color accent)
        {
            var go = new GameObject($"Button_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.95f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 1f);
            colors.pressedColor = accent;
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TextMeshProUGUI text = CreateText("Label", go.transform, font, label, fontSize, TextWhite, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, new Vector2(6f, 4f), new Vector2(-6f, -4f));
            return button;
        }

        private static Image CreateImageObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return AddImage(rect.gameObject, Color.white);
        }

        private static RectTransform CreateDot(RectTransform parent, Vector2 size, Color color)
        {
            RectTransform dot = CreateRect("Dot", parent);
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = size;
            dot.anchoredPosition = Vector2.zero;
            AddImage(dot.gameObject, color);
            return dot;
        }
    }
}
