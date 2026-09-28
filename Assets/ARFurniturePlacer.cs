using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Usa o ARRaycastManager do XR Origin da cena (não adicione outro XR Origin neste objeto).
//
// Gestos:
//  - Toque num móvel: seleciona. Arrastar em seguida: move o móvel sobre o plano.
//  - Toque num plano vazio: coloca o móvel escolhido e já o seleciona.
//  - Dois dedos: pinça escala e torção gira o móvel selecionado.
//  - No PC/Editor: clique = toque, roda do mouse = escala, botão do meio arrastando = girar
//    (o botão direito fica livre para a navegação do XR Simulation).
public class ARFurniturePlacer : MonoBehaviour
{
    [Tooltip("Lista de prefabs de móveis que podem ser colocados (sofá, mesa, cadeira, etc).")]
    [SerializeField] private List<GameObject> furniturePrefabs;

    [Tooltip("Transform vazio que vai agrupar todos os móveis instanciados na cena.")]
    [SerializeField] private Transform spawnRoot;

    [Tooltip("ARRaycastManager que fica no XR Origin.")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Tooltip("Controla a seleção e a cor dos móveis.")]
    [SerializeField] private ObjectColorSelector selector;

    [Header("Manipulação")]
    [SerializeField] private float minScale = 0.2f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float mouseScaleSpeed = 0.1f;
    [SerializeField] private float mouseRotateSpeed = 0.3f;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private int selectedPrefabIndex = 0;

    private bool isDragging;
    private Vector3 dragOffset;
    private bool twoFingerActive;
    private float lastPinchDistance;
    private float lastTwistAngle;
    private Vector3 lastMousePosition;

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (selector == null)
            selector = FindFirstObjectByType<ObjectColorSelector>();
    }

    // Chame este método a partir dos botões da UI que escolhem qual móvel colocar.
    public void SelectPrefab(int index)
    {
        if (index >= 0 && index < furniturePrefabs.Count)
            selectedPrefabIndex = index;
    }

    private void Update()
    {
        if (Input.touchCount >= 2)
        {
            HandleTwoFingers(Input.GetTouch(0), Input.GetTouch(1));
            return;
        }
        twoFingerActive = false;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began: OnPointerDown(touch.position); break;
                case TouchPhase.Moved: OnPointerDrag(touch.position); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: isDragging = false; break;
            }
            return;
        }

        HandleMouse();
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0)) OnPointerDown(Input.mousePosition);
        else if (Input.GetMouseButton(0)) OnPointerDrag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0)) isDragging = false;

        ColorableObject target = selector != null ? selector.Selected : null;
        if (target == null)
        {
            lastMousePosition = Input.mousePosition;
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f && !IsPointerOverUI(Input.mousePosition))
            ScaleTarget(target, 1f + scroll * mouseScaleSpeed);

        if (Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2))
        {
            float deltaX = Input.mousePosition.x - lastMousePosition.x;
            target.transform.Rotate(0f, -deltaX * mouseRotateSpeed, 0f, Space.World);
        }
        lastMousePosition = Input.mousePosition;
    }

    private void OnPointerDown(Vector2 screenPosition)
    {
        isDragging = false;
        if (IsPointerOverUI(screenPosition)) return; // toques que começam em cima de botões da UI

        bool hitPlane = raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);

        // Primeiro verifica se tocou num móvel já colocado: nesse caso só seleciona (e permite arrastar).
        if (selector != null && selector.TrySelectAt(screenPosition))
        {
            isDragging = true;
            Vector3 objPos = selector.Selected.transform.position;
            dragOffset = hitPlane ? objPos - hits[0].pose.position : Vector3.zero;
            dragOffset.y = 0f;
            return;
        }

        if (hitPlane)
        {
            GameObject instance = PlaceFurniture(hits[0].pose);
            if (instance != null && selector != null)
            {
                selector.Select(instance.GetComponent<ColorableObject>());
                isDragging = true;
                dragOffset = Vector3.zero;
            }
        }
        else if (selector != null)
        {
            selector.ClearSelection();
        }
    }

    private void OnPointerDrag(Vector2 screenPosition)
    {
        if (!isDragging || selector == null || selector.Selected == null) return;

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            selector.Selected.transform.position = hits[0].pose.position + dragOffset;
    }

    private void HandleTwoFingers(Touch t0, Touch t1)
    {
        isDragging = false;
        ColorableObject target = selector != null ? selector.Selected : null;
        if (target == null) return;

        Vector2 delta = t1.position - t0.position;
        float distance = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        if (!twoFingerActive || t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            twoFingerActive = true;
            lastPinchDistance = distance;
            lastTwistAngle = angle;
            return;
        }

        if (lastPinchDistance > 0.01f)
            ScaleTarget(target, distance / lastPinchDistance);

        float twist = Mathf.DeltaAngle(lastTwistAngle, angle);
        target.transform.Rotate(0f, -twist, 0f, Space.World);

        lastPinchDistance = distance;
        lastTwistAngle = angle;
    }

    // minScale/maxScale são relativos ao tamanho original do prefab.
    private void ScaleTarget(ColorableObject target, float factor)
    {
        Vector3 initial = target.InitialScale;
        float current = target.transform.localScale.x / initial.x;
        float newScale = Mathf.Clamp(current * factor, minScale, maxScale);
        target.transform.localScale = initial * newScale;
    }

    // Faz o raycast da UI manualmente para funcionar igual com toque e mouse,
    // independente do módulo de input usado pelo EventSystem.
    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        uiHits.Clear();
        EventSystem.current.RaycastAll(eventData, uiHits);
        return uiHits.Count > 0;
    }

    private GameObject PlaceFurniture(Pose pose)
    {
        if (furniturePrefabs == null || furniturePrefabs.Count == 0) return null;

        GameObject prefab = furniturePrefabs[selectedPrefabIndex];
        GameObject instance = Instantiate(prefab, pose.position, pose.rotation, spawnRoot);

        if (instance.GetComponent<ColorableObject>() == null)
            instance.AddComponent<ColorableObject>();

        if (instance.GetComponent<Collider>() == null)
            instance.AddComponent<BoxCollider>(); // ideal é já vir configurado no prefab

        return instance;
    }
}
