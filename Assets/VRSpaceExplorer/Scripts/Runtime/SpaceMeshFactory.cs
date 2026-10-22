using UnityEngine;

namespace VRSpaceExplorer
{
    /// <summary>
    /// Procedural mesh helpers built entirely from code so the project needs
    /// no external model or texture assets. All methods are allocation-light
    /// and safe to call from the Editor generator or at runtime.
    /// </summary>
    public static class SpaceMeshFactory
    {
        /// <summary>
        /// Builds a flat double-sided annulus (ring) mesh lying in the XZ plane.
        /// Vertex alpha fades out toward both edges for a soft Saturn look.
        /// </summary>
        public static Mesh CreateRingMesh(float innerRadius, float outerRadius, int segments = 128)
        {
            Mesh mesh = new Mesh { name = "ProceduralRingMesh" };

            int vertexCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Color[] colors = new Color[vertexCount];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                int inner = i * 2;
                int outer = inner + 1;

                vertices[inner] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
                vertices[outer] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);

                uvs[inner] = new Vector2(0f, i / (float)segments);
                uvs[outer] = new Vector2(1f, i / (float)segments);

                // Soft alpha on both edges.
                colors[inner] = new Color(1f, 1f, 1f, 0.15f);
                colors[outer] = new Color(1f, 1f, 1f, 0.85f);
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int inner = i * 2;
                int outer = inner + 1;
                int nextInner = inner + 2;
                int nextOuter = inner + 3;

                // Top side winding.
                triangles[t++] = inner;
                triangles[t++] = nextOuter;
                triangles[t++] = outer;

                triangles[t++] = inner;
                triangles[t++] = nextInner;
                triangles[t++] = nextOuter;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Writes a closed circular orbit path into an existing LineRenderer.
        /// </summary>
        public static void ConfigureOrbitLine(LineRenderer line, float radius, int segments = 128)
        {
            if (line == null)
            {
                return;
            }

            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments + 1;
            line.startWidth = 0.18f;
            line.endWidth = 0.18f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        /// <summary>
        /// Generates evenly distributed points inside a spherical shell.
        /// Used by the generator to place star particles and distant scenery.
        /// </summary>
        public static Vector3[] GenerateSpherePoints(int count, float minRadius, float maxRadius)
        {
            Vector3[] points = new Vector3[count];
            // Deterministic seed so regenerating the scene looks identical.
            Random.State previousState = Random.state;
            Random.InitState(20261022);

            for (int i = 0; i < count; i++)
            {
                // Uniform direction on the unit sphere.
                float u = Random.Range(-1f, 1f);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                float sphereRadius = Mathf.Sqrt(1f - u * u);
                Vector3 direction = new Vector3(
                    sphereRadius * Mathf.Cos(theta),
                    u,
                    sphereRadius * Mathf.Sin(theta));

                float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
                points[i] = direction * radius;
            }

            Random.state = previousState;
            return points;
        }
    }
}
