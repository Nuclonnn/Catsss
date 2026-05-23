using Unity.Cinemachine;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(CinemachineInputAxisController))]
public class CameraManager : ValidatedMonoBehaviour {
    [Header("References")]
    [SerializeField, Anywhere] private InputReader input;
    [SerializeField, Self] private CinemachineInputAxisController axisController;

    bool isMouseControlEnabled;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (axisController == null)
            axisController = GetComponent<CinemachineInputAxisController>();
    }

    void OnEnable()
    {
        input.ToggleMouseControlCamera += OnToggleMouseControlCamera;
    }

    void Start()
    {
        ApplyMouseControlState(false);
    }
        
    void OnDisable()
    {
        input.ToggleMouseControlCamera -= OnToggleMouseControlCamera;

        // Возвращаем курсор в безопасное состояние при отключении объекта.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnToggleMouseControlCamera()
    {
        ApplyMouseControlState(!isMouseControlEnabled);
    }

    void ApplyMouseControlState(bool enabled)
    {
        isMouseControlEnabled = enabled;

        if (axisController != null)
            axisController.enabled = enabled;

        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

}