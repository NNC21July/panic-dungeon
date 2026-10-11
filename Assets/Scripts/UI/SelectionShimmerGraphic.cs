using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class SelectionShimmerGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        for (int i = 0; i < 3; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i * 0.5f);
            Color tint = color;
            tint.a *= i == 1 ? 1f : 0f;
            mesh.AddVert(new Vector3(x, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x, rect.yMax), tint, Vector2.one);
        }
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(2, 1, 3);
        mesh.AddTriangle(2, 3, 4);
        mesh.AddTriangle(4, 3, 5);
    }
}
