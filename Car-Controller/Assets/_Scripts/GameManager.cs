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
        if (controllers == null || controllers.Count == 0)
        {
            Debug.LogError("GameManager: No controllers assigned!");
            return;
        }

        _currentControllerIndex = 0;
        _currentController = controllers[_currentControllerIndex];

        if (followCamera != null)
            followCamera.Target.TrackingTarget = _currentController.transform;
    }

    public void OnChangeCarInput(InputAction.CallbackContext context)
    {
        if (!context.performed || controllers.Count <= 1) return;

        if (_currentController != null)
        {
            _currentController._moveInput = Vector2.zero;
        }

        _currentControllerIndex = (_currentControllerIndex + 1) % controllers.Count;
        _currentController = controllers[_currentControllerIndex];

        if (followCamera != null)
        {
            followCamera.Target.TrackingTarget = _currentController.transform;
        }
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (_currentController != null)
        {
            _currentController._moveInput = context.ReadValue<Vector2>();
        }
    }
}