using Godot;

namespace SerpentinaRacer.Race;

/// <summary>Smooth third-person chase camera. Call Init() once the target exists
/// so the first frame doesn't fly in from the origin.</summary>
public partial class CameraRig : Camera3D
{
	[Export] public float Distance = 7.5f;
	[Export] public float Height = 3.1f;
	[Export] public float FollowSharpness = 5.5f;
	[Export] public float LookSharpness = 7f;
	[Export] public float LookAheadDistance = 5f;

	private Node3D _target;
	private Vector3 _currentLook;

	public void Init(Node3D target)
	{
		_target = target;
		Vector3 behind = target.GlobalBasis.Z.Normalized();
		GlobalPosition = target.GlobalPosition + behind * Distance + Vector3.Up * Height;
		_currentLook = target.GlobalPosition + Vector3.Up * 1f;
		LookAtFromPosition(GlobalPosition, _currentLook, Vector3.Up);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_target == null)
			return;

		float dt = (float)delta;
		Vector3 behind = _target.GlobalBasis.Z.Normalized();
		Vector3 desiredPos = _target.GlobalPosition + behind * Distance + Vector3.Up * Height;

		float followT = 1f - Mathf.Exp(-FollowSharpness * dt);
		GlobalPosition = GlobalPosition.Lerp(desiredPos, followT);

		Vector3 forward = -_target.GlobalBasis.Z.Normalized();
		Vector3 lookTarget = _target.GlobalPosition + Vector3.Up * 1.1f + forward * LookAheadDistance;
		float lookT = 1f - Mathf.Exp(-LookSharpness * dt);
		_currentLook = _currentLook.Lerp(lookTarget, lookT);

		LookAtFromPosition(GlobalPosition, _currentLook, Vector3.Up);
	}
}
