using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    [SerializeField] private TrailRenderer[] _wheelTrail;

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
    [SerializeField][Range(0f, 1f)] private float steeringDragMitigation = 0.5f;
    [SerializeField] private float maxCorrectiveAccel = 200f;

    [Header("Drift")]
    [SerializeField] private AnimationCurve frontWheelTraction;
    [SerializeField] private AnimationCurve rearWheelTraction;
    [SerializeField] private float maxLateralSlip = 10f; 
    [SerializeField] private float driftThreshold = 3f;

    [Header("Acceleration")]
    [SerializeField] private float maxSpeed = 45f;
    [SerializeField] private float acceleration = 1500f;
    [SerializeField] private float deceleration = 800f;
    [SerializeField] private AnimationCurve powerCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Inputs")]
    public Vector2 _moveInput;
    public bool IsDrifting { get; private set; }

    private Rigidbody _rigidBody;
    private RaceParticipant _raceParticipant;
    private GameObject _wheelTrailParent;

    private RaceManager _raceManager;

    private bool canDrive = false;

    private float _currentSteerAngle;
    private float[] _wheelSpinAngles = new float[4];

    private float[] _wheelTargetDistances = new float[4];
    private float[] _wheelCurrentDistances = new float[4];

    private float[] _wheelCompressions = new float[4];
    private bool[] _isGrounded = new bool[4];

    private Vector3 _spawnPos;
    private Quaternion _spawnRot;

    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _rigidBody.centerOfMass = centerOfMassOffset;
        _raceParticipant = GetComponent<RaceParticipant>();

        _raceManager = RaceManager.Instance;

        _wheelTrail = new TrailRenderer[wheelMesh.Length];
        _wheelTrailParent = new GameObject("WheelTrails");
        _wheelTrailParent.transform.parent = transform;

        _spawnPos = transform.position;
        _spawnRot = transform.rotation;

        for (int i =  0; i < wheelMesh.Length; i++)
        {
            _wheelTargetDistances[i] = restLength;
            _wheelCurrentDistances[i] = restLength;

            TrailRenderer trail = wheelMesh[i].GetComponentInChildren<TrailRenderer>();

            if (trail != null)
            {
                _wheelTrail[i] = trail;
                _wheelTrail[i].transform.SetParent(_wheelTrailParent.transform);
                _wheelTrail[i].emitting = false;
            }
        }

        if (_raceManager != null)
        {
            _raceManager.OnRaceStart += () => canDrive = true;
        }
        else
        {
            canDrive = true;
        }
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

        bool rearWheelDrifting = false;

        for (int i = 0; i < wheelPoints.Length; i++)
        {
            Transform wheel = wheelPoints[i];
            RaycastHit hit;

            float minLength = restLength - sprintTravel;
            float maxLength = restLength + sprintTravel;

            if (Physics.Raycast(wheel.position, -wheel.up, out hit, maxLength + wheelRadius, whatIsDrivable))
            {
                _isGrounded[i] = true;

                float currentSpringLength = hit.distance - wheelRadius;
                _wheelCompressions[i] = Mathf.Clamp01((restLength - currentSpringLength) / sprintTravel);

                _wheelTargetDistances[i] = Mathf.Clamp(currentSpringLength, minLength, maxLength);

                ApplySuspension(i, wheel, hit);
                ApplySteering(i, wheel, _wheelCompressions[i]);

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

                ApplyBraking(wheel);
            }
            else
            {
                _isGrounded[i] = false;
                _wheelCompressions[i] = 0f;

                _wheelTargetDistances[i] = maxLength;

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
            // Spin visual
            float wheelForwardSpeed = Vector3.Dot(_rigidBody.GetPointVelocity(wheelPoints[i].position), wheelPoints[i].forward);
            _wheelSpinAngles[i] += (wheelForwardSpeed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
            wheelMesh[i].rotation = wheelPoints[i].rotation * Quaternion.Euler(_wheelSpinAngles[i], 0f, 0f);
            _wheelCurrentDistances[i] = Mathf.Lerp(_wheelCurrentDistances[i], _wheelTargetDistances[i], Time.deltaTime * 20f);
            wheelMesh[i].position = wheelPoints[i].position - (wheelPoints[i].up * _wheelCurrentDistances[i]);

            _wheelTrail[i].emitting = IsDrifting;
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

    private void ApplySteering(int index, Transform wheel, float compression)
    {
        Vector3 steeringDir = wheel.right;
        Vector3 tireWorldVel = _rigidBody.GetPointVelocity(wheel.position);
        float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);

        float normalizedSlip = Mathf.Clamp01(Mathf.Abs(steeringVel) / maxLateralSlip);

        AnimationCurve activeTractionCurve = (index >= 2) ? rearWheelTraction : frontWheelTraction;

        float slipGrip = activeTractionCurve.Evaluate(normalizedSlip);

        float normalLoadFactor = Mathf.Clamp(compression, 0.2f, 1.5f);

        float finalGrip = slipGrip * normalLoadFactor;

        float forwardSurplus = Vector3.Dot(wheel.forward, tireWorldVel);
        if (forwardSurplus > 0.1f)
        {
            steeringVel *= (1f - steeringDragMitigation);
        }

        float desiredVelChange = -steeringVel * finalGrip;
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

    private void ApplyBraking(Transform wheel)
    {
        if (Mathf.Abs(_moveInput.y) > 0.01f) return;

        float wheelForwardSpeed = Vector3.Dot(wheel.forward, _rigidBody.GetPointVelocity(wheel.position));

        if (Mathf.Abs(wheelForwardSpeed) < 0.01f) return;

        float brakeForce = -Mathf.Sign(wheelForwardSpeed) * deceleration;
        float lowSpeedThreshold = 2.0f;
        if (Mathf.Abs(wheelForwardSpeed) < lowSpeedThreshold)
        {
            brakeForce = -wheelForwardSpeed * (deceleration / lowSpeedThreshold);
        }

        _rigidBody.AddForceAtPosition(wheel.forward * brakeForce, wheel.position);
    }

    #region Input Methods

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (canDrive)
        {
            _moveInput = context.ReadValue<Vector2>();
        }
        else
        {
            _moveInput = Vector2.zero;
        }
    }

    public void OnRestartInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion

    #region Public Methods

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        _rigidBody.linearVelocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;

        _rigidBody.position = position;
        _rigidBody.rotation = rotation;
        transform.position = position;
        transform.rotation = rotation;

        _currentSteerAngle = 0f;

        for (int i = 0; i < wheelPoints.Length; i++)
        {
            _wheelTargetDistances[i] = restLength;
            _wheelCurrentDistances[i] = restLength;

            if (wheelMesh[i] != null)
            {
                wheelMesh[i].position = wheelPoints[i].position - (wheelPoints[i].up * restLength);
            }
            if (_wheelTrail[i] != null)
            {
                _wheelTrail[i].Clear();
            }
        }
    }

    public void TeleportToLastCheckPoint()
    {
        _rigidBody.linearVelocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;

        Vector3 position = _spawnPos;
        Quaternion rotation = _spawnRot;

        if (_raceParticipant != null && _raceParticipant.CurrentCheckpoint != 0)
        {
            position = _raceParticipant.LastCheckpointPosition;
            rotation = _raceParticipant.LastCheckpointRotation;
        }

        _rigidBody.position = position;
        _rigidBody.rotation = rotation;
        transform.position = position;
        transform.rotation = rotation;

        _currentSteerAngle = 0f;

        for (int i = 0; i < wheelPoints.Length; i++)
        {
            _wheelTargetDistances[i] = restLength;
            _wheelCurrentDistances[i] = restLength;

            if (wheelMesh[i] != null)
            {
                wheelMesh[i].position = wheelPoints[i].position - (wheelPoints[i].up * restLength);
            }
            if (_wheelTrail[i] != null)
            {
                _wheelTrail[i].Clear();
            }
        }
    }

    #endregion
}