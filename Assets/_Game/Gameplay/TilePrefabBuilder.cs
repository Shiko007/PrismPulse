using UnityEngine;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Runtime utility to create tile prefab if none is assigned.
    /// Creates a simple quad mesh with a collider for tap detection.
    /// This is a bootstrap helper — replace with proper prefabs for final art.
    /// </summary>
    public static class TilePrefabBuilder
    {
        public static GameObject CreateDefaultTilePrefab(Material material)
        {
            var go = new GameObject("TilePrefab");

            // Quad mesh
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateRoundedQuad();

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            // Collider for mouse/touch input
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 0.1f);

            // TileView component
            var tileView = go.AddComponent<BoardView.TileView>();

            // Use reflection or serialized field setup in editor.
            // For runtime bootstrap, TileView will find its own renderer.

            go.SetActive(false); // It's a prefab template
            return go;
        }

        private static Mesh CreateRoundedQuad()
        {
            // Simple quad mesh — replace with rounded-corner version later
            var mesh = new Mesh { name = "TileQuad" };

            float h = 0.45f; // Slightly smaller than 0.5 for visible gaps
            mesh.vertices = new[]
            {
                new Vector3(-h, -h, 0),
                new Vector3( h, -h, 0),
                new Vector3( h,  h, 0),
                new Vector3(-h,  h, 0)
            };
            mesh.uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
