using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    private Vector2 _moveInput;
    private bool _driftInput;

    [SerializeField] private DriftingController controller;

    public DriftingController Controller { get => controller; set => controller = value; }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();

        if (controller != null)
        {
            controller.MoveInput = _moveInput;
        }
    }

    public void OnDriftInput(InputAction.CallbackContext context)
    {
        if (controller != null)
        {
            controller.DriftInput = context.ReadValueAsButton();
        }
    }
}
