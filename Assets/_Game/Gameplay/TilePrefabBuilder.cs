using UnityEngine;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Runtime utility to create tile prefab if none is assigned.
    /// Creates a quad mesh with collider and a child indicator line for orientation.
    /// This is a bootstrap helper — replace with proper prefabs for final art.
    /// </summary>
    public static class TilePrefabBuilder
    {
        public static GameObject CreateDefaultTilePrefab(Material tileMaterial, Material indicatorMaterial)
        {
            var go = new GameObject("TilePrefab");

            // Quad mesh for the tile background
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateQuad();

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = tileMaterial;

            // Collider for mouse/touch input
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 0.1f);

            // Direction indicator — a thin line showing the tile's routing axis
            var indicator = CreateIndicatorLine(indicatorMaterial);
            indicator.transform.SetParent(go.transform, false);
            indicator.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            // TileView component
            go.AddComponent<BoardView.TileView>();

            go.SetActive(false); // It's a prefab template
            return go;
        }

        /// <summary>
        /// Creates a thin rectangular bar used to show tile orientation.
        /// For Straight: a long bar along the axis.
        /// The TileView rotates the whole tile, so the indicator rotates with it.
        /// </summary>
        private static GameObject CreateIndicatorLine(Material material)
        {
            var go = new GameObject("Indicator");

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateBarMesh();

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            return go;
        }

        private static Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "TileQuad" };

            float h = 0.45f;
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
                -Vector3.forward, -Vector3.forward,
                -Vector3.forward, -Vector3.forward
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A thin vertical bar (tall and narrow). When tile rotation = 0, this shows as vertical (Up/Down axis).
        /// When rotated 90°, it shows as horizontal.
        /// </summary>
        private static Mesh CreateBarMesh()
        {
            var mesh = new Mesh { name = "IndicatorBar" };

            float hw = 0.04f;  // half-width (thin)
            float hh = 0.35f;  // half-height (tall)
            mesh.vertices = new[]
            {
                new Vector3(-hw, -hh, 0),
                new Vector3( hw, -hh, 0),
                new Vector3( hw,  hh, 0),
                new Vector3(-hw,  hh, 0)
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
                -Vector3.forward, -Vector3.forward,
                -Vector3.forward, -Vector3.forward
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
