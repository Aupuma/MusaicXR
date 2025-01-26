using UnityEngine;

public class SliceVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveAudioAnalyzer audioAnalyzer;

    [Header("Visual Settings")]
    [SerializeField] private Material sliceMaterial;
    [SerializeField] private Color sliceColor = Color.yellow;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private int arcSegments = 32;

    private LineRenderer[] edgeLines;  // 0-3: vertical edges, 4-5: arc edges
    private LineRenderer[] arcLines;   // Cross-section arcs at different heights

    private void Start()
    {
        if (audioAnalyzer == null)
            audioAnalyzer = GetComponent<LiveAudioAnalyzer>();

        InitializeLineRenderers();
    }

    private void InitializeLineRenderers()
    {
        // Create edge lines
        edgeLines = new LineRenderer[6];
        for (int i = 0; i < 6; i++)
        {
            edgeLines[i] = CreateLineRenderer($"Edge_{i}", 2);
        }

        // Create arc lines for top and bottom
        arcLines = new LineRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            arcLines[i] = CreateLineRenderer($"Arc_{i}", arcSegments + 1);
        }
    }

    private LineRenderer CreateLineRenderer(string name, int pointCount)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.material = sliceMaterial != null ? sliceMaterial : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        line.startColor = line.endColor = sliceColor;
        line.startWidth = line.endWidth = lineWidth;
        line.positionCount = pointCount;
        line.useWorldSpace = true;
        
        return line;
    }

    private void Update()
    {
        if (audioAnalyzer == null) return;

        float angle = audioAnalyzer.CurrentSliceAngle;
        float width = audioAnalyzer.CurrentSliceWidth;
        float radius = audioAnalyzer.CylinderRadius;
        float height = audioAnalyzer.CylinderHeight;

        UpdateSliceVisualization(angle, width, radius, height);
    }

    private void UpdateSliceVisualization(float angle, float width, float radius, float height)
    {
        Vector3 center = transform.position;
        float halfHeight = height / 2f;

        // Calculate start and end directions
        Vector3 startDir = Quaternion.Euler(0, -angle, 0) * Vector3.right;
        Vector3 endDir = Quaternion.Euler(0, -(angle + width), 0) * Vector3.right;

        // Update vertical edge lines
        Vector3 bottomStart = center + Vector3.down * halfHeight;
        Vector3 topStart = center + Vector3.up * halfHeight;
        
        // Start edge verticals
        edgeLines[0].SetPosition(0, bottomStart);
        edgeLines[0].SetPosition(1, topStart);
        
        edgeLines[1].SetPosition(0, bottomStart + startDir * radius);
        edgeLines[1].SetPosition(1, topStart + startDir * radius);
        
        // End edge verticals
        edgeLines[2].SetPosition(0, bottomStart + endDir * radius);
        edgeLines[2].SetPosition(1, topStart + endDir * radius);

        // Bottom radius lines
        edgeLines[3].SetPosition(0, bottomStart);
        edgeLines[3].SetPosition(1, bottomStart + startDir * radius);
        
        edgeLines[4].SetPosition(0, bottomStart);
        edgeLines[4].SetPosition(1, bottomStart + endDir * radius);

        // Top radius lines
        edgeLines[5].SetPosition(0, topStart);
        edgeLines[5].SetPosition(1, topStart + startDir * radius);

        // Update arc lines for top and bottom
        for (int level = 0; level < 2; level++)
        {
            float heightOffset = level == 0 ? -halfHeight : halfHeight;
            Vector3 arcCenter = center + Vector3.up * heightOffset;
            
            Vector3[] arcPoints = new Vector3[arcSegments + 1];
            for (int i = 0; i <= arcSegments; i++)
            {
                float t = i / (float)arcSegments;
                float currentAngle = angle + width * t;
                Vector3 direction = Quaternion.Euler(0, -currentAngle, 0) * Vector3.right;
                arcPoints[i] = arcCenter + direction * radius;
            }
            
            arcLines[level].SetPositions(arcPoints);
        }
    }
}