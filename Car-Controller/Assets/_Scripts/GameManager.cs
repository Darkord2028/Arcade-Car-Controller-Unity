using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<ArcadeController> controllers;
    [SerializeField] private CinemachineCamera followCamera;

    private ArcadeController _currentController;

    private int _currentControllerIndex;

    void Start()
    {
        _currentController = controllers[0];
        _currentControllerIndex = 0;
        followCamera.Target.TrackingTarget = _currentController.transform;
    }

    public void OnChangeCarInput(InputAction.CallbackContext context)
    {
        int totalCars = controllers.Count;
        if (_currentControllerIndex + 1 > totalCars)
        {
            _currentControllerIndex = 0;
            _currentController = controllers[_currentControllerIndex];
        }
        else
        {
            _currentControllerIndex++;
            _currentController = controllers[_currentControllerIndex];
        }

        followCamera.Target.TrackingTarget = _currentController.transform;
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        _currentController._moveInput = context.ReadValue<Vector2>();
    }

    public void OnDriftInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _currentController._driftInput = true;
        }
        if (context.canceled)
        {
            _currentController._driftInput = false;
        }
    }
}
