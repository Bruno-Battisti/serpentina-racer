using System.Collections.Generic;
using Godot;
using SerpentinaRacer.Core;

namespace SerpentinaRacer.Track;

/// <summary>
/// Builds a closed, winding mountain-road circuit entirely from code: a smooth
/// center-line (Catmull-Rom through procedurally placed control points), the
/// drivable road mesh + collision, lane markings, guardrails, a start/finish
/// gate and a ring of lap checkpoints.
/// </summary>
public partial class TrackBuilder : Node3D
{
	[Export] public float RoadWidth = 10f;
	[Export] public int ControlPointCount = 14;
	[Export] public int SamplesPerSegment = 22;
	[Export] public int CheckpointCount = 8;

	public List<Vector3> PathPoints { get; private set; }
	public List<Vector3> Forwards { get; private set; }
	public List<Vector3> Rights { get; private set; }
	public List<float> CumulativeDistance { get; private set; }
	public float TotalLength { get; private set; }
	public float HalfWidth => RoadWidth * 0.5f;

	public Transform3D StartTransform { get; private set; }
	public List<Transform3D> CheckpointTransforms { get; private set; } = new();
	public List<Checkpoint> Checkpoints { get; private set; } = new();

	private static readonly PhysicsMaterial GroundPhysics = new() { Friction = 1.3f, Bounce = 0f };

	/// <summary>Builds the whole circuit. Must be called once before anything reads its geometry.</summary>
	public void Generate()
	{
		BuildPath();
		BuildRoadMesh();
		BuildLaneMarkings();
		BuildBarriers();
		BuildStartFinishGate();
		BuildCheckpoints();
	}

	private List<Vector3> GenerateControlPoints()
	{
		var points = new List<Vector3>();
		for (int i = 0; i < ControlPointCount; i++)
		{
			float a = (float)i / ControlPointCount * Mathf.Tau;
			float radius = 150f + 40f * Mathf.Sin(3f * a + 0.4f) + 18f * Mathf.Sin(5f * a + 1.1f);
			float height = 7f * Mathf.Sin(2f * a) + 3f * Mathf.Sin(5f * a + 1f);
			float x = radius * Mathf.Cos(a);
			float z = radius * Mathf.Sin(a);
			points.Add(new Vector3(x, height, z));
		}
		return points;
	}

	private void BuildPath()
	{
		var controlPoints = GenerateControlPoints();
		PathPoints = SplineUtils.SampleClosedLoop(controlPoints, SamplesPerSegment);

		int n = PathPoints.Count;
		Forwards = new List<Vector3>(n);
		Rights = new List<Vector3>(n);
		CumulativeDistance = new List<float>(n);

		for (int i = 0; i < n; i++)
		{
			Vector3 prev = PathPoints[(i - 1 + n) % n];
			Vector3 next = PathPoints[(i + 1) % n];
			Vector3 flat = new Vector3(next.X - prev.X, 0f, next.Z - prev.Z);
			if (flat.LengthSquared() < 0.0001f)
				flat = Vector3.Forward;
			flat = flat.Normalized();

			Forwards.Add(flat);
			Rights.Add(flat.Cross(Vector3.Up).Normalized());
		}

		float dist = 0f;
		for (int i = 0; i < n; i++)
		{
			CumulativeDistance.Add(dist);
			Vector3 current = PathPoints[i];
			Vector3 nextPoint = PathPoints[(i + 1) % n];
			dist += current.DistanceTo(nextPoint);
		}
		TotalLength = dist;
	}

	private static void AddTri(SurfaceTool st, List<Vector3> collisionFaces,
		Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
	{
		st.SetNormal(Vector3.Up);
		st.SetUV(uvA);
		st.AddVertex(a);
		st.SetNormal(Vector3.Up);
		st.SetUV(uvB);
		st.AddVertex(b);
		st.SetNormal(Vector3.Up);
		st.SetUV(uvC);
		st.AddVertex(c);
		collisionFaces.Add(a);
		collisionFaces.Add(b);
		collisionFaces.Add(c);
	}

	private void BuildRoadMesh()
	{
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		var collisionFaces = new List<Vector3>();

		int n = PathPoints.Count;
		for (int i = 0; i < n; i++)
		{
			int i2 = (i + 1) % n;
			Vector3 l0 = PathPoints[i] - Rights[i] * HalfWidth;
			Vector3 r0 = PathPoints[i] + Rights[i] * HalfWidth;
			Vector3 l1 = PathPoints[i2] - Rights[i2] * HalfWidth;
			Vector3 r1 = PathPoints[i2] + Rights[i2] * HalfWidth;

			float v0 = CumulativeDistance[i] / 6f;
			float v1 = (i2 == 0 ? TotalLength : CumulativeDistance[i2]) / 6f;

			AddTri(st, collisionFaces, l0, r0, r1, new Vector2(0, v0), new Vector2(1, v0), new Vector2(1, v1));
			AddTri(st, collisionFaces, l0, r1, l1, new Vector2(0, v0), new Vector2(1, v1), new Vector2(0, v1));
		}

		var mesh = st.Commit();
		var asphaltTex = ProceduralTextures.MakeNoiseTexture(64, new Color(0.14f, 0.14f, 0.155f), 0.025f, 7);
		var mat = new StandardMaterial3D
		{
			AlbedoTexture = asphaltTex,
			Roughness = 0.95f,
			Metallic = 0.0f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};

		var meshInstance = new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, Name = "RoadMesh" };
		AddChild(meshInstance);

		var body = new StaticBody3D { Name = "RoadCollision", PhysicsMaterialOverride = GroundPhysics };
		body.SetCollisionLayerValue(1, true);
		var shape = new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = collisionFaces.ToArray() } };
		body.AddChild(shape);
		AddChild(body);
	}

	private void BuildLaneMarkings()
	{
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		var dummyFaces = new List<Vector3>();
		int n = PathPoints.Count;

		for (int i = 0; i < n; i++)
		{
			int i2 = (i + 1) % n;
			float dashPos = CumulativeDistance[i] % 6f;
			bool dash = dashPos < 3f;
			if (dash)
				AddMarkingStrip(st, dummyFaces, i, i2, 0f, 0.18f, 0.02f);

			AddMarkingStrip(st, dummyFaces, i, i2, HalfWidth - 0.45f, HalfWidth - 0.25f, 0.02f);
			AddMarkingStrip(st, dummyFaces, i, i2, -(HalfWidth - 0.25f), -(HalfWidth - 0.45f), 0.02f);
		}

		var mesh = st.Commit();
		var mat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.95f, 0.85f, 0.2f),
			Roughness = 0.6f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			EmissionEnabled = true,
			Emission = new Color(0.6f, 0.5f, 0.1f),
			EmissionEnergyMultiplier = 0.25f
		};
		AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, Name = "LaneMarkings" });
	}

	private void AddMarkingStrip(SurfaceTool st, List<Vector3> faces, int i, int i2, float offsetA, float offsetB, float lift)
	{
		Vector3 a0 = PathPoints[i] + Rights[i] * offsetA + Vector3.Up * lift;
		Vector3 b0 = PathPoints[i] + Rights[i] * offsetB + Vector3.Up * lift;
		Vector3 a1 = PathPoints[i2] + Rights[i2] * offsetA + Vector3.Up * lift;
		Vector3 b1 = PathPoints[i2] + Rights[i2] * offsetB + Vector3.Up * lift;

		AddTri(st, faces, a0, b0, b1, Vector2.Zero, Vector2.Zero, Vector2.Zero);
		AddTri(st, faces, a0, b1, a1, Vector2.Zero, Vector2.Zero, Vector2.Zero);
	}

	private void BuildBarriers()
	{
		float barrierOffset = HalfWidth + 0.5f;
		int n = PathPoints.Count;

		// Visible rail ribbons (one on each side) with alternating red/white vertex colors.
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		var dummyFaces = new List<Vector3>();

		foreach (float side in new[] { -1f, 1f })
		{
			for (int i = 0; i < n; i++)
			{
				int i2 = (i + 1) % n;
				Vector3 a0 = PathPoints[i] + Rights[i] * (barrierOffset * side) + Vector3.Up * 0.65f;
				Vector3 a1 = PathPoints[i2] + Rights[i2] * (barrierOffset * side) + Vector3.Up * 0.65f;
				Vector3 b0 = a0 + Vector3.Up * 0.18f;
				Vector3 b1 = a1 + Vector3.Up * 0.18f;

				Color c = ((int)(CumulativeDistance[i] / 4f) % 2 == 0)
					? new Color(0.85f, 0.1f, 0.08f)
					: new Color(0.9f, 0.9f, 0.88f);

				st.SetColor(c); st.SetNormal(Vector3.Up); st.AddVertex(a0);
				st.SetColor(c); st.SetNormal(Vector3.Up); st.AddVertex(b0);
				st.SetColor(c); st.SetNormal(Vector3.Up); st.AddVertex(b1);
				st.SetColor(c); st.SetNormal(Vector3.Up); st.AddVertex(a0);
				st.SetColor(c); st.SetNormal(Vector3.Up); st.AddVertex(b1);
				st.SetColor(c); st.SetNormal(Vector3.Up); st.AddVertex(a1);
			}
		}

		var railMesh = st.Commit();
		var railMat = new StandardMaterial3D
		{
			VertexColorUseAsAlbedo = true,
			Roughness = 0.5f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};
		AddChild(new MeshInstance3D { Mesh = railMesh, MaterialOverride = railMat, Name = "GuardrailRails" });

		BuildGuardrailPosts(barrierOffset);
		BuildBarrierCollision(barrierOffset);
	}

	private void BuildGuardrailPosts(float barrierOffset)
	{
		int n = PathPoints.Count;
		float postSpacing = 6f;
		var transforms = new List<Transform3D>();

		foreach (float side in new[] { -1f, 1f })
		{
			float nextPostDist = 0f;
			for (int i = 0; i < n; i++)
			{
				if (CumulativeDistance[i] < nextPostDist)
					continue;
				nextPostDist += postSpacing;

				Vector3 pos = PathPoints[i] + Rights[i] * (barrierOffset * side) + Vector3.Up * 0.45f;
				var basis = Basis.LookingAt(Forwards[i], Vector3.Up);
				transforms.Add(new Transform3D(basis, pos));
			}
		}

		var postMesh = new BoxMesh { Size = new Vector3(0.1f, 0.9f, 0.1f) };
		var multiMesh = new MultiMesh
		{
			Mesh = postMesh,
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = transforms.Count
		};
		for (int i = 0; i < transforms.Count; i++)
			multiMesh.SetInstanceTransform(i, transforms[i]);

		var mmi = new MultiMeshInstance3D
		{
			Multimesh = multiMesh,
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.65f, 0.65f, 0.68f), Roughness = 0.6f, Metallic = 0.3f },
			Name = "GuardrailPosts"
		};
		AddChild(mmi);
	}

	private void BuildBarrierCollision(float barrierOffset)
	{
		int n = PathPoints.Count;
		var faces = new List<Vector3>();

		foreach (float side in new[] { -1f, 1f })
		{
			for (int i = 0; i < n; i++)
			{
				int i2 = (i + 1) % n;
				Vector3 a0 = PathPoints[i] + Rights[i] * (barrierOffset * side);
				Vector3 a1 = PathPoints[i2] + Rights[i2] * (barrierOffset * side);
				Vector3 b0 = a0 + Vector3.Up * 1.3f;
				Vector3 b1 = a1 + Vector3.Up * 1.3f;

				faces.Add(a0); faces.Add(b0); faces.Add(b1);
				faces.Add(a0); faces.Add(b1); faces.Add(a1);
			}
		}

		var body = new StaticBody3D { Name = "BarrierCollision", PhysicsMaterialOverride = GroundPhysics };
		body.SetCollisionLayerValue(1, true);
		var shape = new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = faces.ToArray() } };
		body.AddChild(shape);
		AddChild(body);
	}

	private void BuildStartFinishGate()
	{
		Vector3 origin = PathPoints[0];
		Vector3 right = Rights[0];
		Vector3 forward = Forwards[0];

		// Checkered line painted on the road.
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		int cols = 10;
		float cellWidth = RoadWidth / cols;
		float stripeDepth = 1.6f;
		for (int c = 0; c < cols; c++)
		{
			float offsetA = -HalfWidth + c * cellWidth;
			float offsetB = offsetA + cellWidth;
			Color color = c % 2 == 0 ? Colors.White : Colors.Black;

			Vector3 a0 = origin + right * offsetA - forward * stripeDepth * 0.5f + Vector3.Up * 0.03f;
			Vector3 a1 = origin + right * offsetB - forward * stripeDepth * 0.5f + Vector3.Up * 0.03f;
			Vector3 b0 = origin + right * offsetA + forward * stripeDepth * 0.5f + Vector3.Up * 0.03f;
			Vector3 b1 = origin + right * offsetB + forward * stripeDepth * 0.5f + Vector3.Up * 0.03f;

			st.SetColor(color); st.SetNormal(Vector3.Up); st.AddVertex(a0);
			st.SetColor(color); st.SetNormal(Vector3.Up); st.AddVertex(a1);
			st.SetColor(color); st.SetNormal(Vector3.Up); st.AddVertex(b1);
			st.SetColor(color); st.SetNormal(Vector3.Up); st.AddVertex(a0);
			st.SetColor(color); st.SetNormal(Vector3.Up); st.AddVertex(b1);
			st.SetColor(color); st.SetNormal(Vector3.Up); st.AddVertex(b0);
		}
		var stripeMesh = st.Commit();
		var stripeMat = new StandardMaterial3D { VertexColorUseAsAlbedo = true, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.8f };
		AddChild(new MeshInstance3D { Mesh = stripeMesh, MaterialOverride = stripeMat, Name = "StartFinishStripe" });

		// Gate arch.
		var archRoot = new Node3D { Name = "StartGate" };
		// Z axis = -forward so the node's local -Z (Godot's "front") points along the direction of travel.
		var archBasis = new Basis(right, Vector3.Up, -forward).Orthonormalized();
		archRoot.Transform = new Transform3D(archBasis, origin);
		AddChild(archRoot);

		var pillarMat = CarVisualMaterial(new Color(0.9f, 0.5f, 0.05f), 0.5f);
		var beamMat = CarVisualMaterial(new Color(0.95f, 0.95f, 0.95f), 0.4f);

		float pillarX = HalfWidth + 1.1f;
		AddBoxChild(archRoot, new Vector3(0.35f, 5.2f, 0.35f), new Vector3(-pillarX, 2.6f, 0), pillarMat);
		AddBoxChild(archRoot, new Vector3(0.35f, 5.2f, 0.35f), new Vector3(pillarX, 2.6f, 0), pillarMat);
		AddBoxChild(archRoot, new Vector3(pillarX * 2f + 0.5f, 0.5f, 0.5f), new Vector3(0, 5.2f, 0), beamMat);

		StartTransform = new Transform3D(archBasis, origin + Vector3.Up * 0.4f);
	}

	private static StandardMaterial3D CarVisualMaterial(Color c, float roughness)
	{
		return new StandardMaterial3D { AlbedoColor = c, Roughness = roughness, Metallic = 0.2f };
	}

	private static void AddBoxChild(Node3D parent, Vector3 size, Vector3 pos, Material mat)
	{
		var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = mat, Position = pos };
		parent.AddChild(mi);
	}

	private void BuildCheckpoints()
	{
		int n = PathPoints.Count;
		int step = n / CheckpointCount;

		for (int c = 0; c < CheckpointCount; c++)
		{
			int idx = (c * step) % n;
			Vector3 origin = PathPoints[idx];
			Vector3 right = Rights[idx];
			Vector3 forward = Forwards[idx];
			var basis = new Basis(right, Vector3.Up, forward).Orthonormalized();
			var transform = new Transform3D(basis, origin + Vector3.Up * 2f);

			CheckpointTransforms.Add(new Transform3D(basis, origin + Vector3.Up * 0.5f));

			var checkpoint = new Checkpoint { Index = c, Transform = transform, Name = $"Checkpoint{c}" };
			var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(RoadWidth + 2f, 4f, 1.5f) } };
			checkpoint.AddChild(shape);
			AddChild(checkpoint);
			Checkpoints.Add(checkpoint);
		}
	}
}
