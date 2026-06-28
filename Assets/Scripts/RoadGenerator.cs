using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System; // 引入 System 用于 StringSplitOptions

public class RoadGenerator : MonoBehaviour
{
    [Header("File Settings")]
    [Tooltip("Path to the chart timing file relative to project root.")]
    public string chartPath = "Assets/Charts/GrayEfflorescence/GrayEfflorescence.txt";

    [Header("Track Configuration")]
    [Tooltip("Horizontal movement speed of the player in units per second.")]
    public static float speed = 9f;

    [Tooltip("Width of each road segment.")]
    public static float pathWidth = 2f;

    [Tooltip("Thickness / Height of each road segment.")]
    public static float pathThickness = 0.5f;

    [Header("Material Settings")]
    [Tooltip("Material applied to the generated road segments.")]
    public Material roadMaterial;

    [Header("Spawn Settings")]
    [Tooltip("Optional floor prefab instead of a basic Unity cube.")]
    public GameObject floorPrefab;
    public PlayerController playerController;

    public static readonly string GENERATED_ROAD_TRACK_STR = "GeneratedRoadTrack";

    /// <summary>
    /// 外部调用的入口，内部启动统一的异步加载协程
    /// </summary>
        /// <summary>
    /// 外部调用的总入口（兼容 Edit Mode 可视化点击与 Runtime 模式）
    /// </summary>
    public void GenerateRoad()
    {
        if (string.IsNullOrEmpty(chartPath))
        {
            Debug.LogError("Chart path is empty! Please import a chart first.");
            return;
        }

        // 💡 核心修复：如果是处于编辑器下的【非运行状态】
        if (!Application.isPlaying)
        {
            // 检查纯物理文件是否存在
            if (System.IO.File.Exists(chartPath))
            {
                Debug.Log("[Edit Mode同步解析] 检测到处于编辑器模式，直接从本地物理硬盘读取谱面...");
                string textContent = System.IO.File.ReadAllText(chartPath);
                
                // 绕过协程，直接秒杀构建赛道，这样 Undo 和可视化就会瞬间完全复活！
                BuildTrackFromText(textContent); 
            }
            else
            {
                Debug.LogError($"[Edit Mode失败] 在电脑磁盘中未找到该谱面文件，无法执行同步构建: {chartPath}");
            }
        }
        else
        {
            // 💡 运行模式（Runtime / WebGL 网页端）：继续安稳地走跨平台兼容的网络协程
            StartCoroutine(LoadChartAndBuildTrack());
        }
    }

    /// <summary>
    /// 跨平台异步下载谱面文本的协程
    /// </summary>
    private IEnumerator LoadChartAndBuildTrack()
    {
        string url = chartPath;
        
        if (!url.Contains("://"))
        {
            url = "file://" + url;
        }

        Debug.Log($"[谱面解析] 正在请求谱面文本，统一请求 URL: {url}");

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string textContent = uwr.downloadHandler.text;
                Debug.Log("[谱面下载成功] 开始解析文本内容并构建赛道...");
                BuildTrackFromText(textContent);
            }
            else
            {
                Debug.LogError($"[谱面下载失败] 无法读取该谱面。错误信息: {uwr.error}，请求路径: {url}");
            }
        }
    }

    /// <summary>
    /// 赛道构建的核心业务逻辑
    /// </summary>
    private void BuildTrackFromText(string chartText)
    {
        // 1. 清理历史赛道
        ClearGeneratedRoad();

        // 2. 将纯文本行转换为时间戳
        List<float> timestamps = ParseChartTextToTimestamps(chartText);
        if (timestamps == null || timestamps.Count == 0)
        {
            Debug.LogError("No valid timestamps parsed. Aborting road generation.");
            return;
        }

        // 3. 创建父节点
        GameObject roadParent = new GameObject(GENERATED_ROAD_TRACK_STR);
        roadParent.transform.position = Vector3.zero;

        Vector3 lastPoint = Vector3.zero; // 从原点开始
        Vector3 currentDirection = Vector3.forward; // 默认向前 Z

        // 编辑器 Undo 注册
        #if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(roadParent, "Generate Road Track Parent");
        #endif

        // 4. 顺序生成谱面方块
        for (int i = 0; i < timestamps.Count; i++)
        {
            float currentTime = timestamps[i];
            float prevTime = (i == 0) ? 0f : timestamps[i - 1];
            float deltaTime = currentTime - prevTime;

            float segmentLength = speed * deltaTime;
            Vector3 nextPoint = lastPoint + currentDirection * segmentLength;

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BoxCollider col = segment.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = false;
            }
            
            segment.name = $"Segment_{i + 1}_" + (currentDirection == Vector3.forward ? "Z" : "X");
            segment.transform.parent = roadParent.transform;
            
            // 显式管理材质，避免 WebGL URP 变紫
            var renderer = segment.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (roadMaterial != null)
                {
                    renderer.sharedMaterial = roadMaterial;
                }
                else
                {
                    Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpLitShader != null)
                    {
                        renderer.material = new Material(urpLitShader);
                    }
                    renderer.material.SetColor("_BaseColor", SettingManager.groundColor);
                }
            }

            Vector3 midpoint = (lastPoint + nextPoint) * 0.5f;
            segment.transform.position = new Vector3(midpoint.x, -pathThickness * 0.5f, midpoint.z);
            segment.transform.rotation = Quaternion.LookRotation(currentDirection);

            segment.transform.localScale = new Vector3(pathWidth, pathThickness, segmentLength + pathWidth);

            #if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(segment, "Generate Road Segment");
            #endif

            // 转向准备下一轮
            lastPoint = nextPoint;
            currentDirection = (currentDirection == Vector3.forward) ? Vector3.right : Vector3.forward;
        }

        // =================================================================
        // 💡 关键新增：在最后一个时间戳端点后，追加一段长度为 9999 的无限延伸路段
        // =================================================================
        float finalSegmentLength = 9999f;
        Vector3 finalNextPoint = lastPoint + currentDirection * finalSegmentLength;

        GameObject finalSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        BoxCollider finalCol = finalSegment.GetComponent<BoxCollider>();
        if (finalCol != null)
        {
            finalCol.isTrigger = false;
        }

        finalSegment.name = "Segment_Final_Endless_" + (currentDirection == Vector3.forward ? "Z" : "X");
        finalSegment.transform.parent = roadParent.transform;

        // 绑定 URP 材质
        var finalRenderer = finalSegment.GetComponent<MeshRenderer>();
        if (finalRenderer != null)
        {
            if (roadMaterial != null)
            {
                finalRenderer.sharedMaterial = roadMaterial;
            }
            else
            {
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader != null)
                {
                    finalRenderer.material = new Material(urpLitShader);
                }
                finalRenderer.material.SetColor("_BaseColor", SettingManager.groundColor);
            }
        }

        // 计算最后一段路的中点与旋转
        Vector3 finalMidpoint = (lastPoint + finalNextPoint) * 0.5f;
        finalSegment.transform.position = new Vector3(finalMidpoint.x, -pathThickness * 0.5f, finalMidpoint.z);
        finalSegment.transform.rotation = Quaternion.LookRotation(currentDirection);

        // 设置缩放
        finalSegment.transform.localScale = new Vector3(pathWidth, pathThickness, finalSegmentLength + pathWidth);

        #if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(finalSegment, "Generate Final Endless Road Segment");
        #endif

        // 统一刷上地面颜色
        playerController.ApplyGroundColor();

        Debug.Log($"[赛道构建成功] 谱面构建完毕。已成功在终点追加 9999 长度保底赛道。");
    }

    /// <summary>
    /// 将原本基于 System.IO.File 的解析改为基于纯内存文本字符串的按行分割解析
    /// </summary>
    private List<float> ParseChartTextToTimestamps(string textContent)
    {
        List<float> list = new List<float>();

        if (string.IsNullOrEmpty(textContent)) return list;

        string[] lines = textContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("timegroup"))
            {
                continue;
            }

            if (float.TryParse(trimmed, out float ms))
            {
                list.Add(ms / 1000f);
            }
            else
            {
                Debug.LogWarning($"[谱面解析过滤] 跳过无法识别的配置行: '{line}'");
            }
        }

        return list;
    }

    /// <summary>
    /// Safely clears any previously generated track.
    /// </summary>
    public void ClearGeneratedRoad()
    {
        GameObject oldRoad = GameObject.Find(GENERATED_ROAD_TRACK_STR);
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
}