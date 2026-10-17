using System.Collections.Generic;
using Godot;

namespace SerpentinaRacer.Track;

public static class SplineUtils
{
	/// <summary>Catmull-Rom interpolation between p1 and p2, using p0/p3 as outer tangents.</summary>
	public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float t2 = t * t;
		float t3 = t2 * t;
		return 0.5f * (
			(2f * p1) +
			(-p0 + p2) * t +
			(2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
			(-p0 + 3f * p1 - 3f * p2 + p3) * t3);
	}

	/// <summary>Samples a smooth closed loop through the control points.</summary>
	public static List<Vector3> SampleClosedLoop(List<Vector3> controlPoints, int samplesPerSegment)
	{
		var result = new List<Vector3>();
		int n = controlPoints.Count;
		for (int i = 0; i < n; i++)
		{
			Vector3 p0 = controlPoints[(i - 1 + n) % n];
			Vector3 p1 = controlPoints[i];
			Vector3 p2 = controlPoints[(i + 1) % n];
			Vector3 p3 = controlPoints[(i + 2) % n];

			for (int s = 0; s < samplesPerSegment; s++)
			{
				float t = (float)s / samplesPerSegment;
				result.Add(CatmullRom(p0, p1, p2, p3, t));
			}
		}
		return result;
	}
}
