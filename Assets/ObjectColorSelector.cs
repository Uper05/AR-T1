using UnityEngine;

// Guarda qual objeto está selecionado. A entrada (toque/clique) é tratada pelo ARFurniturePlacer,
// que chama TrySelectAt antes de decidir se o toque deve colocar um objeto novo.
public class ObjectColorSelector : MonoBehaviour
{
    [Tooltip("Câmera do AR Session Origin.")]
    [SerializeField] private Camera arCamera;

    [Tooltip("Painel com a paleta de cores e ações; só aparece quando há um objeto selecionado.")]
    [SerializeField] private GameObject selectionPanel;

    public ColorableObject Selected { get; private set; }

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    // Retorna true se o toque acertou um objeto colorível (que passa a ser o selecionado).
    public bool TrySelectAt(Vector2 screenPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ColorableObject obj = hit.collider.GetComponentInParent<ColorableObject>();
            if (obj != null)
            {
                Select(obj);
                return true;
            }
        }
        return false;
    }

    public void Select(ColorableObject obj)
    {
        if (Selected == obj) return;

        if (Selected != null)
            Selected.SetHighlighted(false);

        Selected = obj;

        if (Selected != null)
            Selected.SetHighlighted(true);

        if (selectionPanel != null)
            selectionPanel.SetActive(Selected != null);
    }

    public void ClearSelection() => Select(null);

    // Ligado aos botões de cor da interface (ver ColorButton.cs).
    public void OnColorButtonPressed(Color color)
    {
        if (Selected != null)
            Selected.ApplyColor(color);
    }

    // Ligado ao botão "Restaurar" da interface.
    public void ResetSelectedColor()
    {
        if (Selected != null)
            Selected.ResetColor();
    }

    // Ligado ao botão "Excluir" da interface.
    public void DeleteSelected()
    {
        if (Selected == null) return;

        GameObject target = Selected.gameObject;
        Select(null);
        Destroy(target);
    }
}
