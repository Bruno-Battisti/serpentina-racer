using System.Collections.Generic;
using Godot;
using SerpentinaRacer.Core;
using SerpentinaRacer.Track;

namespace SerpentinaRacer.Environment;

/// <summary>
/// Builds the whole backdrop around the circuit: sky/sun, a height-varied
/// terrain that is seated under the road and rises into mountains at the
/// edges, and scattered low-poly trees/rocks/bushes that never touch the
/// track surface.
/// </summary>
public partial class EnvironmentBuilder : Node3D
{
	[Export] public float TerrainRadius = 420f;
	[Export] public int TerrainResolution = 100;

	private static readonly PhysicsMaterial GroundPhysics = new() { Friction = 1.1f, Bounce = 0f };

	public void Generate(TrackBuilder track)
	{
		var proximity = new PathProximity(track.PathPoints, 4);
		BuildSkyAndLight();
		BuildTerrain(track, proximity);
		ScatterVegetation(track, proximity);
		BuildMountainRing();
	}

	private void BuildSkyAndLight()
	{
		var sky = new Sky { SkyMaterial = new ProceduralSkyMaterial
		{
			SkyTopColor = new Color(0.25f, 0.52f, 0.82f),
			SkyHorizonColor = new Color(0.75f, 0.85f, 0.9f),
			GroundBottomColor = new Color(0.3f, 0.32f, 0.28f),
			GroundHorizonColor = new Color(0.75f, 0.85f, 0.9f),
			SunAngleMax = 30f
		}};

		var env = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Sky,
			Sky = sky,
			AmbientLightSource = Godot.Environment.AmbientSource.Sky,
			AmbientLightEnergy = 1.0f,
			FogEnabled = true,
			FogLightColor = new Color(0.8f, 0.86f, 0.9f),
			FogDensity = 0.0015f,
			TonemapMode = Godot.Environment.ToneMapper.Filmic
		};

		AddChild(new WorldEnvironment { Environment = env });

		var sun = new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48f, -35f, 0f),
			LightEnergy = 1.15f,
			LightColor = new Color(1f, 0.96f, 0.88f),
			ShadowEnabled = true
		};
		AddChild(sun);
	}

	private void BuildTerrain(TrackBuilder track, PathProximity proximity)
	{
		int res = TerrainResolution;
		float half = TerrainRadius;
		float cell = (half * 2f) / res;

		var noiseLow = new FastNoiseLite { Seed = 11, Frequency = 0.01f };
		var noiseHigh = new FastNoiseLite { Seed = 22, Frequency = 0.05f };

		var heights = new float[(res + 1) * (res + 1)];
		var colors = new Color[(res + 1) * (res + 1)];

		for (int zi = 0; zi <= res; zi++)
		{
			for (int xi = 0; xi <= res; xi++)
			{
				float x = -half + xi * cell;
				float z = -half + zi * cell;
				var p = new Vector2(x, z);

				float distFromCenter = p.Length();
				float farT = Mathf.SmoothStep(230f, 390f, distFromCenter);

				float mountainNoise = noiseLow.GetNoise2D(x, z) * 42f + noiseHigh.GetNoise2D(x, z) * 10f;
				float mountainHeight = 30f + mountainNoise;

				var (trackDist, trackHeight) = proximity.Nearest(p);
				float rollingHill = noiseLow.GetNoise2D(x, z) * 2.5f;
				float trackBlend = Mathf.SmoothStep(track.HalfWidth + 6f, track.HalfWidth + 40f, trackDist);
				// Dip the terrain under/beside the road so it never pokes through the
				// asphalt mesh (z-fighting), fading back to normal ground further out.
				const float roadClearance = 0.4f;
				float baseHeight = Mathf.Lerp(trackHeight - roadClearance, trackHeight + rollingHill, trackBlend);

				float finalHeight = Mathf.Lerp(baseHeight, mountainHeight, farT);

				int idx = zi * (res + 1) + xi;
				heights[idx] = finalHeight;
				colors[idx] = ColorForHeight(finalHeight, trackHeight);
			}
		}

		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		for (int zi = 0; zi < res; zi++)
		{
			for (int xi = 0; xi < res; xi++)
			{
				int i00 = zi * (res + 1) + xi;
				int i10 = zi * (res + 1) + xi + 1;
				int i01 = (zi + 1) * (res + 1) + xi;
				int i11 = (zi + 1) * (res + 1) + xi + 1;

				Vector3 p00 = IndexToPos(xi, zi, cell, half, heights, res);
				Vector3 p10 = IndexToPos(xi + 1, zi, cell, half, heights, res);
				Vector3 p01 = IndexToPos(xi, zi + 1, cell, half, heights, res);
				Vector3 p11 = IndexToPos(xi + 1, zi + 1, cell, half, heights, res);

				AddTerrainTri(st, p00, p10, p11, colors[i00], colors[i10], colors[i11]);
				AddTerrainTri(st, p00, p11, p01, colors[i00], colors[i11], colors[i01]);
			}
		}
		st.GenerateNormals();
		var mesh = st.Commit();

		var mat = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 0.95f };
		AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, Name = "Terrain" });

		var body = new StaticBody3D { Name = "TerrainCollision", PhysicsMaterialOverride = GroundPhysics };
		body.SetCollisionLayerValue(1, true);
		var heightShape = new HeightMapShape3D
		{
			MapWidth = res + 1,
			MapDepth = res + 1,
			MapData = heights
		};
		var shapeNode = new CollisionShape3D { Shape = heightShape, Scale = new Vector3(cell, 1f, cell) };
		body.AddChild(shapeNode);
		AddChild(body);
	}

	private static Vector3 IndexToPos(int xi, int zi, float cell, float half, float[] heights, int res)
	{
		float x = -half + xi * cell;
		float z = -half + zi * cell;
		return new Vector3(x, heights[zi * (res + 1) + xi], z);
	}

	private static void AddTerrainTri(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
	{
		st.SetColor(ca); st.AddVertex(a);
		st.SetColor(cb); st.AddVertex(b);
		st.SetColor(cc); st.AddVertex(c);
	}

	private static Color ColorForHeight(float height, float trackHeight)
	{
		float relative = height - trackHeight;
		if (relative < 3f)
			return new Color(0.27f, 0.45f, 0.2f);
		if (relative < 12f)
			return new Color(0.42f, 0.4f, 0.28f);
		if (relative < 28f)
			return new Color(0.5f, 0.5f, 0.5f);
		return new Color(0.88f, 0.9f, 0.92f);
	}

	private void ScatterVegetation(TrackBuilder track, PathProximity proximity)
	{
		var rng = new RandomNumberGenerator { Seed = 99 };

		var treeTransforms = new List<Transform3D>();
		var treeFoliageTransforms = new List<Transform3D>();
		var rockTransforms = new List<Transform3D>();
		var bushTransforms = new List<Transform3D>();

		int attempts = 2600;
		for (int i = 0; i < attempts; i++)
		{
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float radius = rng.RandfRange(0f, TerrainRadius * 0.62f);
			var p = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

			var (dist, groundHeight) = proximity.Nearest(p);
			float clearance = track.HalfWidth + 6.5f;
			if (dist < clearance)
				continue;

			var pos = new Vector3(p.X, groundHeight, p.Y);

			if (dist < clearance + 7f)
			{
				// Near-road decoration strip: bushes only, kept low.
				if (rng.Randf() < 0.55f)
				{
					float s = rng.RandfRange(0.6f, 1.1f);
					bushTransforms.Add(Transform3D.Identity.Scaled(new Vector3(s, s, s)).Translated(pos));
				}
				continue;
			}

			float roll = rng.Randf();
			if (roll < 0.62f)
			{
				float scale = rng.RandfRange(0.8f, 1.9f);
				float yaw = rng.RandfRange(0f, Mathf.Tau);
				var basis = new Basis(Vector3.Up, yaw).Scaled(new Vector3(scale, scale, scale));
				treeTransforms.Add(new Transform3D(basis, pos));
				treeFoliageTransforms.Add(new Transform3D(basis, pos + Vector3.Up * (1.1f * scale)));
			}
			else if (roll < 0.85f)
			{
				float scale = rng.RandfRange(0.5f, 1.6f);
				float yaw = rng.RandfRange(0f, Mathf.Tau);
				var basis = new Basis(Vector3.Up, yaw).Scaled(new Vector3(scale, scale * 0.8f, scale));
				rockTransforms.Add(new Transform3D(basis, pos + Vector3.Up * 0.2f * scale));
			}
			else
			{
				float scale = rng.RandfRange(0.7f, 1.3f);
				bushTransforms.Add(Transform3D.Identity.Scaled(new Vector3(scale, scale, scale)).Translated(pos));
			}
		}

		AddMultiMesh("TreeTrunks", new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.26f, Height = 2.2f, RadialSegments = 6 },
			new Color(0.32f, 0.21f, 0.12f), 0.9f, treeTransforms);
		AddMultiMesh("TreeFoliage", new CylinderMesh { TopRadius = 0.05f, BottomRadius = 1.4f, Height = 3.0f, RadialSegments = 7 },
			new Color(0.16f, 0.4f, 0.18f), 0.85f, treeFoliageTransforms);
		AddMultiMesh("Rocks", new BoxMesh { Size = new Vector3(1.1f, 0.9f, 1.0f) },
			new Color(0.55f, 0.54f, 0.52f), 0.8f, rockTransforms);
		AddMultiMesh("Bushes", new SphereMesh { Radius = 0.55f, Height = 0.9f, RadialSegments = 8, Rings = 5 },
			new Color(0.22f, 0.45f, 0.22f), 0.9f, bushTransforms);
	}

	private void AddMultiMesh(string name, Mesh mesh, Color color, float roughness, List<Transform3D> transforms)
	{
		if (transforms.Count == 0)
			return;

		var multiMesh = new MultiMesh
		{
			Mesh = mesh,
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = transforms.Count
		};
		for (int i = 0; i < transforms.Count; i++)
			multiMesh.SetInstanceTransform(i, transforms[i]);

		var mmi = new MultiMeshInstance3D
		{
			Multimesh = multiMesh,
			MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = roughness },
			Name = name
		};
		AddChild(mmi);
	}

	private void BuildMountainRing()
	{
		var rng = new RandomNumberGenerator { Seed = 7 };
		var transforms = new List<Transform3D>();
		var peakMesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 1f, Height = 1f, RadialSegments = 6 };

		int count = 46;
		for (int i = 0; i < count; i++)
		{
			float angle = (float)i / count * Mathf.Tau + rng.RandfRange(-0.05f, 0.05f);
			float radius = TerrainRadius * rng.RandfRange(0.88f, 1.05f);
			float height = rng.RandfRange(55f, 130f);
			float baseScale = rng.RandfRange(55f, 95f);

			var pos = new Vector3(Mathf.Cos(angle) * radius, 20f, Mathf.Sin(angle) * radius);
			var basis = new Basis(Vector3.Up, rng.RandfRange(0f, Mathf.Tau)).Scaled(new Vector3(baseScale, height, baseScale));
			transforms.Add(new Transform3D(basis, pos));
		}

		AddMultiMesh("MountainRing", peakMesh, new Color(0.42f, 0.46f, 0.5f), 1.0f, transforms);
	}
}
