using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using static Platformer;

[CreateAssetMenu(fileName = "InputReader", menuName = "Platformer/InputReader")]
public class InputReader : ScriptableObject, Platformer.IPlayerActions
{
    public event UnityAction<Vector2> Move = delegate { };

    public event UnityAction<Vector2, bool> Look = delegate { };
    public event UnityAction ToggleMouseControlCamera = delegate { };
    public event UnityAction<bool> Jump = delegate{};
    public event UnityAction<bool> Run = delegate{};
    public event UnityAction Attack = delegate { };

    Platformer inputActions;
    public Vector3 Direction => (Vector3)inputActions.Player.Move.ReadValue<Vector2>();

    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new Platformer();
            inputActions.Player.SetCallbacks(instance: this);
        }
    }
    public void EnablePlayerActions(){
        inputActions.Enable();
    }


    public void OnMove(InputAction.CallbackContext context)
    {
        Move.Invoke(arg0: context.ReadValue<Vector2>());
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        Look.Invoke(context.ReadValue<Vector2>(), IsDeviceMouse(context));
    }
    private bool IsDeviceMouse(InputAction.CallbackContext context)
    {
        return context.control.device.name == "Mouse";
    }


    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            Attack.Invoke();
    }

    public void OnMouseControlCamera(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            ToggleMouseControlCamera.Invoke();
    }
    public void OnRun(InputAction.CallbackContext context)
    {
        switch (context.phase){
            case InputActionPhase.Started:
                Run.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Run.Invoke(false);
                break;
        }
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        switch (context.phase){
            case InputActionPhase.Started:
            Jump.Invoke(true);
            break;
            case InputActionPhase.Canceled:
            Jump.Invoke(false);
            break;
        }
    }
}
