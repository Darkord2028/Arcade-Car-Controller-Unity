# Raycast Vehicle Physics Controller for Unity

## Overview

I originally started this project with a big scope in mind: building a chaotic, top-down racing and drifting game to play with my brothers. While a full game needs a lot more polish, the core of that idea lives on in this arcade-style vehicle controller—designed for simple controls, responsive handling, and chaotic gameplay. 

Inspired by the physics style of *Very Very Valet* by Toyful Games ([Watch Gameplay](https://www.youtube.com/watch?v=CdPYlj5uZeI)).

### What's Included
* **Raycast Suspension:** Four corner raycasts simulate spring and damper forces to keep the chassis stable over bumps, slopes, and jumps.
* **Friction & Grip Control:** Custom lateral friction cancellation handles cornering and drifting, while controlled longitudinal forces manage acceleration and braking without unwanted drag.
* **Configurable Drive-Trains:** Easily switch between **All-Wheel Drive (AWD)**, **Front-Wheel Drive (FWD)**, and **Rear-Wheel Drive (RWD)** in the inspector to change how the car handles and drifts.

https://github.com/user-attachments/assets/d1ad8c1a-314f-4b6d-a4a8-f6650c117fd7

---

## How It Works: The 4-Raycast Setup

Instead of simulating physical wheels that collide with the terrain, there is a cube with `Rigidbody` attached to the car. Every physics frame, the script fires four downward raycasts from the corners of the chassis to detect the road surface.

* **Single Rigid Body:** All forces (engine throttle, braking, suspension pushback, and steering grip) are applied to one central `Rigidbody`.
* **Virtual Contact Patches:** Wherever a raycast hits a layer assigned to `whatIsDrivable`, that hit point becomes the tire's contact patch on the ground.
* **Airborne Detection:** If a raycast hits nothing, that specific wheel is marked as airborne, its spring compression drops to zero, and the controller stops applying ground forces for that corner.

---

## Suspension & Anti-Roll Bars

### Suspension (Hooke's Law)

Each wheel acts like an independent spring and shock absorber. The script measures how far the spring is compressed between its normal resting length and the ground, then pushes upward.

```csharp
private void ApplySuspension(int index, Transform wheel, RaycastHit hit)
{
    Vector3 wheelVelocity = _rigidBody.GetPointVelocity(wheel.position);
    float springVelocity = Vector3.Dot(wheelVelocity, wheel.up);

    float damperForce = springVelocity * damperStiffness;
    float springForce = _wheelCompressions[index] * springStiffness;
    float netForce = springForce - damperForce;

    _rigidBody.AddForceAtPosition(netForce * wheel.up, wheel.position);
}

```

* **Spring Force:** Pushes the chassis up based on how hard the suspension is currently compressed (`_wheelCompressions[index] * springStiffness`).
* **Damper Force:** Measures how fast the wheel is compressing or rebounding (`springVelocity`) and resists that movement so the car doesn't bounce like a pogo stick.
* **Bumps & Rough Terrain:** When one wheel hits a speed bump or curb, only that raycast shortens. The individual spring absorbs the impact without throwing the entire car body into the air.

https://github.com/user-attachments/assets/e8874b6b-6248-4511-9693-d6427b61e8b9

## Sideways Grip & Steering

### Sideways Friction & Forward Drag Mitigation

To stop the car from sliding sideways like a hockey puck, the script checks how fast each tire is moving along its own horizontal right axis and applies a counter-force.

```csharp
Vector3 steeringDir = wheel.right;
Vector3 tireWorldVel = _rigidBody.GetPointVelocity(wheel.position);
float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);

float forwardSurplus = Vector3.Dot(wheel.forward, tireWorldVel);
if (forwardSurplus > 0.1f)
{
    steeringVel *= (1f - steeringDragMitigation);
}

```

* **Isolating Slip:** `Vector3.Dot(steeringDir, tireWorldVel)` measures exactly how fast the tire is sliding sideways across the asphalt.
* **Drag Mitigation:** In simple arcade physics, cancelling sideways speed during a turn acts like a brake and kills your forward momentum. The `steeringDragMitigation` variable tapers off that resistance so you retain speed through corners.
* **Load-Based Grip:** Grip is multiplied by the current spring compression (`normalLoadFactor`), meaning a tire pressed hard into the ground gets more cornering traction.

https://github.com/user-attachments/assets/6ab561c4-4a4e-4efc-8344-3bdc2172cdfb

### Speed-Sensitive Steering

Turning the front wheels 35 degrees at low speeds is necessary for sharp turns, but doing that at 150 km/h will cause an instant spin-out.

```csharp
float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
float dynamicMaxSteer = Mathf.Lerp(maxSteerAngle, highSpeedSteerLimit, speedFactor);
float targetSteerAngle = _moveInput.x * dynamicMaxSteer;

```

* **Dynamic Limits:** As the car accelerates toward `maxSpeed`, the maximum allowed steering lock smoothly shrinks from `maxSteerAngle` (35 degree) down to `highSpeedSteerLimit` (12 degree).

---

## Drifting & Skid Marks

Drifting works by temporarily dropping the grip multiplier on the rear axle while keeping the front tires glued to the road.

```csharp
// Smoothly transition rear grip when drift input is held
float targetRearGrip = _driftInput ? driftGripFactor : 1f;
_currentRearGrip = Mathf.MoveTowards(_currentRearGrip, targetRearGrip, driftGripTransition * Time.fixedDeltaTime);

// Verify the rear wheels are actually sliding sideways
float rearLateralSlip = Mathf.Abs(Vector3.Dot(wheel.right, _rigidBody.GetPointVelocity(wheel.position)));
bool rearWheelDrifting = rearLateralSlip > driftThreshold;

IsDrifting = _isGrounded[2] && rearWheelDrifting;

```

* **Smooth Transition:** `Mathf.MoveTowards` prevents the rear tires from instantly snapping between normal grip and drift grip, giving you a smooth entry into the slide.
* **Real Drift Verification:** Simply holding the drift button doesn't trigger skid marks. The script checks `rearLateralSlip > driftThreshold` to prove the back of the car is actually sliding sideways.
* **Trail Renderers:** The `IsDrifting` boolean directly toggles the `emitting` state of the wheel trail renderers so tire marks only appear during an active skid.

https://github.com/user-attachments/assets/cb626ccf-bb6a-47dc-b031-d4369742f110

---

## Terrain Response: Slopes & Air Time

### Driving on Hills & Slopes

To prevent the speedometer and engine throttle from breaking when driving up inclines, the controller projects the car's orientation onto a flat ground plane.

```csharp
Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
float currentSpeed = Vector3.Dot(_rigidBody.linearVelocity, flatForward);

```

* **Accurate Speed:** Ignoring vertical fall speed ensures the engine delivers consistent torque whether you are driving on a flat drag strip or climbing a steep hill.
* **Natural Hold:** When resting on a slope, gravity compresses the downhill springs more than the uphill ones. That extra compression increases normal load grip, preventing the car from slowly sliding backward down the hill.

https://github.com/user-attachments/assets/30e06963-6f54-41e3-8279-363170378df2

### Jumps & Airborne State

When the car launches off a ramp, the controller disables ground physics and extends the wheels.

* **Zeroing Compression:** Airborne wheels stop pushing against the chassis immediately.
* **Visual Wheel Drop:** The visual target positions drop down to their maximum stretch limit (`restLength + sprintTravel`), letting the tires hang down out of the wheel wells while in the air.
* **Landing Impact:** The moment raycasts touch the ground again, the springs compress and absorb the landing shock without clipping through the floor.

https://github.com/user-attachments/assets/eec5601e-6fc3-442c-816f-1089a4ca5179

---

## Visual Integration (Connecting the Meshes)

To keep physics calculations deterministic, all raycast and force math runs inside `FixedUpdate()`. Cosmetic wheel movement is handled separately inside `LateUpdate()` so visual rendering stays smooth at high framerates.

```csharp
void LateUpdate()
{
    for (int i = 0; i < wheelMesh.Length; i++)
    {
        // 1. Roll the wheel based on forward speed
        float wheelForwardSpeed = Vector3.Dot(_rigidBody.GetPointVelocity(wheelPoints[i].position), wheelPoints[i].forward);
        _wheelSpinAngles[i] += (wheelForwardSpeed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
        wheelMesh[i].rotation = wheelPoints[i].rotation * Quaternion.Euler(_wheelSpinAngles[i], 0f, 0f);

        // 2. Smoothly move the mesh up and down to match suspension travel
        wheelMesh[i].position = Vector3.Lerp(wheelMesh[i].position, _wheelTargetPositions[i], Time.deltaTime * 20f);

        // 3. Trigger skid marks
        _wheelTrail[i].emitting = IsDrifting;
    }
}

```

* **Rolling Rotation:** Calculates exact forward speed at each wheel corner and converts it to degrees (`_wheelSpinAngles`), rolling the tire mesh at a speed that matches the ground.
* **Suspension Travel:** Uses `Vector3.Lerp` to slide the 3D wheel model up and down to match where the raycast hit the floor (`_wheelTargetPositions`), creating visual suspension bounce.
* **Steering Sync:** Copies local rotation directly from `wheelPoints` so the front wheel meshes turn left and right with your steering input.

---

## Inspector Parameter Reference

| Parameter | Default Value | Description |
| --- | --- | --- |
| **Center of Mass Offset** | `(0, -0.5, 0)` | Lowers the physical balance point below the chassis to prevent tipping over in turns. |
| **Spring Stiffness** | `25000` | How hard the suspension pushes upward per meter of compression. |
| **Damper Stiffness** | `2500` | Shock absorber resistance that stops the car from bouncing repeatedly. |
| **Rest Length / Travel** | `0.3 / 0.15` | Resting suspension height and maximum allowable upward/downward stretch. |
| **Anti-Roll Stiffness** | `5000` | Force applied across left/right wheels to eliminate body roll during cornering. |
| **Steering Drag Mitigation** | `0.5` | How much sideways turning drag is ignored (`0` = full drag, `1` = zero drag). |
| **Drift Grip Factor** | `0.2` | Traction multiplier applied to rear wheels during a drift (`0.2` = 20% grip). |
| **Max Corrective Accel** | `200` | Clamps maximum sideways counter-force to prevent physics jitter on high-grip surfaces. |

---
