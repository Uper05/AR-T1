using UnityEngine;

public class ObjectColorSelector : MonoBehaviour
{
    [Tooltip("Câmera do AR Session Origin.")]
    [SerializeField] private Camera arCamera;

    private ColorableObject selectedObject;

    private void Update()
    {
        if (!TryGetPointerDown(out Vector2 screenPosition)) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ColorableObject obj = hit.collider.GetComponentInParent<ColorableObject>();
            if (obj != null)
                selectedObject = obj;
        }
    }

    // Detecta toque real (celular) e também clique de mouse (para testar no Editor/PC).
    private bool TryGetPointerDown(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
            screenPosition = default;
            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    // Ligado aos botões de cor da interface (ver ColorButton.cs).
    public void OnColorButtonPressed(Color color)
    {
        if (selectedObject != null)
            selectedObject.ApplyColor(color);
    }
}
