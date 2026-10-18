using Godot;
using SerpentinaRacer.Audio;

namespace SerpentinaRacer.Car;

/// <summary>
/// Arcade-tuned vehicle controller. Physics runs on the four VehicleWheel3D
/// raycast wheels; wheel meshes are separate visual nodes that the controller
/// positions by hand every frame (world position/orientation copied from the
/// physics wheel, plus a hand-rolled steering and spin animation) so the look
/// never depends on undocumented engine behaviour.
/// </summary>
public partial class CarController : VehicleBody3D
{
	[Export] public float EngineMaxForce = 900f;
	[Export] public float ReverseMaxForce = 450f;
	[Export] public float MaxSteerAngle = 0.5f;
	[Export] public float BrakeForce = 55f;
	[Export] public float HandbrakeForce = 140f;
	[Export] public float WheelRadius = 0.33f;

	public bool ControlsEnabled = true;

	private VehicleWheel3D[] _wheels = new VehicleWheel3D[4];
	private Node3D[] _wheelVisuals = new Node3D[4];
	private Node3D[] _wheelSpins = new Node3D[4];
	private bool[] _isSteeringWheel = new bool[4];
	private float _rollAngle0, _rollAngle1, _rollAngle2, _rollAngle3;
	private float _currentSteer;
	private double _crashCooldown;
	private Transform3D _spawnTransform;

	public float SpeedKmh => LinearVelocity.Length() * 3.6f;
	public float ForwardSpeedMs => LinearVelocity.Dot(-GlobalBasis.Z);

	public override void _Ready()
	{
		Mass = 1150f;
		CenterOfMassMode = CenterOfMassModeEnum.Custom;
		CenterOfMass = new Vector3(0f, -0.45f, 0f);
		ContactMonitor = true;
		MaxContactsReported = 6;
		BodyEntered += OnBodyEntered;
		// The road/terrain are thin concave trimeshes; without continuous collision
		// detection a fast-falling or bouncing car can tunnel straight through them.
		ContinuousCd = true;

		CollisionLayer = 0;
		CollisionMask = 0;
		SetCollisionLayerValue(2, true); // "Car" layer, so triggers/other bodies can detect it
		SetCollisionMaskValue(1, true); // collide with "World" static geometry

		BuildChassisCollision();
		BuildVisualBody();
		BuildWheels();

		_spawnTransform = GlobalTransform;
	}

	private void BuildChassisCollision()
	{
		var shape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(1.75f, 1.1f, 3.9f) },
			Position = new Vector3(0, 0.75f, 0f)
		};
		AddChild(shape);
	}

	private void BuildVisualBody()
	{
		var visualRoot = new Node3D { Name = "Body" };
		AddChild(visualRoot);
		CarVisualFactory.BuildBody(visualRoot, new Color(0.78f, 0.12f, 0.12f), new Color(0.08f, 0.08f, 0.09f));
	}

	private void BuildWheels()
	{
		float x = 0.86f;
		float frontZ = -1.25f;
		float rearZ = 1.25f;
		float attachY = 0.45f;

		AddWheel(0, new Vector3(-x, attachY, frontZ), true, true);
		AddWheel(1, new Vector3(x, attachY, frontZ), true, true);
		AddWheel(2, new Vector3(-x, attachY, rearZ), false, true);
		AddWheel(3, new Vector3(x, attachY, rearZ), false, true);
	}

	private void AddWheel(int index, Vector3 localPos, bool steering, bool traction)
	{
		var wheel = new VehicleWheel3D
		{
			Position = localPos,
			WheelRadius = WheelRadius,
			WheelRestLength = 0.4f,
			SuspensionStiffness = 65f,
			SuspensionTravel = 0.26f,
			DampingCompression = 0.5f,
			DampingRelaxation = 0.6f,
			WheelFrictionSlip = 6.5f,
			UseAsSteering = steering,
			UseAsTraction = traction,
			WheelRollInfluence = 0.15f
		};
		AddChild(wheel);
		_wheels[index] = wheel;
		_isSteeringWheel[index] = steering;

		var visuals = new Node3D { Name = $"WheelVisual{index}" };
		AddChild(visuals);
		CarVisualFactory.BuildWheelVisual(visuals, WheelRadius, 0.26f, out var spin);
		_wheelVisuals[index] = visuals;
		_wheelSpins[index] = spin;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		float throttleInput = 0f;
		float steerInput = 0f;
		bool handbrake = false;

		if (ControlsEnabled)
		{
			// Godot's VehicleBody3D applies positive engine force along +Z, opposite of the
			// visual "forward" (-Z) the car model and track orientation use, so the mapping
			// is intentionally swapped here to make W/up actually drive forward.
			throttleInput = Input.GetActionStrength("move_backward") - Input.GetActionStrength("move_forward");
			steerInput = Input.GetActionStrength("turn_left") - Input.GetActionStrength("turn_right");
			handbrake = Input.IsActionPressed("handbrake");

			if (Input.IsActionJustPressed("reset_car"))
				ResetToLastSpawn();
		}

		float speedMs = ForwardSpeedMs;
		float speedKmh = SpeedKmh;

		// Steering eases toward the target and loses range at speed for stability.
		float speedFactor = Mathf.Clamp(1f - speedKmh / 220f, 0.45f, 1f);
		float targetSteer = steerInput * MaxSteerAngle * speedFactor;
		_currentSteer = Mathf.Lerp(_currentSteer, targetSteer, dt * 6f);
		Steering = _currentSteer;

		if (throttleInput > 0.01f)
		{
			EngineForce = throttleInput * EngineMaxForce;
		}
		else if (throttleInput < -0.01f)
		{
			// Reverse only once nearly stopped; otherwise brake like a real car.
			if (speedMs > 1.5f)
			{
				EngineForce = 0f;
				Brake = BrakeForce;
			}
			else
			{
				EngineForce = throttleInput * ReverseMaxForce;
				Brake = 0f;
			}
		}
		else
		{
			EngineForce = 0f;
			Brake = BrakeForce * 0.08f;
		}

		if (handbrake)
		{
			Brake = HandbrakeForce;
			foreach (var w in _wheels)
				w.WheelFrictionSlip = 1.6f;
		}
		else
		{
			foreach (var w in _wheels)
				w.WheelFrictionSlip = 6.5f;
		}

		UpdateWheelVisuals(dt, speedMs);

		float throttle01 = Mathf.Abs(throttleInput);
		float speedRatio = Mathf.Clamp(speedKmh / 160f, 0f, 1f);
		AudioManager.Instance?.UpdateEngine(throttle01, speedRatio);

		if (_crashCooldown > 0)
			_crashCooldown -= delta;
	}

	private void UpdateWheelVisuals(float dt, float forwardSpeed)
	{
		float spinDelta = forwardSpeed / WheelRadius * dt;

		for (int i = 0; i < 4; i++)
		{
			var wheelNode = _wheels[i];
			var visual = _wheelVisuals[i];

			visual.GlobalPosition = wheelNode.GlobalPosition;
			visual.GlobalBasis = GlobalBasis;

			if (_isSteeringWheel[i])
				visual.RotateObjectLocal(Vector3.Up, _currentSteer);

			_wheelSpins[i].RotateObjectLocal(Vector3.Right, spinDelta);
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (_crashCooldown > 0)
			return;

		if (SpeedKmh > 12f)
		{
			AudioManager.Instance?.PlayCrash();
			_crashCooldown = 0.6;
		}
	}

	public void SetSpawn(Transform3D transform)
	{
		_spawnTransform = transform;
	}

	public void ResetToLastSpawn()
	{
		PhysicsServer3D.BodySetState(GetRid(), PhysicsServer3D.BodyState.Transform, _spawnTransform);
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		GlobalTransform = _spawnTransform;
	}

	public void HardReset(Transform3D transform)
	{
		SetSpawn(transform);
		ResetToLastSpawn();
	}
}
