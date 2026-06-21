using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(RoadGenerator))]
public class RoadGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw all standard fields (speed, pathWidth, pathThickness, roadMaterial, floorPrefab etc.)
        DrawDefaultInspector();

        RoadGenerator generator = (RoadGenerator)target;

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Dancing Line Map Builder", EditorStyles.boldLabel);

        // 2. Button to Generate Road in Editor Mode
        GUI.backgroundColor = new Color(0.12f, 0.73f, 0.35f); // Beautiful green color
        if (GUILayout.Button("一键根据谱面生成轨道 (Edit Mode)", GUILayout.Height(42)))
        {
            // Execute the core generator
            generator.GenerateRoadInEditor();
            
            // Mark scene dirty so scene files detect modification and can be saved
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        // 3. Button to Clear Road
        GUI.backgroundColor = new Color(0.85f, 0.24f, 0.2f); // Crimson Red
        if (GUILayout.Button("清除已生成的轨道", GUILayout.Height(30)))
        {
            generator.ClearGeneratedRoad();
            
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        // Reset color to default
        GUI.backgroundColor = Color.white;
    }
}