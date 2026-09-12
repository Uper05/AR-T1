using UnityEngine;

// Adicionado automaticamente pelo ARFurniturePlacer em cada móvel instanciado.
public class ColorableObject : MonoBehaviour
{
    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    // "_BaseColor" é o nome usado pelo URP/Lit. Se o projeto usar o Built in Render Pipeline
    // com o shader Standard, troque para "_Color".
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
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
}
