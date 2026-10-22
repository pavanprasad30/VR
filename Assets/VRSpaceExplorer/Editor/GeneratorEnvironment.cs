using TMPro;
using UnityEditor;
using UnityEngine;
using VRSpaceExplorer;

namespace VRSpaceExplorer.EditorTools
{
    /// <summary>Builds the galaxy: star field, sun, planets, station, markers and labels.</summary>
    public static partial class GalaxySceneGenerator
    {
        private static readonly Vector3 SunPosition = Vector3.zero;

        public static void BuildEnvironment(BuildContext context)
        {
            var environment = new GameObject("Environment");
            environment.transform.SetParent(context.root.transform, false);

            BuildLighting(environment.transform);
            BuildStarField(environment.transform);
            BuildSun(context, environment.transform);
            BuildPlanets(context, environment.transform);
            BuildSpaceStation(context, environment.transform);
        }

        private static void BuildLighting(Transform parent)
        {
            var lightObject = new GameObject("Fill Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.35f;
            light.color = new Color(0.65f, 0.78f, 1f);
        }

        private static void BuildStarField(Transform parent)
        {
            var starObject = new GameObject("Star Field");
            starObject.transform.SetParent(parent, false);
            starObject.AddComponent<ParticleSystem>();
            starObject.AddComponent<StarField>();

            var renderer = starObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial("StarSprite", "Sprites/Default", material =>
            {
                material.color = Color.white;
            });
        }

        private static void BuildSun(BuildContext context, Transform parent)
        {
            var sun = new GameObject("Sun");
            sun.transform.SetParent(parent, false);
            sun.transform.position = SunPosition;
            context.sunTransform = sun.transform;

            Material sunMaterial = GetOrCreateMaterial("Sun", "Standard", material =>
            {
                material.color = new Color(1f, 0.85f, 0.55f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.35f) * 4f);
            });

            var sunMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunMesh.name = "Sun Mesh";
            DestroyImmediateCollider(sunMesh);
            sunMesh.transform.SetParent(sun.transform, false);
            sunMesh.transform.localScale = Vector3.one * 16f;
            sunMesh.GetComponent<MeshRenderer>().sharedMaterial = sunMaterial;
            sun.AddComponent<OrbitMotion>().Configure(null, 0f, 6f, sunMesh.transform);

            // Soft additive halo.
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "Sun Halo";
            DestroyImmediateCollider(halo);
            halo.transform.SetParent(sun.transform, false);
            halo.transform.localScale = Vector3.one * 24f;
            halo.GetComponent<MeshRenderer>().sharedMaterial =
                GetOrCreateMaterial("SunHalo", "Sprites/Default", material =>
                {
                    material.color = new Color(1f, 0.7f, 0.35f, 0.16f);
                });

            var lightObject = new GameObject("Sun Light");
            lightObject.transform.SetParent(sun.transform, false);
            var pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.range = 260f;
            pointLight.intensity = 2.2f;
            pointLight.color = new Color(1f, 0.92f, 0.78f);

            CreateLabel(sun.transform, "Sol", context.font, new Vector3(0f, 12f, 0f), new Color(1f, 0.9f, 0.6f));
        }

        private static void BuildPlanets(BuildContext context, Transform parent)
        {
            var planetGroup = new GameObject("Planets");
            planetGroup.transform.SetParent(parent, false);

            var orbitGroup = new GameObject("Orbit Paths");
            orbitGroup.transform.SetParent(parent, false);

            Material lineMaterial = GetOrCreateMaterial("OrbitLine", "Sprites/Default", material =>
            {
                material.color = new Color(0.25f, 0.8f, 1f, 0.35f);
            });

            Material markerMaterial = GetOrCreateMaterial("DestinationMarker", "Standard", material =>
            {
                material.color = new Color(0.1f, 0.9f, 1f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 1f) * 3.5f);
            });

            Material beamMaterial = GetOrCreateMaterial("MarkerBeam", "Sprites/Default", material =>
            {
                material.color = new Color(0.2f, 0.85f, 1f, 0.3f);
            });

            PlanetData[] definitions = CreatePlanetDefinitions();

            for (int i = 0; i < definitions.Length; i++)
            {
                PlanetData data = definitions[i];
                float initialAngle = (i / (float)definitions.Length) * Mathf.PI * 2f;
                var root = new GameObject($"Planet_{i:D2}_{data.planetName}");
                root.transform.SetParent(planetGroup.transform, false);
                root.transform.position = new Vector3(
                    Mathf.Cos(initialAngle) * data.orbitRadius,
                    0f,
                    Mathf.Sin(initialAngle) * data.orbitRadius);

                // Visible planet mesh.
                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh.name = "Planet Mesh";
                mesh.transform.SetParent(root.transform, false);
                mesh.transform.localScale = Vector3.one * data.radius * 2f;
                Material planetMaterial = GetOrCreateMaterial($"Planet_{data.planetName}", "Standard", material =>
                {
                    material.color = data.surfaceColor;
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", data.emissionColor * data.emissionStrength);
                });
                mesh.GetComponent<MeshRenderer>().sharedMaterial = planetMaterial;

                root.AddComponent<OrbitMotion>().Configure(context.sunTransform, data.orbitSpeed, data.spinSpeed, mesh.transform);

                // Saturn-style rings.
                if (data.hasRings)
                {
                    BuildRing(root.transform, data, i);
                }

                // Orbit path line centred on the sun.
                var lineObject = new GameObject($"Orbit_{data.planetName}");
                lineObject.transform.SetParent(orbitGroup.transform, false);
                lineObject.transform.position = SunPosition;
                var line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = lineMaterial;
                SpaceMeshFactory.ConfigureOrbitLine(line, data.orbitRadius);

                // Glowing destination marker (only active for the selected planet).
                GameObject marker;
                Light markerLight;
                BuildSelectionMarker(root.transform, data, markerMaterial, beamMaterial, out marker, out markerLight);

                // Floating name label.
                CreateLabel(
                    root.transform,
                    data.planetName,
                    context.font,
                    new Vector3(0f, data.radius + 2.4f, 0f),
                    Color.white);

                // Arrival point just outside the planet surface.
                var arrival = new GameObject("Arrival Point");
                arrival.transform.SetParent(root.transform, false);
                arrival.transform.localPosition = new Vector3(data.radius + 5f, 0f, 0f);

                var view = root.AddComponent<PlanetView>();
                view.Initialize(i, data, new[] { mesh.GetComponent<MeshRenderer>() }, marker, markerLight);

                context.planetData.Add(data);
                context.planetAnchors.Add(root.transform);
                context.arrivalPoints.Add(arrival.transform);
                context.planetViews.Add(view);
            }
        }

        private static void BuildRing(Transform parent, PlanetData data, int index)
        {
            var ringObject = new GameObject($"Ring_{data.planetName}");
            ringObject.transform.SetParent(parent, false);
            ringObject.transform.rotation = Quaternion.Euler(22f, 0f, 8f);

            Mesh ringMesh = SpaceMeshFactory.CreateRingMesh(data.ringInnerRadius, data.ringOuterRadius);
            ringMesh.name = $"RingMesh_{index}";
            var filter = ringObject.AddComponent<MeshFilter>();
            filter.sharedMesh = ringMesh;
            var renderer = ringObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial($"RingMaterial_{data.planetName}", "Sprites/Default", material =>
            {
                material.color = data.ringColor;
            });
        }

        private static void BuildSelectionMarker(
            Transform parent,
            PlanetData data,
            Material markerMaterial,
            Material beamMaterial,
            out GameObject marker,
            out Light markerLight)
        {
            marker = new GameObject("Selection Marker");
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, data.radius + 3.2f, 0f);

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Marker Core";
            DestroyImmediateCollider(core);
            core.transform.SetParent(marker.transform, false);
            core.transform.localScale = Vector3.one * 0.55f;
            core.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Marker Beam";
            DestroyImmediateCollider(beam);
            beam.transform.SetParent(marker.transform, false);
            beam.transform.localScale = new Vector3(0.12f, 3.2f, 0.12f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = beamMaterial;

            var lightObject = new GameObject("Marker Light");
            lightObject.transform.SetParent(marker.transform, false);
            markerLight = lightObject.AddComponent<Light>();
            markerLight.type = LightType.Point;
            markerLight.range = 18f;
            markerLight.intensity = 2f;
            markerLight.color = data.emissionColor;
            markerLight.enabled = false;

            marker.AddComponent<MarkerPulse>();
            marker.SetActive(false);
        }

        private static void BuildSpaceStation(BuildContext context, Transform parent)
        {
            var station = new GameObject(GalaxyConfig.HubName);
            station.transform.SetParent(parent, false);
            station.transform.position = new Vector3(0f, 0f, -48f);
            context.hubAnchor = station.transform;

            Material hullMaterial = GetOrCreateMaterial("StationHull", "Standard", material =>
            {
                material.color = new Color(0.55f, 0.62f, 0.72f);
                material.SetFloat("_Glossiness", 0.7f);
            });

            Material glowMaterial = GetOrCreateMaterial("StationGlow", "Standard", material =>
            {
                material.color = new Color(0.1f, 0.2f, 0.3f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 1f) * 2.2f);
            });

            Material panelMaterial = GetOrCreateMaterial("SolarPanel", "Standard", material =>
            {
                material.color = new Color(0.08f, 0.15f, 0.35f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.12f, 0.3f, 0.7f) * 0.8f);
            });

            // Central horizontal core.
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "Station Core";
            DestroyImmediateCollider(core);
            core.transform.SetParent(station.transform, false);
            core.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            core.transform.localScale = new Vector3(1.3f, 2.4f, 1.3f);
            core.GetComponent<MeshRenderer>().sharedMaterial = hullMaterial;

            // Vertical ring around the core.
            var ringObject = new GameObject("Station Ring");
            ringObject.transform.SetParent(station.transform, false);
            ringObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ringFilter = ringObject.AddComponent<MeshFilter>();
            ringFilter.sharedMesh = SpaceMeshFactory.CreateRingMesh(4.2f, 5.6f, 96);
            var ringRenderer = ringObject.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = hullMaterial;

            // Four habitat pods on the ring.
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                var pod = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pod.name = $"Habitat Pod {i + 1}";
                DestroyImmediateCollider(pod);
                pod.transform.SetParent(station.transform, false);
                pod.transform.localPosition = new Vector3(Mathf.Cos(angle) * 4.9f, Mathf.Sin(angle) * 4.9f, 0f);
                pod.transform.localScale = Vector3.one * 0.9f;
                pod.GetComponent<MeshRenderer>().sharedMaterial = glowMaterial;
            }

            // Solar panels on both sides.
            for (int side = -1; side <= 1; side += 2)
            {
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = side < 0 ? "Solar Panel L" : "Solar Panel R";
                DestroyImmediateCollider(panel);
                panel.transform.SetParent(station.transform, false);
                panel.transform.localPosition = new Vector3(side * 8.5f, 0f, 0f);
                panel.transform.localScale = new Vector3(4.2f, 0.12f, 1.8f);
                panel.GetComponent<MeshRenderer>().sharedMaterial = panelMaterial;

                // Emissive strip along the panel edge.
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "Panel Strip";
                DestroyImmediateCollider(strip);
                strip.transform.SetParent(station.transform, false);
                strip.transform.localPosition = new Vector3(side * 8.5f, 0.15f, 0.9f);
                strip.transform.localScale = new Vector3(4.2f, 0.08f, 0.08f);
                strip.GetComponent<MeshRenderer>().sharedMaterial = glowMaterial;
            }

            var stationLight = new GameObject("Station Light");
            stationLight.transform.SetParent(station.transform, false);
            var light = stationLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 34f;
            light.intensity = 1.6f;
            light.color = new Color(0.6f, 0.85f, 1f);

            CreateLabel(
                station.transform,
                GalaxyConfig.HubName,
                null,
                new Vector3(0f, 7.2f, 0f),
                new Color(0.4f, 0.95f, 1f));

            // Font is assigned uniformly after creation (labels created with null get the shared font below).
            AssignFontToLabels(station.transform, context.font);

            var arrival = new GameObject("Hub Arrival Point");
            arrival.transform.SetParent(station.transform, false);
            arrival.transform.localPosition = new Vector3(0f, 1.5f, 9f);
            context.hubArrival = arrival.transform;
        }

        private static void CreateLabel(Transform parent, string text, TMP_FontAsset font, Vector3 localPosition, Color color)
        {
            var labelObject = new GameObject($"Label_{text}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            var label = labelObject.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 3.2f;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (font != null)
            {
                label.font = font;
            }

            labelObject.AddComponent<LabelBillboard>();
        }

        private static void AssignFontToLabels(Transform root, TMP_FontAsset font)
        {
            if (font == null)
            {
                return;
            }

            foreach (var label in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                label.font = font;
            }
        }

        private static void DestroyImmediateCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static PlanetData[] CreatePlanetDefinitions()
        {
            return new[]
            {
                new PlanetData
                {
                    planetName = "Aetheron",
                    description = "A windswept ocean world wrapped in violet storms.",
                    radius = 1.8f, orbitRadius = 45f, orbitSpeed = 7.2f, spinSpeed = 22f,
                    surfaceColor = new Color(0.18f, 0.42f, 0.85f),
                    emissionColor = new Color(0.15f, 0.5f, 1f), emissionStrength = 0.4f
                },
                new PlanetData
                {
                    planetName = "Pyralis",
                    description = "Molten crust and rivers of glowing lava dominate the surface.",
                    radius = 2.5f, orbitRadius = 62f, orbitSpeed = 5.6f, spinSpeed = 14f,
                    surfaceColor = new Color(0.92f, 0.35f, 0.1f),
                    emissionColor = new Color(1f, 0.35f, 0.08f), emissionStrength = 0.55f
                },
                new PlanetData
                {
                    planetName = "Verdantis",
                    description = "Lush jungle continents beneath a perpetual oxygen-rich mist.",
                    radius = 2.9f, orbitRadius = 80f, orbitSpeed = 4.6f, spinSpeed = 17f,
                    surfaceColor = new Color(0.22f, 0.68f, 0.33f),
                    emissionColor = new Color(0.2f, 0.8f, 0.4f), emissionStrength = 0.3f
                },
                new PlanetData
                {
                    planetName = "Cryon",
                    description = "A frozen ice giant with blinding crystalline glaciers.",
                    radius = 2.3f, orbitRadius = 98f, orbitSpeed = 3.9f, spinSpeed = 11f,
                    surfaceColor = new Color(0.82f, 0.9f, 1f),
                    emissionColor = new Color(0.55f, 0.8f, 1f), emissionStrength = 0.35f
                },
                new PlanetData
                {
                    planetName = "Nimbara",
                    description = "Swirling cyan gas bands hide a diamond-rain interior.",
                    radius = 3.5f, orbitRadius = 118f, orbitSpeed = 3.2f, spinSpeed = 20f,
                    surfaceColor = new Color(0.18f, 0.72f, 0.82f),
                    emissionColor = new Color(0.2f, 0.9f, 1f), emissionStrength = 0.35f
                },
                new PlanetData
                {
                    planetName = "Vortex Prime",
                    description = "Ringed purple gas giant and the crown jewel of the system.",
                    radius = 4.6f, orbitRadius = 142f, orbitSpeed = 2.5f, spinSpeed = 16f,
                    surfaceColor = new Color(0.44f, 0.24f, 0.78f),
                    emissionColor = new Color(0.6f, 0.35f, 1f), emissionStrength = 0.4f,
                    hasRings = true, ringInnerRadius = 6.2f, ringOuterRadius = 9.4f,
                    ringColor = new Color(0.86f, 0.72f, 0.5f, 0.85f)
                },
                new PlanetData
                {
                    planetName = "Terranox",
                    description = "Earth-like colony candidate with blue oceans and green plains.",
                    radius = 2.1f, orbitRadius = 164f, orbitSpeed = 2f, spinSpeed = 19f,
                    surfaceColor = new Color(0.16f, 0.52f, 0.48f),
                    emissionColor = new Color(0.2f, 0.7f, 0.65f), emissionStrength = 0.3f
                },
                new PlanetData
                {
                    planetName = "Umbra",
                    description = "A dark outer world rumoured to host a hidden research base.",
                    radius = 3.8f, orbitRadius = 188f, orbitSpeed = 1.5f, spinSpeed = 9f,
                    surfaceColor = new Color(0.24f, 0.12f, 0.4f),
                    emissionColor = new Color(0.7f, 0.25f, 1f), emissionStrength = 0.5f
                }
            };
        }
    }
}
