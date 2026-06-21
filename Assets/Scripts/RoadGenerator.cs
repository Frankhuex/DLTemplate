using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class RoadGenerator : MonoBehaviour
{
    [Header("File Settings")]
    [Tooltip("Path to the chart timing file relative to project root.")]
    public string chartPath = "Assets/Charts/GrayEfflorescence/GrayEfflorescence.txt";

    [Header("Track Configuration")]
    [Tooltip("Horizontal movement speed of the player in units per second.")]
    public float speed = 6.0f;

    [Tooltip("Width of each road segment.")]
    public float pathWidth = 2.0f;

    [Tooltip("Thickness / Height of each road segment.")]
    public float pathThickness = 0.5f;

    [Header("Material Settings")]
    [Tooltip("Material applied to the generated road segments.")]
    public Material roadMaterial;

    [Header("Spawn Settings")]
    [Tooltip("Optional floor prefab instead of a basic Unity cube.")]
    public GameObject floorPrefab;

    /// <summary>
    /// Reads the chart file, parses timestamps, and builds a permanent 3D track in the editor.
    /// Supports Undo and registers objects cleanly.
    /// </summary>
    public void GenerateRoadInEditor()
    {
        // 1. Clean previous generation
        ClearGeneratedRoad();

        // 2. Parse timestamps from file
        List<float> timestamps = ParseChartTimestamps();
        if (timestamps == null || timestamps.Count == 0)
        {
            Debug.LogError("No valid timestamps parsed. Aborting road generation.");
            return;
        }

        // 3. Create parent container to group generated objects
        GameObject roadParent = new GameObject("Generated_Road_Track");
        roadParent.transform.position = Vector3.zero;

        Vector3 lastPoint = Vector3.zero; // Start at center of coordinate
        Vector3 currentDirection = Vector3.forward; // Start heading +Z

        // Register container for Undo
        #if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(roadParent, "Generate Road Track Parent");
        #endif

        // 4. Generate segments sequentially
        for (int i = 0; i < timestamps.Count; i++)
        {
            float currentTime = timestamps[i];
            float prevTime = (i == 0) ? 0f : timestamps[i - 1];
            float deltaTime = currentTime - prevTime;

            // Length of segment = speed * duration
            float segmentLength = speed * deltaTime;

            // Calculate exact target coordinate
            Vector3 nextPoint = lastPoint + currentDirection * segmentLength;

            // Create segment cube (using prefab if assigned, otherwise primitive Cube)
            GameObject segment;
            if (floorPrefab != null)
            {
                segment = Instantiate(floorPrefab);
            }
            else
            {
                segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // Keep the default BoxCollider so it acts as solid ground!
                BoxCollider col = segment.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.isTrigger = false;
                }
            }

            segment.name = $"Segment_{i + 1}_" + (currentDirection == Vector3.forward ? "Z" : "X");
            segment.transform.parent = roadParent.transform;

            // Assign Material
            if (roadMaterial != null)
            {
                segment.GetComponent<Renderer>().sharedMaterial = roadMaterial;
            }

            // Calculate midpoint for position placement (Pivot is at center of default Cube)
            Vector3 midpoint = (lastPoint + nextPoint) * 0.5f;
            segment.transform.position = new Vector3(midpoint.x, -pathThickness * 0.5f, midpoint.z);
            segment.transform.rotation = Quaternion.LookRotation(currentDirection);

            // Scale segment: X=width, Y=thickness, Z=length
            // Z adds pathWidth so consecutive perpendicular segments overlap symmetrically at corners
            segment.transform.localScale = new Vector3(pathWidth, pathThickness, segmentLength + pathWidth);

            // Register segment for Undo
            #if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(segment, "Generate Road Segment");
            #endif

            // Shift states to prepare for next turn
            lastPoint = nextPoint;
            currentDirection = (currentDirection == Vector3.forward) ? Vector3.right : Vector3.forward;
        }

        Debug.Log($"Successfully generated {timestamps.Count} seamless track segments! Total road span: {lastPoint.magnitude:F1} units.");
    }

    /// <summary>
    /// Safely clears any previously generated track.
    /// </summary>
    public void ClearGeneratedRoad()
    {
        GameObject oldRoad = GameObject.Find("Generated_Road_Track");
        if (oldRoad != null)
        {
            #if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(oldRoad);
            #else
            Destroy(oldRoad);
            #endif
            Debug.Log("Cleared previous generated track.");
        }
    }

    /// <summary>
    /// Reads and parses timestamps (in milliseconds) into seconds.
    /// </summary>
    private List<float> ParseChartTimestamps()
    {
        List<float> list = new List<float>();

        if (!File.Exists(chartPath))
        {
            Debug.LogError($"Chart file not found at: {chartPath}. Please check your path.");
            return list;
        }

        string[] lines = File.ReadAllLines(chartPath);
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("timegroup"))
            {
                continue; // Ignore comments and headers
            }

            if (float.TryParse(trimmed, out float ms))
            {
                list.Add(ms / 1000f); // Convert ms to seconds
            }
            else
            {
                Debug.LogWarning($"Skipped unparseable line: '{line}'");
            }
        }

        return list;
    }
}