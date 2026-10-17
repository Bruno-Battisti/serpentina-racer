using System.Collections.Generic;
using Godot;

namespace SerpentinaRacer.Environment;

/// <summary>Cheap nearest-point lookup against a decimated copy of the track center-line,
/// used to keep terrain seated under the road and to keep scenery off the asphalt.</summary>
public class PathProximity
{
	private readonly List<Vector2> _xz = new();
	private readonly List<float> _heights = new();

	public PathProximity(List<Vector3> fullPath, int step)
	{
		for (int i = 0; i < fullPath.Count; i += step)
		{
			_xz.Add(new Vector2(fullPath[i].X, fullPath[i].Z));
			_heights.Add(fullPath[i].Y);
		}
	}

	public (float distance, float height) Nearest(Vector2 p)
	{
		float best = float.MaxValue;
		float bestHeight = 0f;
		for (int i = 0; i < _xz.Count; i++)
		{
			float d = p.DistanceSquaredTo(_xz[i]);
			if (d < best)
			{
				best = d;
				bestHeight = _heights[i];
			}
		}
		return (Mathf.Sqrt(best), bestHeight);
	}
}
