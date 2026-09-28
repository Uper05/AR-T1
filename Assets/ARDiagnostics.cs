#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;

// Diagnóstico temporário do XR Simulation: só existe no Editor e se cria sozinho ao dar Play.
// Escreve no Console, a cada 2 s, o estado do XR, da sessão AR, dos planos, da câmera e do mouse.
// Pode apagar este arquivo quando a simulação estiver funcionando.
public class ARDiagnostics : MonoBehaviour
{
    private float nextLog;
    private int logsLeft = 20;
    private int clicks;
    private int rightHeldFrames;
    private int wasdFrames;
    private float mouseDeltaSum;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("[ARDiagnostics]");
        DontDestroyOnLoad(go);
        go.AddComponent<ARDiagnostics>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) clicks++;

        // O que o Input System (usado pela navegação do XR Simulation) está recebendo.
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse != null && mouse.rightButton.isPressed)
        {
            rightHeldFrames++;
            mouseDeltaSum += mouse.delta.ReadValue().magnitude;
        }
        if (kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed))
            wasdFrames++;

        if (logsLeft <= 0 || Time.unscaledTime < nextLog) return;
        nextLog = Time.unscaledTime + 2f;
        logsLeft--;

        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        string loader = manager == null ? "sem XRManager"
            : manager.activeLoader == null ? $"NENHUM loader ativo (inicializado={manager.isInitializationComplete})"
            : manager.activeLoader.name;

        var planeManager = FindFirstObjectByType<ARPlaneManager>();
        int planes = planeManager != null ? planeManager.trackables.count : -1;

        var cam = Camera.main;
        string camInfo = cam != null
            ? $"pos={cam.transform.position:F2} rot={cam.transform.eulerAngles:F0}"
            : "sem Camera.main";

        int origins = FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length;
        var placer = FindFirstObjectByType<ARFurniturePlacer>();

        Debug.Log($"[ARDiag] loader={loader} | session={ARSession.state} ({ARSession.notTrackingReason}) | " +
                  $"planos={planes} | xrOrigins={origins} | camera {camInfo} | cliques={clicks} | " +
                  $"placer={(placer != null ? "ok" : "AUSENTE")}");
        Debug.Log($"[ARDiag] InputSystem: mouse={(Mouse.current != null)} teclado={(Keyboard.current != null)} | " +
                  $"framesBotaoDireito={rightHeldFrames} somaDeltaMouse={mouseDeltaSum:F0} framesWASD={wasdFrames} | " +
                  $"playModeBehavior={InputSystem.settings.editorInputBehaviorInPlayMode} " +
                  $"background={InputSystem.settings.backgroundBehavior} focado={Application.isFocused}");
    }
}
#endif
