using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _cameraRoot;

    [Header("Player Data")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float deceleration;
    [SerializeField] private float rotationSpeed;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float maxFallSpeed = -40f;
    [SerializeField] private float groundedDownForce = -2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController _characterController;
    private Animator _animator;
    private Collider[] _groundHitCollider = new Collider[1];

    // Player variables
    private float _moveAmount;
    private Vector2 _movementInput;
    private float _verticalVelocity;
    private Vector3 _currentMovementVelocity;
    private Vector3 _targetMovementVelocity;
    private Vector3 _flatMoveDirection;

    // Animation Hashes
    private static readonly int _animMoveY = Animator.StringToHash("moveY");

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        
        if (_cameraRoot == null)
        {
            _cameraRoot = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleGravity();
        HandleLocomotion();
        HandleRotation();
        ApplyFinalMovement();
        UpdateAnimator();
    }

    private void ApplyFinalMovement()
    {
        Vector3 finalMovement = _currentMovementVelocity + Vector3.up * _verticalVelocity;
        _characterController.Move(finalMovement * Time.deltaTime);
    }

    private void HandleLocomotion()
    {
        float targetSpeed = 0.0f;
        _moveAmount = _movementInput.magnitude;

        if (_moveAmount > 0.01f)
        {
            targetSpeed = _moveAmount > 0.5f ? runSpeed : walkSpeed;
        }

        Vector3 moveDir = _cameraRoot.forward * _movementInput.y
                + _cameraRoot.right * _movementInput.x;
        moveDir.y = 0;
        if (moveDir.sqrMagnitude > 0.001f) moveDir.Normalize();

        _flatMoveDirection = moveDir;
        _targetMovementVelocity = moveDir * targetSpeed;

        float accelRate = (_targetMovementVelocity.sqrMagnitude > 0.001f) ? acceleration : deceleration;

        _currentMovementVelocity = Vector3.MoveTowards(_currentMovementVelocity, _targetMovementVelocity, accelRate * Time.deltaTime);
    }

    public void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;

        targetDirection = _cameraRoot.forward * _movementInput.y;
        targetDirection = targetDirection + _cameraRoot.right * _movementInput.x;
        targetDirection.Normalize();
        targetDirection.y = 0;

        if (targetDirection == Vector3.zero)
        {
            targetDirection = transform.forward;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.rotation = playerRotation;
    }

    private void HandleGravity()
    {
        if (CheckIfGrounded() && _verticalVelocity < 0.01f)
        {
            _verticalVelocity = groundedDownForce;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, maxFallSpeed);
        }
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat(_animMoveY, _moveAmount, 0.1f, Time.deltaTime);
    }

    private bool CheckIfGrounded()
    {
        int hit = Physics.OverlapSphereNonAlloc(groundCheckPoint.position, groundCheckRadius, _groundHitCollider, groundMask);
        return hit > 0 || _characterController.isGrounded;
    }

    #region Input Methods

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        _movementInput = context.ReadValue<Vector2>();
    }

    #endregion

}
