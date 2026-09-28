using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using Unity.XR.CoreUtils;

public static class ProjectAutoFix
{
    private const string ScenePath = "Assets/a.unity";
    private const string FbxDir = "Assets/FBX";
    private const string PrefabDir = "Assets/Prefabs/Furniture";
    private const string PlanePrefabPath = "Assets/Prefabs/AR Default Plane.prefab";

    [MenuItem("Tools/AutoFix/Run All")]
    public static void Run()
    {
        CreateFurniturePrefabs();
        var prefabs = LoadFurniturePrefabs();
        FixScene(prefabs);
        FixBuildSettings();
        FixXRInit();
        FixApplicationId();
        FixXRSimulationControls();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ProjectAutoFix] Done. {prefabs.Length} furniture prefabs wired.");
    }

    private static void CreateFurniturePrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Furniture");

        var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { FbxDir });
        foreach (var guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            string prefabPath = $"{PrefabDir}/{name}.prefab";

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (fbx == null) continue;

            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.transform.SetParent(root.transform, false);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1))
                    bounds.Encapsulate(r.bounds);

                var collider = root.AddComponent<BoxCollider>();
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.size = bounds.size;
            }
            else
            {
                root.AddComponent<BoxCollider>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject[] LoadFurniturePrefabs()
    {
        return AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(p => p != null)
            .ToArray();
    }

    private static void FixScene(GameObject[] prefabs)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var placer = Object.FindFirstObjectByType<ARFurniturePlacer>(FindObjectsInactive.Include);
        if (placer == null)
        {
            Debug.LogError("[ProjectAutoFix] ARFurniturePlacer not found in scene.");
            return;
        }

        var placerSO = new SerializedObject(placer);
        var listProp = placerSO.FindProperty("furniturePrefabs");
        listProp.ClearArray();
        for (int i = 0; i < prefabs.Length; i++)
        {
            listProp.InsertArrayElementAtIndex(i);
            listProp.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }
        var selector = Object.FindFirstObjectByType<ObjectColorSelector>(FindObjectsInactive.Include);
        if (selector != null)
            placerSO.FindProperty("selector").objectReferenceValue = selector;
        else
            Debug.LogWarning("[ProjectAutoFix] ObjectColorSelector not found in scene.");
        placerSO.ApplyModifiedProperties();

        var planeManager = Object.FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        FixDuplicateXROrigin(placer, planeManager);

        if (planeManager != null)
        {
            var planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            var planeSO = new SerializedObject(planeManager);
            planeSO.FindProperty("m_PlanePrefab").objectReferenceValue = planePrefab;
            planeSO.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("[ProjectAutoFix] ARPlaneManager not found in scene.");
        }

        ConfigureCanvasScaler();
        BuildFurnitureUI(placer, prefabs);
        if (selector != null)
            BuildSelectionPanel(selector);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // O placer não pode ter um XR Origin próprio: ele deve usar o ARRaycastManager do XR Origin
    // que tem a câmera e o ARPlaneManager. Duas origens na cena confundem o AR Foundation.
    private static void FixDuplicateXROrigin(ARFurniturePlacer placer, ARPlaneManager planeManager)
    {
        if (planeManager == null) return;

        var mainOrigin = planeManager.gameObject;
        if (placer.gameObject != mainOrigin)
        {
            var extraRaycast = placer.GetComponent<ARRaycastManager>();
            if (extraRaycast != null) Object.DestroyImmediate(extraRaycast);
            var extraOrigin = placer.GetComponent<XROrigin>();
            if (extraOrigin != null) Object.DestroyImmediate(extraOrigin);
        }

        var raycastManager = mainOrigin.GetComponent<ARRaycastManager>();
        if (raycastManager == null) raycastManager = mainOrigin.AddComponent<ARRaycastManager>();

        var placerSO = new SerializedObject(placer);
        placerSO.FindProperty("raycastManager").objectReferenceValue = raycastManager;
        placerSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildFurnitureUI(ARFurniturePlacer placer, GameObject[] prefabs)
    {
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogWarning("[ProjectAutoFix] Canvas not found in scene, skipping UI creation.");
            return;
        }

        var existingPanel = canvasObj.transform.Find("FurniturePanel");
        if (existingPanel != null)
            Object.DestroyImmediate(existingPanel.gameObject);

        var panel = new GameObject("FurniturePanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 40);
        panelRect.sizeDelta = new Vector2(0, 170);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.35f);

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(panel.transform, false);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        vpRect.pivot = new Vector2(0.5f, 0.5f);
        var vpImage = viewport.AddComponent<Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.01f);
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0.5f);
        contentRect.anchorMax = new Vector2(0, 0.5f);
        contentRect.pivot = new Vector2(0, 0.5f);

        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = vpRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        for (int i = 0; i < prefabs.Length; i++)
        {
            var btnObj = new GameObject(prefabs[i].name + "Button", typeof(RectTransform));
            btnObj.transform.SetParent(content.transform, false);
            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(220, 130);

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.sprite = buttonSprite;
            btnImage.type = Image.Type.Sliced;
            btnImage.color = Color.white;

            var button = btnObj.AddComponent<Button>();

            var textObj = new GameObject("Text (TMP)", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = prefabs[i].name;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.fontSize = 30;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            int index = i;
            UnityEventTools.AddIntPersistentListener(button.onClick, placer.SelectPrefab, index);
        }
    }

    private static readonly Color[] PaletteColors =
    {
        Color.white,
        new Color(0.12f, 0.12f, 0.12f),
        new Color(0.55f, 0.55f, 0.55f),
        new Color(0.85f, 0.15f, 0.15f),
        new Color(0.95f, 0.55f, 0.10f),
        new Color(0.98f, 0.85f, 0.20f),
        new Color(0.25f, 0.70f, 0.30f),
        new Color(0.10f, 0.60f, 0.60f),
        new Color(0.15f, 0.40f, 0.85f),
        new Color(0.50f, 0.25f, 0.75f),
        new Color(0.95f, 0.50f, 0.70f),
        new Color(0.45f, 0.28f, 0.15f),
    };

    // Canvas pensado para celular em retrato: escala conforme a resolução da tela.
    private static void ConfigureCanvasScaler()
    {
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    // Painel com a paleta de cores, "Restaurar" e "Excluir".
    // Fica logo acima da barra de móveis e só aparece com um objeto selecionado.
    private static void BuildSelectionPanel(ObjectColorSelector selector)
    {
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return;

        // Remove UI antiga: botões de cor soltos e o botão "Excluir" do canto.
        foreach (var oldButton in canvasObj.GetComponentsInChildren<ColorButton>(true))
            Object.DestroyImmediate(oldButton.gameObject);
        foreach (var name in new[] { "DeleteButton", "SelectionPanel" })
        {
            var existing = canvasObj.transform.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }

        var panel = new GameObject("SelectionPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 230);
        panelRect.sizeDelta = new Vector2(-40, 0);

        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var panelFitter = panel.AddComponent<ContentSizeFitter>();
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Paleta: 2 linhas de 6 cores.
        var palette = new GameObject("Palette", typeof(RectTransform));
        palette.transform.SetParent(panel.transform, false);
        var grid = palette.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(130, 130);
        grid.spacing = new Vector2(24, 24);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.MiddleCenter;

        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        for (int i = 0; i < PaletteColors.Length; i++)
        {
            var swatch = new GameObject($"Color{i}", typeof(RectTransform));
            swatch.transform.SetParent(palette.transform, false);
            var img = swatch.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = PaletteColors[i];
            swatch.AddComponent<Button>();
            var colorButton = swatch.AddComponent<ColorButton>();

            var so = new SerializedObject(colorButton);
            so.FindProperty("color").colorValue = PaletteColors[i];
            so.FindProperty("selector").objectReferenceValue = selector;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Ações.
        var actions = new GameObject("Actions", typeof(RectTransform));
        actions.transform.SetParent(panel.transform, false);
        var hlg = actions.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        actions.AddComponent<LayoutElement>().preferredHeight = 110;

        var resetButton = CreateTextButton(actions.transform, "ResetColorButton", "Restaurar cor",
            new Color(0.9f, 0.9f, 0.9f), Color.black, sprite);
        UnityEventTools.AddPersistentListener(resetButton.onClick, selector.ResetSelectedColor);

        var deleteButton = CreateTextButton(actions.transform, "DeleteButton", "Excluir",
            new Color(0.85f, 0.25f, 0.25f), Color.white, sprite);
        UnityEventTools.AddPersistentListener(deleteButton.onClick, selector.DeleteSelected);

        var selectorSO = new SerializedObject(selector);
        selectorSO.FindProperty("selectionPanel").objectReferenceValue = panel;
        selectorSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button CreateTextButton(Transform parent, string name, string label,
        Color background, Color textColor, Sprite sprite)
    {
        var btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        var img = btnObj.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = background;
        var button = btnObj.AddComponent<Button>();

        var textObj = new GameObject("Text (TMP)", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.fontSize = 36;

        return button;
    }

    private static void FixBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }

    private static void FixXRInit()
    {
        const string path = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        var objs = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var o in objs)
        {
            if (o == null) continue;
            if (o.name != "Android Providers" && o.name != "Standalone Providers") continue;

            var so = new SerializedObject(o);
            var autoLoading = so.FindProperty("m_AutomaticLoading");
            var autoRunning = so.FindProperty("m_AutomaticRunning");
            if (autoLoading != null) autoLoading.boolValue = true;
            if (autoRunning != null) autoRunning.boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(o);
        }
        AssetDatabase.SaveAssets();
    }

    // Neste Editor Linux o Input System não recebe o botão direito do mouse, e o XR Simulation só
    // libera a navegação enquanto a ação "Unlock" (botão direito) está pressionada. Usamos controles
    // próprios sem "Unlock" (navegação sempre ativa): WASD/QE movem, setas olham ao redor.
    private const string SimulationControlsPath = "Assets/XR/XRSimulationControls.inputactions";
    private const string SimulationPreferencesPath = "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";

    private static void FixXRSimulationControls()
    {
        AssetDatabase.ImportAsset(SimulationControlsPath);
        var references = AssetDatabase.LoadAllAssetsAtPath(SimulationControlsPath)
            .OfType<UnityEngine.InputSystem.InputActionReference>()
            .Where(r => r.action != null)
            .ToDictionary(r => r.action.name);

        var preferences = AssetDatabase.LoadMainAssetAtPath(SimulationPreferencesPath);
        if (preferences == null || !references.ContainsKey("Move") || !references.ContainsKey("Look"))
        {
            Debug.LogWarning("[ProjectAutoFix] XR Simulation controls or preferences not found, skipping.");
            return;
        }

        var so = new SerializedObject(preferences);
        so.FindProperty("m_UnlockInputActionReference").objectReferenceValue = null;
        so.FindProperty("m_MoveInputActionReference").objectReferenceValue = references["Move"];
        so.FindProperty("m_LookInputActionReference").objectReferenceValue = references["Look"];
        so.FindProperty("m_SprintInputActionReference").objectReferenceValue = references["Sprint"];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(preferences);
    }

    private static void FixApplicationId()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.DefaultCompany.ARFurniturePlacer");
    }
}
