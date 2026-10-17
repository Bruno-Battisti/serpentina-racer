using Godot;

namespace SerpentinaRacer.Track;

public partial class Checkpoint : Area3D
{
	[Signal]
	public delegate void CarPassedEventHandler(int index);

	public int Index { get; set; }

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 0;
		SetCollisionMaskValue(2, true); // detect the car (layer 2)
		Monitoring = true;
		Monitorable = false;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		EmitSignal(SignalName.CarPassed, Index);
	}
}
