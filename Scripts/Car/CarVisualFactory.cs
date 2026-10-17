using Godot;

namespace SerpentinaRacer.Car;

/// <summary>
/// Builds the player car's visible geometry entirely from primitive meshes
/// assembled by code - no imported model files. Forward is -Z (Godot convention).
/// </summary>
public static class CarVisualFactory
{
	public static StandardMaterial3D MakeMaterial(Color color, float roughness = 0.4f, float metallic = 0.15f)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = roughness,
			Metallic = metallic
		};
	}

	public static StandardMaterial3D MakeEmissive(Color color, float energy = 1.5f)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = energy,
			Roughness = 0.3f
		};
	}

	private static MeshInstance3D AddBox(Node3D parent, Vector3 size, Vector3 position, Material mat, Vector3? rotationDeg = null)
	{
		var mesh = new BoxMesh { Size = size };
		var mi = new MeshInstance3D { Mesh = mesh, MaterialOverride = mat };
		mi.Position = position;
		if (rotationDeg.HasValue)
			mi.RotationDegrees = rotationDeg.Value;
		parent.AddChild(mi);
		return mi;
	}

	private static MeshInstance3D AddPrism(Node3D parent, Vector3 size, Vector3 position, Material mat, Vector3? rotationDeg = null, float leftToRight = 0f)
	{
		var mesh = new PrismMesh { Size = size, LeftToRight = leftToRight };
		var mi = new MeshInstance3D { Mesh = mesh, MaterialOverride = mat };
		mi.Position = position;
		if (rotationDeg.HasValue)
			mi.RotationDegrees = rotationDeg.Value;
		parent.AddChild(mi);
		return mi;
	}

	/// <summary>Builds the chassis/cabin/lights/bumpers under the given root and returns body bounds info.</summary>
	public static void BuildBody(Node3D root, Color bodyColor, Color accentColor)
	{
		var bodyMat = MakeMaterial(bodyColor, 0.35f, 0.25f);
		var darkMat = MakeMaterial(new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.0f);
		var glassMat = MakeMaterial(new Color(0.15f, 0.22f, 0.28f), 0.1f, 0.0f);
		glassMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		glassMat.AlbedoColor = new Color(0.15f, 0.22f, 0.28f, 0.75f);
		var headlightMat = MakeEmissive(new Color(1f, 0.97f, 0.85f), 2.0f);
		var taillightMat = MakeEmissive(new Color(0.9f, 0.08f, 0.05f), 1.6f);
		var accentMat = MakeMaterial(accentColor, 0.4f, 0.1f);

		// Lower chassis / floor pan.
		AddBox(root, new Vector3(1.78f, 0.28f, 3.9f), new Vector3(0, 0.38f, 0f), bodyMat);

		// Main cabin block (greenhouse base).
		AddBox(root, new Vector3(1.68f, 0.5f, 2.1f), new Vector3(0, 0.78f, 0.15f), bodyMat);

		// Hood (slanted prism toward the front, car faces -Z).
		AddPrism(root, new Vector3(1.6f, 0.34f, 1.3f), new Vector3(0, 0.62f, -1.55f), bodyMat,
			rotationDeg: new Vector3(0, 180, 0), leftToRight: 0.08f);

		// Windshield (slanted prism).
		AddPrism(root, new Vector3(1.5f, 0.55f, 0.9f), new Vector3(0, 1.1f, -0.55f), glassMat,
			rotationDeg: new Vector3(0, 180, 0), leftToRight: 0.1f);

		// Roof.
		AddBox(root, new Vector3(1.42f, 0.08f, 1.15f), new Vector3(0, 1.4f, 0.1f), bodyMat);

		// Rear windshield (slanted the other way).
		AddPrism(root, new Vector3(1.48f, 0.48f, 0.75f), new Vector3(0, 1.08f, 0.85f), glassMat,
			rotationDeg: new Vector3(0, 0, 0), leftToRight: 0.1f);

		// Trunk / rear deck.
		AddBox(root, new Vector3(1.7f, 0.3f, 0.9f), new Vector3(0, 0.68f, 1.55f), bodyMat);

		// Front bumper.
		AddBox(root, new Vector3(1.82f, 0.32f, 0.3f), new Vector3(0, 0.34f, -2.0f), accentMat);
		// Rear bumper.
		AddBox(root, new Vector3(1.82f, 0.32f, 0.3f), new Vector3(0, 0.34f, 2.0f), accentMat);

		// Side skirts.
		AddBox(root, new Vector3(0.08f, 0.18f, 3.2f), new Vector3(-0.92f, 0.32f, 0f), darkMat);
		AddBox(root, new Vector3(0.08f, 0.18f, 3.2f), new Vector3(0.92f, 0.32f, 0f), darkMat);

		// Headlights.
		AddBox(root, new Vector3(0.28f, 0.16f, 0.1f), new Vector3(-0.68f, 0.5f, -2.02f), headlightMat);
		AddBox(root, new Vector3(0.28f, 0.16f, 0.1f), new Vector3(0.68f, 0.5f, -2.02f), headlightMat);

		// Taillights.
		AddBox(root, new Vector3(0.3f, 0.18f, 0.08f), new Vector3(-0.72f, 0.62f, 2.02f), taillightMat);
		AddBox(root, new Vector3(0.3f, 0.18f, 0.08f), new Vector3(0.72f, 0.62f, 2.02f), taillightMat);

		// Side mirrors.
		AddBox(root, new Vector3(0.1f, 0.1f, 0.18f), new Vector3(-0.92f, 1.0f, -0.5f), darkMat);
		AddBox(root, new Vector3(0.1f, 0.1f, 0.18f), new Vector3(0.92f, 1.0f, -0.5f), darkMat);

		// Rear spoiler.
		AddBox(root, new Vector3(0.06f, 0.3f, 0.15f), new Vector3(-0.7f, 0.95f, 2.05f), darkMat);
		AddBox(root, new Vector3(0.06f, 0.3f, 0.15f), new Vector3(0.7f, 0.95f, 2.05f), darkMat);
		AddBox(root, new Vector3(1.5f, 0.06f, 0.4f), new Vector3(0, 1.15f, 2.05f), accentMat);

		// Front grille.
		AddBox(root, new Vector3(1.0f, 0.18f, 0.06f), new Vector3(0, 0.42f, -2.12f), darkMat);
	}

	/// <summary>
	/// Builds a free-standing wheel visual (tire + rim) under a "Spin" node that the
	/// controller rotates by hand each frame. It is NOT parented to the physics
	/// VehicleWheel3D node - the controller copies that node's world transform onto
	/// this visual every frame instead, which keeps the look fully predictable.
	/// </summary>
	public static Node3D BuildWheelVisual(Node3D parent, float radius, float width, out Node3D spin)
	{
		var root = new Node3D();
		parent.AddChild(root);

		var tireMat = MakeMaterial(new Color(0.04f, 0.04f, 0.045f), 0.85f, 0.0f);
		var rimMat = MakeMaterial(new Color(0.72f, 0.73f, 0.75f), 0.3f, 0.75f);

		spin = new Node3D { Name = "Spin" };
		root.AddChild(spin);

		var tire = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = width, RadialSegments = 14 },
			MaterialOverride = tireMat,
			RotationDegrees = new Vector3(0, 0, 90)
		};
		spin.AddChild(tire);

		var rim = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = radius * 0.55f, BottomRadius = radius * 0.55f, Height = width * 1.05f, RadialSegments = 10 },
			MaterialOverride = rimMat,
			RotationDegrees = new Vector3(0, 0, 90)
		};
		spin.AddChild(rim);

		return root;
	}
}
