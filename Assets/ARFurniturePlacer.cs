using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Coloca este script num objeto que também tenha AR Session Origin / AR Raycast Manager na cena.
[RequireComponent(typeof(ARRaycastManager))]
public class ARFurniturePlacer : MonoBehaviour
{
    [Tooltip("Lista de prefabs de móveis que podem ser colocados (sofá, mesa, cadeira, etc).")]
    [SerializeField] private List<GameObject> furniturePrefabs;

    [Tooltip("Transform vazio que vai agrupar todos os móveis instanciados na cena.")]
    [SerializeField] private Transform spawnRoot;

    private ARRaycastManager raycastManager;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private int selectedPrefabIndex = 0;

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    // Chame este método a partir dos botões da UI que escolhem qual móvel colocar.
    public void SelectPrefab(int index)
    {
        if (index >= 0 && index < furniturePrefabs.Count)
            selectedPrefabIndex = index;
    }

    private void Update()
    {
        if (!TryGetPointerDown(out Vector2 screenPosition)) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return; // ignora cliques/toques que começam em cima de botões da UI

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            PlaceFurniture(hitPose);
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

    private void PlaceFurniture(Pose pose)
    {
        if (furniturePrefabs == null || furniturePrefabs.Count == 0) return;

        GameObject prefab = furniturePrefabs[selectedPrefabIndex];
        GameObject instance = Instantiate(prefab, pose.position, pose.rotation, spawnRoot);

        if (instance.GetComponent<ColorableObject>() == null)
            instance.AddComponent<ColorableObject>();

        if (instance.GetComponent<Collider>() == null)
            instance.AddComponent<BoxCollider>(); // ideal é já vir configurado no prefab
    }
}
