using UnityEngine;

// Adicionado automaticamente pelo ARFurniturePlacer em cada móvel instanciado.
public class ColorableObject : MonoBehaviour
{
    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private LineRenderer selectionRing;

    // Escala com que o objeto foi criado; usada como referência para os limites de escala.
    public Vector3 InitialScale { get; private set; }

    // "_BaseColor" é o nome usado pelo URP/Lit. Se o projeto usar o Built in Render Pipeline
    // com o shader Standard, troque para "_Color".
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    private const int RingSegments = 48;
    private static readonly Color RingColor = new Color(0.2f, 0.8f, 1f, 1f);

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        InitialScale = transform.localScale;
    }

    public void ApplyColor(Color color)
    {
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorProperty, color);
            rend.SetPropertyBlock(propertyBlock);
        }
    }

    // Volta às cores originais dos materiais do modelo.
    public void ResetColor()
    {
        foreach (var rend in renderers)
            rend.SetPropertyBlock(null);
    }

    // Mostra/esconde um anel na base do objeto para indicar que ele está selecionado.
    public void SetHighlighted(bool highlighted)
    {
        if (highlighted && selectionRing == null)
            selectionRing = CreateSelectionRing();

        if (selectionRing != null)
            selectionRing.enabled = highlighted;
    }

    private LineRenderer CreateSelectionRing()
    {
        // Calcula os limites do modelo no espaço local, para o anel acompanhar mover/girar/escalar.
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        foreach (var rend in renderers)
        {
            Bounds b = rend.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = transform.InverseTransformPoint(rend.transform.TransformPoint(corner));
                if (!hasBounds) { localBounds = new Bounds(p, Vector3.zero); hasBounds = true; }
                localBounds.Encapsulate(p);
            }
        }

        float radius = hasBounds
            ? Mathf.Max(localBounds.extents.x, localBounds.extents.z) * 1.15f
            : 0.5f;
        Vector3 center = hasBounds
            ? new Vector3(localBounds.center.x, localBounds.min.y + 0.005f, localBounds.center.z)
            : Vector3.zero;

        var ringObj = new GameObject("SelectionRing");
        ringObj.transform.SetParent(transform, false);

        var line = ringObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = RingSegments;
        line.widthMultiplier = 0.01f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = RingColor;
        line.endColor = RingColor;

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        return line;
    }
}
