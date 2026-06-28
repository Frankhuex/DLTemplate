using UnityEngine;
using System;
using System.Collections;

public class SettingManager : MonoBehaviour
{
    public GameObject settingPanel;
    public GameObject importPanel;
    public PlayerController player;
    public RoadGenerator roadGenerator;

    [Header("Sliders & UI Texts")]
    public UnityEngine.UI.Slider latencySlider;
    public TMPro.TMP_Text latencyText;

    public UnityEngine.UI.Slider lineColorRSlider;
    public UnityEngine.UI.Slider lineColorGSlider;
    public UnityEngine.UI.Slider lineColorBSlider;
    public TMPro.TMP_Text lineColorRText;
    public TMPro.TMP_Text lineColorGText;
    public TMPro.TMP_Text lineColorBText;

    public UnityEngine.UI.Slider groundColorRSlider;
    public UnityEngine.UI.Slider groundColorGSlider;
    public UnityEngine.UI.Slider groundColorBSlider;
    public TMPro.TMP_Text groundColorRText;
    public TMPro.TMP_Text groundColorGText;
    public TMPro.TMP_Text groundColorBText;
    
    [Header("Import File Name Displayer")]
    public TMPro.TMP_Text chartNameText;
    public TMPro.TMP_Text musicNameText;

    [Header("Global Settings Configuration")]
    public static float latency = 0f;
    public static Color lineColor = Color.orange;
    public static Color groundColor = Color.darkGray;

    public static bool isImportPanelOpen = false;
    public static bool isSettingPanelOpen = false;

    public enum ColorDim
    {
        R = 0,
        G = 1,
        B = 2,
    }

    public enum ColorObjType
    {
        LINE = 0,
        GROUND = 1,
    }

    public void Start()
    {
        // 1. 初始化面板状态
        settingPanel.SetActive(false);
        importPanel.SetActive(false);
        isImportPanelOpen = false;
        isSettingPanelOpen = false;
        
        // 2. 将滑动条的初始值与全局静态数据同步
        latencySlider.value = latency;
        lineColorRSlider.value = lineColor.r;
        lineColorGSlider.value = lineColor.g;
        lineColorBSlider.value = lineColor.b;
        groundColorRSlider.value = groundColor.r;
        groundColorGSlider.value = groundColor.g;
        groundColorBSlider.value = groundColor.b;

        // 3. 初始化 UI 文本数字显示
        SetFloatText(latencyText, latency);
        SetFloatText(lineColorRText, lineColor.r);
        SetFloatText(lineColorGText, lineColor.g);
        SetFloatText(lineColorBText, lineColor.b);
        SetFloatText(groundColorRText, groundColor.r);
        SetFloatText(groundColorGText, groundColor.g);
        SetFloatText(groundColorBText, groundColor.b);

        // 4. 监听所有 Slider 的变化事件
        latencySlider.onValueChanged.AddListener(SetLatency);
        lineColorRSlider.onValueChanged.AddListener((value) => SetColorAndText(ColorObjType.LINE, ColorDim.R, value));
        lineColorGSlider.onValueChanged.AddListener((value) => SetColorAndText(ColorObjType.LINE, ColorDim.G, value));
        lineColorBSlider.onValueChanged.AddListener((value) => SetColorAndText(ColorObjType.LINE, ColorDim.B, value));
        groundColorRSlider.onValueChanged.AddListener((value) => SetColorAndText(ColorObjType.GROUND, ColorDim.R, value));
        groundColorGSlider.onValueChanged.AddListener((value) => SetColorAndText(ColorObjType.GROUND, ColorDim.G, value));
        groundColorBSlider.onValueChanged.AddListener((value) => SetColorAndText(ColorObjType.GROUND, ColorDim.B, value));

        // 5. 开局渲染一次默认音乐名字（从 PlayerController.musicPath 静态区读）
        if (musicNameText != null)
        {
            musicNameText.text = PlayerController.musicPath;
        }
    }

    // =================================================================
    // 💡 核心数据接收区：无论是 WebGL 还是 PC，最后都统一回调这里
    // =================================================================
    
    /// <summary>
    /// 当谱面文本文件上传成功后触发
    /// </summary>
    public void OnWebGLChartUpload(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            chartNameText.text = System.IO.Path.GetFileName(url);
            roadGenerator.chartPath = url; // 将本地路径或网页 blob:// 链接喂给地图生成器
        }
    }

    /// <summary>
    /// 当音频音乐文件上传成功后触发
    /// </summary>
    public void OnWebGLMusicUpload(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            musicNameText.text = System.IO.Path.GetFileName(url);
            PlayerController.musicPath = url; 

            // 驱动玩家身上的网络请求协程去流式加载这首歌
            StartCoroutine(player.LoadAudioAndAssign(url));
        }
    }

    public void GenerateRoad()
    {
        roadGenerator.GenerateRoad();
    }

    // =================================================================
    // 💡 UI 面板与滑块事件响应区
    // =================================================================

    public void SetLatency(float _latency)
    {
        latency = _latency;
        SetFloatText(latencyText, latency);
    }

    public void SetColorAndText(ColorObjType type, ColorDim dim, float value)
    {
        SetText(type, value);
        SetColor(type, dim, value);
    }

    public void SetText(ColorObjType type, float value)
    {
        switch (type)
        {
            case ColorObjType.LINE:
                SetFloatText(lineColorRText, value);
                break;
            case ColorObjType.GROUND:
                SetFloatText(groundColorRText, value);
                break;
            default:
                return;
        }
    }

    public void SetFloatText(TMPro.TMP_Text text, float value)
    {
        text.text = value.ToString("F2");
    }

    public void SetColor(ColorObjType type, ColorDim dim, float value)
    {
        Color color;
        switch (type)
        {
            case ColorObjType.LINE:
                color = lineColor;
                break;
            case ColorObjType.GROUND:
                color = groundColor;
                break;
            default:
                return;
        }
        color = dim switch
        {
            ColorDim.R => new Color(value, color.g, color.b),
            ColorDim.G => new Color(color.r, value, color.b),
            ColorDim.B => new Color(color.r, color.g, value),
            _ => color,
        };
        switch (type)
        {
            case ColorObjType.LINE:
                lineColor = color;
                player.ApplyLineColor();
                break;
            case ColorObjType.GROUND:
                groundColor = color;
                player.ApplyGroundColor();
                break;
            default:
                return;
        }
    }

    public void ToggleSettingPanel()
    {
        bool targetState = !settingPanel.activeSelf;
        settingPanel.SetActive(targetState);
        isSettingPanelOpen = targetState;
    }

    public void ToggleImportPanel()
    {
        bool targetState = !importPanel.activeSelf;
        importPanel.SetActive(targetState);
        isImportPanelOpen = targetState;
    }
}