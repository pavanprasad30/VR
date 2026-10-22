using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// One-shot GPU particle star field. Emits thousands of stars inside a
    /// spherical shell on Awake, then stops simulating, so the per-frame cost
    /// is effectively zero while keeping the draw call count at one.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class StarField : MonoBehaviour
    {
        [SerializeField] private int starCount = 3500;
        [SerializeField] private float innerRadius = 320f;
        [SerializeField] private float outerRadius = 950f;
        [SerializeField] private float minSize = 0.6f;
        [SerializeField] private float maxSize = 2.4f;

        private void Awake()
        {
            ParticleSystem particleSystem = GetComponent<ParticleSystem>();

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = 99999f;
            main.maxParticles = starCount + 1;
            main.duration = 99999f;

            var emission = particleSystem.emission;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;

            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                if (renderer.sharedMaterial == null)
                {
                    renderer.material = new Material(Shader.Find("Sprites/Default"));
                }
            }

            Vector3[] points = SpaceMeshFactory.GenerateSpherePoints(starCount, innerRadius, outerRadius);

            // Deterministic pseudo-random tint/size per star.
            var emitParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < points.Length; i++)
            {
                float hash = Mathf.Repeat(Mathf.Sin(i * 12.9898f) * 43758.5453f, 1f);
                Color tint = Color.Lerp(
                    new Color(0.75f, 0.85f, 1f),
                    Color.white,
                    hash);

                emitParams.position = points[i];
                emitParams.velocity = Vector3.zero;
                emitParams.startColor = tint;
                emitParams.startSize = Mathf.Lerp(minSize, maxSize, hash);
                emitParams.startLifetime = 99999f;
                particleSystem.Emit(emitParams, 1);
            }
        }
    }
}
