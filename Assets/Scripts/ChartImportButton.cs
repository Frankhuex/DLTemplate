using UnityEngine;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;
using SFB;

public class ChartImportButton : MonoBehaviour, IPointerDownHandler
{
    // 把你的 SettingManager 拖进来
    public SettingManager settingManager;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif

    // 💡 只有在 PC/编辑器下才用标准点击
    void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var btn = GetComponent<UnityEngine.UI.Button>();
        if (btn != null) btn.onClick.AddListener(OnClickStandalone);
#endif
    }

    // 💡 WebGL 网页端：按下即触发，绝对不会被浏览器拦截，也不受射线穿透干扰
    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // 直接把通知发回给 SettingManager 的响应函数
        UploadFile(settingManager.gameObject.name, "OnWebGLChartUpload", ".txt", false);
#endif
    }

    private void OnClickStandalone()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("Import Chart (.txt)", "", new[] { new ExtensionFilter("Text", "txt") }, false, paths =>
        {
            if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                settingManager.OnWebGLChartUpload(paths[0]); // 复用 WebGL 的成功回调逻辑
            }
        });
    }
}