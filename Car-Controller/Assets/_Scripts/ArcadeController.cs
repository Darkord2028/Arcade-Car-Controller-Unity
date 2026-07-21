using UnityEngine;
using UnityEngine.InputSystem;

public enum DriveTrain
{
    Front, Rear, AllWheel
}

[RequireComponent(typeof(Rigidbody))]
public class ArcadeController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private float debug_currentSpeed;

    [Header("Wheels")]
    [SerializeField] private Transform[] wheelPoints;
    [SerializeField] private LayerMask whatIsDrivable;
    [SerializeField] private DriveTrain driveTrain = DriveTrain.Rear;

    [Header("Visuals")]
    [SerializeField] private Transform[] wheelMesh;

    [Header("Physics")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Suspension")]
    [SerializeField] private float springStiffness = 25000f;
    [SerializeField] private float damperStiffness = 2500f;
    [SerializeField] private float restLength = 0.3f;
    [SerializeField] private float sprintTravel = 0.15f;
    [SerializeField] private float wheelRadius = 0.3f;

    [Header("Anti-Roll Bars")]
    [SerializeField] private float antiRollStiffness = 5000f;

    [Header("Steering & Dynamics")]
    [SerializeField] private float tireMass = 300f;
    [SerializeField] private float maxSteerAngle = 35f;
    [SerializeField] private float highSpeedSteerLimit = 12f;
    [SerializeField] private float steerSpeed = 5f;
    [SerializeField] private AnimationCurve gripCurve = AnimationCurve.EaseInOut(0f, 0.95f, 1f, 0.5f);
    [SerializeField][Range(0f, 1f)] private float steeringDragMitigation = 0.5f;
    [SerializeField] private float maxCorrectiveAccel = 200f;

    [Header("Drift")]
    [SerializeField] private float driftGripFactor = 0.2f;
    [SerializeField] private float driftGripTransition = 8f;
    [SerializeField] private float driftThreshold = 3f;

    [Header("Acceleration")]
    [SerializeField] private float maxSpeed = 45f;
    [SerializeField] private float acceleration = 1500f;
    [SerializeField] private float deceleration = 800f;
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Inputs")]
    [SerializeField] Vector2 _moveInput;
    [SerializeField] bool _driftInput;
    public bool IsDrifting { get; private set; }

    private Rigidbody _rigidBody;

    private float _currentSteerAngle;
    private float[] _wheelSpinAngles = new float[4];
    private Vector3[] _wheelTargetPositions = new Vector3[4];
    private float _currentRearGrip;

    private float[] _wheelCompressions = new float[4];
    private bool[] _isGrounded = new bool[4];

    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _currentRearGrip = 1f;
        _rigidBody.centerOfMass = centerOfMassOffset;
    }

    void FixedUpdate()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float currentSpeed = Vector3.Dot(_rigidBody.linearVelocity, flatForward);
        debug_currentSpeed = currentSpeed * 3.6f;

        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float dynamicMaxSteer = Mathf.Lerp(maxSteerAngle, highSpeedSteerLimit, speedFactor);

        float targetSteerAngle = _moveInput.x * dynamicMaxSteer;
        _currentSteerAngle = Mathf.MoveTowards(_currentSteerAngle, targetSteerAngle, steerSpeed * maxSteerAngle * Time.fixedDeltaTime);

        wheelPoints[0].localRotation = Quaternion.Euler(0f, _currentSteerAngle, 0f);
        wheelPoints[1].localRotation = Quaternion.Euler(0f, _currentSteerAngle, 0f);

        float targetRearGrip = _driftInput ? driftGripFactor : 1f;
        _currentRearGrip = Mathf.MoveTowards(_currentRearGrip, targetRearGrip, driftGripTransition * Time.fixedDeltaTime);

        bool rearWheelDrifting = false;

        for (int i = 0; i < wheelPoints.Length; i++)
        {
            Transform wheel = wheelPoints[i];
            RaycastHit hit;
            float maxLength = restLength + sprintTravel;

            if (Physics.Raycast(wheel.position, -wheel.up, out hit, maxLength + wheelRadius, whatIsDrivable))
            {
                _isGrounded[i] = true;

                float currentSpringLength = hit.distance - wheelRadius;
                _wheelCompressions[i] = Mathf.Clamp01((restLength - currentSpringLength) / sprintTravel);

                _wheelTargetPositions[i] = hit.point + (wheel.up * wheelRadius);

                float gripOverride = (i >= 2) ? _currentRearGrip : 1f;

                ApplySuspension(i, wheel, hit);
                ApplySteering(wheel, _wheelCompressions[i], gripOverride, currentSpeed);

                if (i == 2)
                {
                    float rearLateralSlip = Mathf.Abs(Vector3.Dot(wheel.right, _rigidBody.GetPointVelocity(wheel.position)));
                    rearWheelDrifting = rearLateralSlip > driftThreshold;
                }

                bool isDrivenWheel =
                    driveTrain == DriveTrain.AllWheel ||
                    (driveTrain == DriveTrain.Front && i < 2) ||
                    (driveTrain == DriveTrain.Rear && i >= 2);

                if (isDrivenWheel)
                {
                    ApplyThrottle(wheel, currentSpeed);
                }

                ApplyBraking(wheel, currentSpeed);
            }
            else
            {
                _isGrounded[i] = false;
                _wheelCompressions[i] = 0f;

                _wheelTargetPositions[i] = wheel.position - (wheel.up * maxLength);

                Debug.DrawLine(wheel.position, wheel.position + (wheelRadius + maxLength) * -wheel.up, Color.red);
            }
        }

        IsDrifting = _isGrounded[2] && rearWheelDrifting;

        ApplyAntiRollBar(0, 1);
        ApplyAntiRollBar(2, 3);
    }

    void LateUpdate()
    {
        for (int i = 0; i < wheelMesh.Length; i++)
        {
            float wheelForwardSpeed = Vector3.Dot(_rigidBody.GetPointVelocity(wheelPoints[i].position), wheelPoints[i].forward);
            _wheelSpinAngles[i] += (wheelForwardSpeed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
            wheelMesh[i].rotation = wheelPoints[i].rotation * Quaternion.Euler(_wheelSpinAngles[i], 0f, 0f);
            wheelMesh[i].position = Vector3.Lerp(wheelMesh[i].position, _wheelTargetPositions[i], Time.deltaTime * 20f);
        }
    }

    private void ApplySuspension(int index, Transform wheel, RaycastHit hit)
    {
        Vector3 wheelVelocity = _rigidBody.GetPointVelocity(wheel.position);
        float springVelocity = Vector3.Dot(wheelVelocity, wheel.up);

        float damperForce = springVelocity * damperStiffness;
        float springForce = _wheelCompressions[index] * springStiffness;
        float netForce = springForce - damperForce;

        _rigidBody.AddForceAtPosition(netForce * wheel.up, wheel.position);
        Debug.DrawLine(wheel.position, hit.point, Color.green);
    }

    private void ApplyAntiRollBar(int leftIndex, int rightIndex)
    {
        float travelLeft = _wheelCompressions[leftIndex];
        float travelRight = _wheelCompressions[rightIndex];

        float antiRollForce = (travelLeft - travelRight) * antiRollStiffness;

        if (_isGrounded[leftIndex])
            _rigidBody.AddForceAtPosition(wheelPoints[leftIndex].up * -antiRollForce, wheelPoints[leftIndex].position);

        if (_isGrounded[rightIndex])
            _rigidBody.AddForceAtPosition(wheelPoints[rightIndex].up * antiRollForce, wheelPoints[rightIndex].position);
    }

    private void ApplySteering(Transform wheel, float compression, float gripOverride, float currentSpeed)
    {
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);

        float normalLoadFactor = Mathf.Clamp(compression, 0.2f, 1.5f);
        float gripFactor = gripCurve.Evaluate(normalizedSpeed) * normalLoadFactor * gripOverride;

        Vector3 steeringDir = wheel.right;
        Vector3 tireWorldVel = _rigidBody.GetPointVelocity(wheel.position);
        float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);

        float forwardSurplus = Vector3.Dot(wheel.forward, tireWorldVel);
        if (forwardSurplus > 0.1f)
        {
            steeringVel *= (1f - steeringDragMitigation);
        }

        float desiredVelChange = -steeringVel * gripFactor;
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

        desiredAccel = Mathf.Clamp(desiredAccel, -maxCorrectiveAccel, maxCorrectiveAccel);

        _rigidBody.AddForceAtPosition(steeringDir * tireMass * desiredAccel, wheel.position);
    }

    private void ApplyThrottle(Transform wheel, float currentSpeed)
    {
        if (Mathf.Abs(_moveInput.y) < 0.01f) return;

        Vector3 accelDir = wheel.forward;
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float availableTorque = powerCurve.Evaluate(normalizedSpeed) * _moveInput.y * acceleration;

        _rigidBody.AddForceAtPosition(accelDir * availableTorque, wheel.position);
    }

    private void ApplyBraking(Transform wheel, float currentSpeed)
    {
        if (Mathf.Abs(_moveInput.y) > 0.01f) return;
        if (Mathf.Abs(currentSpeed) <= 0.5f) return;

        Vector3 accelDir = wheel.forward;
        float brakeForce = -Mathf.Sign(currentSpeed) * deceleration;
        _rigidBody.AddForceAtPosition(accelDir * brakeForce, wheel.position);
    }

    #region Input Actions

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnDriftInput(InputAction.CallbackContext context)
    {
        _driftInput = context.ReadValueAsButton();
    }

    #endregion
}