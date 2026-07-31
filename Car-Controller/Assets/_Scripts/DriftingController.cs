using UnityEngine;

public class DriftingController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform _cameraTransform;

    [Header("Wheels")]
    [SerializeField] Transform[] wheelPoints; // 0, 1 - front, 3, 4 - rear
    [SerializeField] Transform[] wheelMeshes;
    [SerializeField] LayerMask whatIsDrivable;

    [Header("Suspension Config")]
    [SerializeField] private float springStiffness = 25000f;
    [SerializeField] private float damperStiffness = 2500f;
    [SerializeField] private float restLength = 0.3f;
    [SerializeField] private float sprintTravel = 0.15f;
    [SerializeField] private float wheelRadius = 0.3f;

    [Header("Acceleration")]
    [SerializeField] private float maxSpeed = 45f;
    [SerializeField] private float acceleration = 1500f;
    [SerializeField] private float deceleration = 800f;
    [SerializeField, Range(0, 1)] private float sidewaysSlip = 0.8f;
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Drifting")]
    [SerializeField] private float baseDriftAngle = 35f;
    [SerializeField] private float driftSteerInfluence = 15f;

    [Header("Chassis Visual Polish")]
    [SerializeField] private Transform _chassisMesh;

    [Header("Normal Steering Visuals")]
    [SerializeField] private float normalYawAngle = 15f;
    [SerializeField] private float normalRollAngle = 10f;

    [Header("Drift Visuals")]
    [SerializeField] private float driftYawAngle = 25f;
    [SerializeField] private float driftCounterRollAngle = 15f;
    [SerializeField] private float leanSmoothSpeed = 12f;

    [Header("Camera Alignment Torque")]
    [SerializeField] private float turnStrength = 60f;
    [SerializeField] private float turnDamper = 10f;
    [SerializeField] private float maxAlignmentTorque = 100f;

    [Header("Camera Steer")]
    [SerializeField] private float turnSpeed = 120f;

    // Input Variables
    private Vector2 _moveInput;
    private bool _driftInput;

    // Getters and Setters
    public Vector2 MoveInput { get => _moveInput; set => _moveInput = value; }
    public bool DriftInput { get => _driftInput; set => _driftInput = value; }

    // Components
    private Rigidbody _rigidbody;

    // Private Variables
    private readonly RaycastHit[] _wheelHitResults = new RaycastHit[1];
    private float[] _wheelCompressions = new float[4];
    private bool[] _isGrounded = new bool[4];

    private int _driftDirection = 0; // -1 = left, 1 = right, 0 = not drifting

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float turnAmount = _moveInput.x * turnSpeed * Time.deltaTime;
        _cameraTransform.Rotate(0f, turnAmount, 0f, Space.World);
    }

    private void FixedUpdate()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float currentSpeed = Vector3.Dot(_rigidbody.linearVelocity, flatForward);

        UpdateDriftState();

        int groundedWheelCount = 0;

        for (int i = 0; i < wheelPoints.Length; i++)
        {
            Transform wheel = wheelPoints[i];
            float maxLength = restLength + sprintTravel;

            int hits = Physics.RaycastNonAlloc(
                wheel.position,
                -wheel.up,
                _wheelHitResults,
                maxLength + wheelRadius,
                whatIsDrivable
            );

            if (hits > 0)
            {
                RaycastHit hit = _wheelHitResults[0];
                _isGrounded[i] = true;
                groundedWheelCount++;

                float currentSpringLength = hit.distance - wheelRadius;
                _wheelCompressions[i] = Mathf.Clamp01((restLength - currentSpringLength) / sprintTravel);

                ApplySuspension(i, wheel, hit);
            }
            else
            {
                _isGrounded[i] = false;
                _wheelCompressions[i] = 0f;
                Debug.DrawLine(wheel.position, wheel.position - wheel.up * maxLength, Color.red);
            }
        }

        if (groundedWheelCount > 0)
        {
            ApplyThrottle(currentSpeed);
            ApplyBraking();
            AlignToCamera();
            HandleLateralSlip();
        }
    }

    private void LateUpdate()
    {
        if (_cameraTransform != null)
        {
            _cameraTransform.position = transform.position;
        }

        AnimateChassis();
    }

    private void AnimateChassis()
    {
        if (_chassisMesh == null) return;

        float targetYawY = 0f;
        float targetRollZ = 0f;

        if (_driftDirection != 0)
        {
            targetYawY = (_driftDirection * driftYawAngle) + (_moveInput.x * 10f);
            targetRollZ = _driftDirection * driftCounterRollAngle;
        }
        else
        {
            targetYawY = _moveInput.x * normalYawAngle;
            targetRollZ = -_moveInput.x * normalRollAngle;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, targetYawY, targetRollZ);
        _chassisMesh.localRotation = Quaternion.Slerp(
            _chassisMesh.localRotation,
            targetRotation,
            Time.deltaTime * leanSmoothSpeed
        );

        float wheelSteerAngle = _moveInput.x * 30f;
        for (int i = 0; i < 2; i++)
        {
            if (wheelMeshes[i] != null)
            {
                wheelMeshes[i].localRotation = Quaternion.Euler(0f, wheelSteerAngle, 0f);
            }
        }
    }

    private void UpdateDriftState()
    {
        if (_driftInput)
        {
            if (_driftDirection == 0)
            {
                if (_moveInput.x < -0.1f)
                    _driftDirection = -1;
                else if (_moveInput.x > 0.1f)
                    _driftDirection = 1;
                else
                    _driftDirection = 1;
            }
        }
        else
        {
            _driftDirection = 0;
        }
    }

    private void ApplySuspension(int index, Transform wheel, RaycastHit hit)
    {
        Vector3 wheelVelocity = _rigidbody.GetPointVelocity(wheel.position);
        float springVelocity = Vector3.Dot(wheelVelocity, wheel.up);

        float damperForce = damperStiffness * springVelocity;
        float springForce = _wheelCompressions[index] * springStiffness;
        float netForce = springForce - damperForce;

        _rigidbody.AddForceAtPosition(netForce * wheel.up, wheel.position);
        Debug.DrawLine(wheel.position, hit.point, Color.green);
    }

    private void ApplyThrottle(float currentSpeed)
    {
        if (_cameraTransform == null) return;

        float throttleInput = (_driftDirection != 0) ? 1f : _moveInput.y;
        if (Mathf.Abs(throttleInput) < 0.01f) return;

        if (Mathf.Abs(currentSpeed) >= maxSpeed && throttleInput > 0f) return;

        Vector3 accelDir = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;

        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float availableTorque = powerCurve.Evaluate(normalizedSpeed) * throttleInput * acceleration;

        _rigidbody.AddForce(accelDir * availableTorque);
        Vector3 flatVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 clampedVelocity = flatVelocity.normalized * maxSpeed;
            _rigidbody.linearVelocity = new Vector3(clampedVelocity.x, _rigidbody.linearVelocity.y, clampedVelocity.z);
        }
    }

    private void ApplyBraking()
    {
        if (Mathf.Abs(_moveInput.y) > 0.01f || _driftDirection != 0 || _cameraTransform == null) return;

        Vector3 flatCamForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        float forwardSpeed = Vector3.Dot(flatCamForward, _rigidbody.linearVelocity);

        if (Mathf.Abs(forwardSpeed) < 0.01f) return;

        float brakeForce = -Mathf.Sign(forwardSpeed) * deceleration;
        float lowSpeedThreshold = 2.0f;
        if (Mathf.Abs(forwardSpeed) < lowSpeedThreshold)
        {
            brakeForce = -forwardSpeed * (deceleration / lowSpeedThreshold);
        }

        _rigidbody.AddForce(flatCamForward * brakeForce);
    }

    private void AlignToCamera()
    {
        if (_cameraTransform == null) return;

        Vector3 carFwdFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 camFwdFlat = new Vector3(_cameraTransform.forward.x, 0f, _cameraTransform.forward.z).normalized;

        if (_driftDirection != 0)
        {
            float driftAngle = (baseDriftAngle * _driftDirection) + (_moveInput.x * driftSteerInfluence);
            camFwdFlat = Quaternion.AngleAxis(driftAngle, Vector3.up) * camFwdFlat;
        }

        float angleOffset = Vector3.SignedAngle(carFwdFlat, camFwdFlat, Vector3.up) * Mathf.Deg2Rad;
        float angularVelAroundAxis = Vector3.Dot(_rigidbody.angularVelocity, Vector3.up);
        float turnForce = (angleOffset * turnStrength) - (angularVelAroundAxis * turnDamper);
        float turnForceMag = Mathf.Clamp(turnForce, -maxAlignmentTorque, maxAlignmentTorque);
        float uprightFactor = Mathf.Clamp01(Vector3.Dot(transform.forward, _cameraTransform.forward));

        _rigidbody.AddTorque(Vector3.up * (turnForceMag * uprightFactor), ForceMode.Acceleration);
    }

    private void HandleLateralSlip()
    {
        if (_cameraTransform == null) return;

        Vector3 steeringDir = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 carWorldVel = _rigidbody.linearVelocity;

        float steeringVel = Vector3.Dot(steeringDir, carWorldVel);
        float desiredAccel = -steeringVel / Time.fixedDeltaTime;
        float limitedSlipAccel = desiredAccel * sidewaysSlip;

        _rigidbody.AddForce(steeringDir * limitedSlipAccel, ForceMode.Acceleration);
    }
}