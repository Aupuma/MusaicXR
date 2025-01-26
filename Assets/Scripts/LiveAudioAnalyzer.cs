using UnityEngine;
using System.Collections.Generic;
using Fusion.Addons.LineDrawing;
using System.Linq;
using System;

public class LiveAudioAnalyzer : MonoBehaviour
{
    [System.Serializable]
    public class AudioLineType
    {
        public MusicLineType type;
    }

    // Event that will be triggered when slice analysis is complete
    public delegate void SliceAnalyzedHandler(List<MusicPacket> musicPackets);
    public event SliceAnalyzedHandler OnSliceAnalyzed;

    [Header("Line Types Setup")]
    public List<AudioLineType> lineTypes;

    [Header("Slice Settings")]
    public float cylinderHeight = 2f;     
    public float cylinderRadius = 5f;  
    public float xDivisor = 10f;
    public float yDivisor = 1f;   

    [Header("Debug")]
    public bool showDebugGizmos = false;
    public bool showDebugUI = true;  

    private NetworkTempoController tempoController;
    private float currentSliceAngle;
    private float currentSliceWidth;
    private Dictionary<Color, AudioLineType> colorToType;
    private bool isInitialized = false;

    private class DebugInfo
    {
        public int pointsInSlice;
        public float distribution;
    }
    private Dictionary<Color, DebugInfo> debugInfo = new Dictionary<Color, DebugInfo>();

    void Start()
    {
        InitializeTypes();
        ConnectToTempoController();
    }

    void InitializeTypes()
    {
        colorToType = new Dictionary<Color, AudioLineType>();
        foreach (var lineType in lineTypes)
        {
            if (lineType.type == null)
            {
                Debug.LogError("LiveAudioAnalyzer: MusicLineType not assigned!");
                continue;
            }
            colorToType[lineType.type.color] = lineType;
        }
        isInitialized = true;
    }

    void ConnectToTempoController()
    {
        tempoController = FindObjectOfType<NetworkTempoController>();
        if (tempoController != null)
        {
            tempoController.OnSliceUpdated += UpdateSlicePosition;
        }
        else
        {
            Debug.LogWarning("LiveAudioAnalyzer: No NetworkTempoController found in scene!");
        }
    }

    void UpdateSlicePosition(float angle, float width)
    {
        currentSliceAngle = angle;
        currentSliceWidth = width;
        AnalyzeLines();
    }

    void AnalyzeLines()
{
    if (!isInitialized) return;

    var allDrawings = FindObjectsOfType<NetworkLineDrawing>();
    Dictionary<Color, List<Vector3>> pointsInSliceByColor = new Dictionary<Color, List<Vector3>>();

    // Initialize debug info for all colors
    foreach (var lineType in lineTypes)
    {
        if (lineType.type != null)
        {
            Color color = lineType.type.color;
            if (!debugInfo.ContainsKey(color))
                debugInfo[color] = new DebugInfo();
            
            // Reset values
            debugInfo[color].pointsInSlice = 0;
            debugInfo[color].distribution = 0f;
        }
    }

    foreach (var drawing in allDrawings)
    {
        // Check if the network object is properly spawned and valid
        if (drawing == null || drawing.Object == null || !drawing.Object.IsValid)
            continue;

        // Check if the object has been properly initialized in the network
        if (!drawing.Object.IsValid)
            continue;

        try 
        {
            // Only process if the drawing is finished
            if (drawing.IsFinished)
            {
                var lineDrawing = drawing.GetComponent<LineDrawing>();
                var lineRenderer = lineDrawing?.transform.GetComponentInChildren<LineRenderer>();
                
                if (lineRenderer != null)
                {
                    Color lineColor = lineRenderer.startColor;
                    List<Vector3> pointsInSlice = GetPointsInSlice(lineRenderer);
                    
                    if (pointsInSlice.Count > 0)
                    {
                        if (!pointsInSliceByColor.ContainsKey(lineColor))
                            pointsInSliceByColor[lineColor] = new List<Vector3>();
                        pointsInSliceByColor[lineColor].AddRange(pointsInSlice);
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Skip this object if we can't access networked properties yet
            continue;
        }
    }

    List<MusicPacket> musicPackets = new List<MusicPacket>();

    // Analyze and trigger events for each color
    foreach (var kvp in pointsInSliceByColor)
    {
        Color color = kvp.Key;
        colorToType.TryGetValue(color, out var audioLineType);
        var clipId = audioLineType.type.clipId;
        List<Vector3> points = kvp.Value;
        float distribution = CalculateDistribution(points);

        // Update debug info
        if (debugInfo.ContainsKey(color))
        {
            debugInfo[color].pointsInSlice = points.Count;
            debugInfo[color].distribution = distribution;
        }

        var x = Mathf.Clamp(points.Count / xDivisor, 0f, 1f);
        var y = Mathf.Clamp(distribution / yDivisor, 0f, 1f);

        musicPackets.Add(new MusicPacket{
            clipID = clipId,
            parameters = new Dictionary<string, float>{
                {"x", x},
                {"y", y}
            }
        });
    }
    
    OnSliceAnalyzed?.Invoke(musicPackets);
}

    List<Vector3> GetPointsInSlice(LineRenderer line)
    {
        List<Vector3> pointsInSlice = new List<Vector3>();
        Vector3[] localPositions = new Vector3[line.positionCount];
        line.GetPositions(localPositions);

        Vector3[] worldPositions = new Vector3[line.positionCount];
        for (int i = 0; i < line.positionCount; i++)
        {
            worldPositions[i] = line.transform.TransformPoint(localPositions[i]);
        }

        foreach (Vector3 worldPos in worldPositions)
        {
            float relativeHeight = worldPos.y - transform.position.y;
            if (Mathf.Abs(relativeHeight) > cylinderHeight/2)
                continue;

            Vector2 pointXZ = new Vector2(
                worldPos.x - transform.position.x, 
                worldPos.z - transform.position.z
            );
            if (pointXZ.magnitude > cylinderRadius)
                continue;

            float angle = Vector2.SignedAngle(Vector2.right, pointXZ);
            angle = Mathf.Repeat(angle, 360f);
            
            if (IsAngleInSlice(angle))
            {
                pointsInSlice.Add(worldPos);
            }
        }

        return pointsInSlice;
    }

    // Calculate the distribution of points in 3D space
    float CalculateDistribution(List<Vector3> points)
    {
        if (points.Count < 2) return 0f;

        // Calculate centroid
        Vector3 centroid = Vector3.zero;
        foreach (var point in points)
            centroid += point;
        centroid /= points.Count;

        // Calculate average distance from centroid and standard deviation
        float averageDistance = 0f;
        float varianceSum = 0f;
        
        foreach (var point in points)
        {
            float distance = Vector3.Distance(point, centroid);
            averageDistance += distance;
        }
        averageDistance /= points.Count;

        foreach (var point in points)
        {
            float distance = Vector3.Distance(point, centroid);
            varianceSum += Mathf.Pow(distance - averageDistance, 2);
        }
        
        float standardDeviation = Mathf.Sqrt(varianceSum / points.Count);
        
        // Normalize the distribution value between 0 and 1
        // Higher standard deviation means more scattered points
        float maxPossibleDeviation = cylinderRadius; // Maximum possible deviation within cylinder
        return Mathf.Clamp01(standardDeviation / maxPossibleDeviation);
    }

    bool IsLineInSlice(LineRenderer line)
    {
        Vector3[] localPositions = new Vector3[line.positionCount];
        line.GetPositions(localPositions);

        // Convert local positions to world space
        Vector3[] worldPositions = new Vector3[line.positionCount];
        for (int i = 0; i < line.positionCount; i++)
        {
            worldPositions[i] = line.transform.TransformPoint(localPositions[i]);
        }

        for (int i = 0; i < worldPositions.Length; i++)
        {
            Vector3 worldPos = worldPositions[i];
            
            // Check height relative to analyzer position
            float relativeHeight = worldPos.y - transform.position.y;
            if (relativeHeight < -cylinderHeight/2 || relativeHeight > cylinderHeight/2)
                continue;

            // Check if point is within radius from analyzer center
            Vector2 pointXZ = new Vector2(
                worldPos.x - transform.position.x, 
                worldPos.z - transform.position.z
            );
            if (pointXZ.magnitude > cylinderRadius)
                continue;

            float angle = Vector2.SignedAngle(Vector2.right, pointXZ);
            angle = Mathf.Repeat(angle, 360f);
            
            if (IsAngleInSlice(angle))
                return true;
        }
        return false;
    }

    bool IsAngleInSlice(float angle)
    {
        float sliceEnd = (currentSliceAngle + currentSliceWidth) % 360f;
        if (currentSliceAngle <= sliceEnd)
            return angle >= currentSliceAngle && angle <= sliceEnd;
        else
            return angle >= currentSliceAngle || angle <= sliceEnd;
    }

    void OnGUI()
    {
        if (!showDebugUI) return;

        int yPos = 10;
        GUI.Label(new Rect(10, yPos, 300, 20), $"Current Slice Angle: {currentSliceAngle:F1}°");
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 300, 20), $"Slice Width: {currentSliceWidth:F1}°");
        yPos += 25;

        foreach (var lineType in lineTypes)
        {
            if (lineType.type == null) continue;

            Color color = lineType.type.color;
            if (debugInfo.TryGetValue(color, out var debug))
            {
                GUI.backgroundColor = color;
                string info = $"Color: {ColorToString(color)}\n" +
                            $"Points in slice: {debug.pointsInSlice}\n" +
                            $"Distribution: {debug.distribution:F2}";
                
                GUI.Label(new Rect(10, yPos, 300, 100), info);
                yPos += 105;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 center = transform.position;

        // Draw cylinder outline
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
        DrawCylinderWireframe(center, cylinderRadius, cylinderHeight);

        // Draw the slice
        Vector3 startDir = Quaternion.Euler(0, -currentSliceAngle, 0) * Vector3.right;
        Vector3 endDir = Quaternion.Euler(0, -(currentSliceAngle + currentSliceWidth), 0) * Vector3.right;

        // Draw lines at both top and bottom of cylinder
        float halfHeight = cylinderHeight / 2;
        Gizmos.color = Color.yellow;
        
        // Bottom
        Gizmos.DrawLine(center + Vector3.down * halfHeight, 
                        center + Vector3.down * halfHeight + startDir * cylinderRadius);
        Gizmos.DrawLine(center + Vector3.down * halfHeight, 
                        center + Vector3.down * halfHeight + endDir * cylinderRadius);
        
        // Top
        Gizmos.DrawLine(center + Vector3.up * halfHeight, 
                        center + Vector3.up * halfHeight + startDir * cylinderRadius);
        Gizmos.DrawLine(center + Vector3.up * halfHeight, 
                        center + Vector3.up * halfHeight + endDir * cylinderRadius);

        // Draw vertical lines at slice boundaries
        Gizmos.DrawLine(center + Vector3.down * halfHeight + startDir * cylinderRadius,
                        center + Vector3.up * halfHeight + startDir * cylinderRadius);
        Gizmos.DrawLine(center + Vector3.down * halfHeight + endDir * cylinderRadius,
                        center + Vector3.up * halfHeight + endDir * cylinderRadius);

        // Draw arc segments at different heights
        int segments = 32;
        for (float h = -halfHeight; h <= halfHeight; h += halfHeight)
        {
            Vector3 previousPoint = center + Vector3.up * h + startDir * cylinderRadius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = currentSliceAngle + (currentSliceWidth * i / segments);
                Vector3 direction = Quaternion.Euler(0, -angle, 0) * Vector3.right;
                Vector3 point = center + Vector3.up * h + direction * cylinderRadius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }

         // Debug active lines
    if (isInitialized && Application.isPlaying)
    {
        var allDrawings = FindObjectsOfType<NetworkLineDrawing>();
        foreach (var drawing in allDrawings)
        {
            if (drawing.IsFinished)
            {
                var lineRenderer = drawing.GetComponent<LineDrawing>()?.transform
                    .GetComponentInChildren<LineRenderer>();
                if (lineRenderer != null)
                {
                    Vector3[] localPositions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(localPositions);
                    
                    // Convert to world positions
                    Vector3[] worldPositions = new Vector3[lineRenderer.positionCount];
                    for (int i = 0; i < lineRenderer.positionCount; i++)
                    {
                        worldPositions[i] = lineRenderer.transform.TransformPoint(localPositions[i]);
                    }
                    
                    // Draw all points in cylinder in grey
                    Gizmos.color = Color.grey;
                    foreach (var worldPos in worldPositions)
                    {
                        float relativeHeight = worldPos.y - transform.position.y;
                        Vector2 pointXZ = new Vector2(
                            worldPos.x - transform.position.x, 
                            worldPos.z - transform.position.z
                        );
                        
                        if (Mathf.Abs(relativeHeight) <= cylinderHeight/2 && 
                            pointXZ.magnitude <= cylinderRadius)
                        {
                            Gizmos.DrawWireSphere(worldPos, 0.02f);
                        }
                    }
                    
                    // Draw points in slice in green
                    Gizmos.color = Color.green;
                    foreach (var worldPos in worldPositions)
                    {
                        float relativeHeight = worldPos.y - transform.position.y;
                        Vector2 pointXZ = new Vector2(
                            worldPos.x - transform.position.x, 
                            worldPos.z - transform.position.z
                        );
                        
                        float angle = Vector2.SignedAngle(Vector2.right, pointXZ);
                        angle = Mathf.Repeat(angle, 360f);

                        if (Mathf.Abs(relativeHeight) <= cylinderHeight/2 && 
                            pointXZ.magnitude <= cylinderRadius &&
                            IsAngleInSlice(angle))
                        {
                            Gizmos.DrawWireSphere(worldPos, 0.05f);
                        }
                    }
                }
            }
        }
    }
    }

    private void DrawCylinderWireframe(Vector3 center, float radius, float height)
    {
        float halfHeight = height / 2;
        int segments = 32;
        
        // Draw circles at top and bottom
        for (float h = -halfHeight; h <= halfHeight; h += height)
        {
            Vector3 previousPoint = center + Vector3.up * h + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i * 360f / segments);
                Vector3 direction = Quaternion.Euler(0, -angle, 0) * Vector3.right;
                Vector3 point = center + Vector3.up * h + direction * radius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
        
        // Draw vertical lines
        for (int i = 0; i < segments; i++)
        {
            float angle = (i * 360f / segments);
            Vector3 direction = Quaternion.Euler(0, -angle, 0) * Vector3.right;
            Vector3 bottom = center + Vector3.down * halfHeight + direction * radius;
            Vector3 top = center + Vector3.up * halfHeight + direction * radius;
            Gizmos.DrawLine(bottom, top);
        }
    }

    private string ColorToString(Color color)
    {
        if (color == Color.red) return "Red";
        if (color == Color.green) return "Green";
        if (color == Color.blue) return "Blue";
        if (color == Color.yellow) return "Yellow";
        if (color == Color.magenta) return "Magenta";
        if (color == Color.cyan) return "Cyan";
        return $"RGB({color.r:F2}, {color.g:F2}, {color.b:F2})";
    }
}