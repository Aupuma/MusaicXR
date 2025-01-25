using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointCloudProcessor : MonoBehaviour
{
    // Structure for a 3D Point with thickness
    public struct PointWithThickness
    {
        public Vector3 Point;
        public float Thickness;

        public PointWithThickness(Vector3 point, float thickness)
        {
            Point = point;
            Thickness = thickness;
        }
    }

    // Function to order points based on angle in the XZ plane
    public static List<PointWithThickness> OrderLinePoints(List<PointWithThickness> P, Vector3 centre)
    {
        return P.OrderBy(p =>
        {
            Vector3 direction = p.Point - centre;
            float angle = Mathf.Atan2(direction.z, direction.x);
            return angle;
        }).ToList();
    }

    // Function to calculate density and variance of a subset of points
    public static (float density, Vector3 variance) ExtractParamsFromPieSlice(
        List<PointWithThickness> orderedPoints,
        float FullCircleSeconds,
        float PieSliceSeconds,
        float SliceStartSeconds)
    {
        SliceStartSeconds = Mathf.Repeat(SliceStartSeconds, FullCircleSeconds);
        int sizeOfP = orderedPoints.Count;

        // Calculate start index and step size
        int start = Mathf.Clamp(Mathf.FloorToInt((sizeOfP / FullCircleSeconds) * SliceStartSeconds), 0, sizeOfP - 1);
        int step = Mathf.Clamp(Mathf.FloorToInt((sizeOfP / FullCircleSeconds) * PieSliceSeconds), 0, sizeOfP - start);

        // Subset of points for analysis
        var subset = orderedPoints.Skip(start).Take(step).Select(p => p.Point).ToList();

        if (subset.Count == 0)
        {
            return (0f, Vector3.zero);
        }

        // Calculate density (points per unit volume)
        Vector3 minBounds = new Vector3(
            subset.Min(p => p.x),
            subset.Min(p => p.y),
            subset.Min(p => p.z));

        Vector3 maxBounds = new Vector3(
            subset.Max(p => p.x),
            subset.Max(p => p.y),
            subset.Max(p => p.z));

        Vector3 boundsSize = maxBounds - minBounds;
        float volume = boundsSize.x * boundsSize.y * boundsSize.z;
        float density = volume > 0 ? subset.Count / volume : 0f;

        // Calculate variance of the subset
        Vector3 mean = new Vector3(
            subset.Average(p => p.x),
            subset.Average(p => p.y),
            subset.Average(p => p.z));

        Vector3 variance = new Vector3(
            subset.Average(p => Mathf.Pow(p.x - mean.x, 2)),
            subset.Average(p => Mathf.Pow(p.y - mean.y, 2)),
            subset.Average(p => Mathf.Pow(p.z - mean.z, 2))
        );

        return (density, variance);
    }

    // Example usage
    void Start()
    {
        List<PointWithThickness> blue_line = new List<PointWithThickness>
        {
            new PointWithThickness(new Vector3(1, 0, 1), 0.5f),
            new PointWithThickness(new Vector3(2, 0, 2), 0.3f),
            new PointWithThickness(new Vector3(-1, 0, -1), 0.7f),
            new PointWithThickness(new Vector3(0, 0, 1), 0.4f),
        };

        Vector3 centre = new Vector3(0, 0, 0);

        // Order points
        var orderedPoints = OrderLinePoints(blue_line, centre);

        // Extract parameters
        float T = 10f;
        float W = 2f;
        float S = 1f;

        (float density, Vector3 variance) = ExtractParamsFromPieSlice(orderedPoints, T, W, S);

        Debug.Log($"Density: {density}, Variance: {variance}");
    }
}
