using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MM_CarController : MonoBehaviour
{
    [Header("Car Specs")]
    public float mass = 1500f;
    public float wheelRadius = 0.34f;
    public float cgHeight = 0.5f;
    public float wheelbase = 2.6f;
    public float distanceFrontToCG = 1.3f; // Distance from center of gravity to front axle
    public float distanceRearToCG = 1.3f;  // Distance from center of gravity to rear axle

    [Header("Engine & Transmission")]
    public AnimationCurve torqueCurve;     // Map RPM (X) to Torque in Nm (Y)
    public float gearRatio = 2.66f;        // 1st gear ratio for testing
    public float differentialRatio = 3.42f;
    public float transmissionEfficiency = 0.7f;
    public float maxBrakeForce = 8000f;

    [Header("Aerodynamics & Resistance")]
    public float frontalArea = 2.2f;
    public float dragCoefficient = 0.30f;
    public float airDensity = 1.29f;
    public float rollingResistanceCoeff = 12.8f;

    [Header("Tires & Steering")]
    public float tireFrictionMultiplier = 1.0f;
    public float corneringStiffnessFront = 50000f; // Force generated per radian of slip
    public float corneringStiffnessRear = 50000f;
    public float maxSteeringAngle = 30f;

    // Private State
    private Rigidbody rb;
    private float currentSpeed; // meters per second
    private float previousSpeed;
    private float acceleration;
    private float engineRPM;

    // Inputs
    private float throttleInput;
    private float brakeInput;
    private float steeringInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;

        // Setup a default Corvette LS1 style torque curve if none exists
        if (torqueCurve.keys.Length == 0)
        {
            torqueCurve = new AnimationCurve(
                new Keyframe(1000, 300),
                new Keyframe(4400, 475), // Peak torque
                new Keyframe(6000, 350)
            );
        }
    }

    void Update()
    {
        // 1. Gather Input
        throttleInput = Input.GetAxis("Vertical");
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        steeringInput = Input.GetAxis("Horizontal");

        // Clamp inputs
        throttleInput = Mathf.Clamp(throttleInput, 0f, 1f); // No reverse in this simple model
    }

    void FixedUpdate()
    {
        // Convert world velocity to local velocity (relative to the car's orientation)
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        currentSpeed = localVelocity.z;

        // Calculate acceleration for weight transfer (a = dv/dt)
        acceleration = (currentSpeed - previousSpeed) / Time.fixedDeltaTime;
        previousSpeed = currentSpeed;

        ApplyLongitudinalForces(localVelocity);
        ApplyLateralForces(localVelocity);
    }

    private void ApplyLongitudinalForces(Vector3 localVelocity)
    {
        // 1. ENGINE & TRACTION FORCE
        // wheel RPM = (speed / circumference) * 60 seconds
        float wheelRPM = (Mathf.Abs(currentSpeed) / (2f * Mathf.PI * wheelRadius)) * 60f;

        // Engine RPM = wheel RPM * gear ratios. Clamp to keep it in the curve bounds.
        engineRPM = Mathf.Clamp(wheelRPM * gearRatio * differentialRatio, 1000f, 6000f);

        float maxEngineTorque = torqueCurve.Evaluate(engineRPM);

        // F_drive = (u * T_engine * x_g * x_d * n) / R_w
        float driveForce = (throttleInput * maxEngineTorque * gearRatio * differentialRatio * transmissionEfficiency) / wheelRadius;

        // 2. AERODYNAMIC DRAG
        // F_drag = -0.5 * Cd * A * rho * v * |v|
        // We use currentSpeed * Mathf.Abs(currentSpeed) to maintain the correct negative/positive sign
        float dragForce = -0.5f * dragCoefficient * frontalArea * airDensity * (currentSpeed * Mathf.Abs(currentSpeed));

        // 3. ROLLING RESISTANCE
        // F_rr = -C_rr * v
        float rrForce = -rollingResistanceCoeff * currentSpeed;

        // 4. BRAKING
        float brakeForce = 0f;
        if (brakeInput > 0f)
        {
            brakeForce = -Mathf.Sign(currentSpeed) * maxBrakeForce;
            // Prevent brakes from pushing the car backward if it comes to a stop
            if (Mathf.Abs(currentSpeed) < 0.1f) brakeForce = 0f;
        }

        // 5. TOTAL LONGITUDINAL FORCE
        float totalLongForce = driveForce + dragForce + rrForce + brakeForce;

        // Apply straight forward from the center of gravity
        rb.AddForce(transform.forward * totalLongForce, ForceMode.Force);
    }

    private void ApplyLateralForces(Vector3 localVelocity)
    {
        // 1. DYNAMIC WEIGHT TRANSFER
        float weight = mass * 9.81f; // W = M * g

        // Static weight distribution based on distances to CG, plus dynamic shift based on acceleration
        float weightFront = (distanceRearToCG / wheelbase) * weight - (cgHeight / wheelbase) * mass * acceleration;
        float weightRear = (distanceFrontToCG / wheelbase) * weight + (cgHeight / wheelbase) * mass * acceleration;

        // 2. SLIP ANGLES (Simplified)
        // Angular velocity around the Y axis (Yaw rate)
        float yawRate = rb.angularVelocity.y;
        float steeringAngleRad = (steeringInput * maxSteeringAngle) * Mathf.Deg2Rad;

        // Protect against divide-by-zero at standstill
        float absSpeed = Mathf.Max(Mathf.Abs(currentSpeed), 1f);

        // Angle difference between where wheels are pointing vs where they are moving
        float slipAngleFront = Mathf.Atan2(localVelocity.x + yawRate * distanceFrontToCG, absSpeed) - steeringAngleRad;
        float slipAngleRear = Mathf.Atan2(localVelocity.x - yawRate * distanceRearToCG, absSpeed);

        // 3. LATERAL FORCES (Cornering stiffness * slip angle)
        float latForceFront = -corneringStiffnessFront * slipAngleFront;
        float latForceRear = -corneringStiffnessRear * slipAngleRear;

        // 4. FRICTION CIRCLE LIMIT (Traction limits based on normal load)
        float maxGripFront = tireFrictionMultiplier * weightFront;
        float maxGripRear = tireFrictionMultiplier * weightRear;

        latForceFront = Mathf.Clamp(latForceFront, -maxGripFront, maxGripFront);
        latForceRear = Mathf.Clamp(latForceRear, -maxGripRear, maxGripRear);

        // 5. APPLY FORCES AT AXLES
        // Apply lateral forces at the specific distances from the Center of Mass to induce yaw (rotation)
        Vector3 frontAxlePos = transform.position + transform.forward * distanceFrontToCG;
        Vector3 rearAxlePos = transform.position - transform.forward * distanceRearToCG;

        rb.AddForceAtPosition(transform.right * latForceFront, frontAxlePos, ForceMode.Force);
        rb.AddForceAtPosition(transform.right * latForceRear, rearAxlePos, ForceMode.Force);
    }
}