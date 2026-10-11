using UnityEngine;
using UnityEngine.UI;

// UI geometry, so circles stay round and crisp without a texture asset.
[RequireComponent(typeof(CanvasRenderer))]
public class SelectionCircleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        mesh.AddVert(center, color, new Vector2(0.5f, 0.5f));
        const int segments = 80;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(center + dir * radius, color, dir * 0.5f + Vector2.one * 0.5f);
            if (i > 0)
                mesh.AddTriangle(0, i, i + 1);
        }
    }
}
