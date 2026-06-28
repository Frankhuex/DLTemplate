using UnityEngine;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;
using SFB;

public class MusicImportButton : MonoBehaviour, IPointerDownHandler
{
    // 把你的 SettingManager 拖进来
    public SettingManager settingManager;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif

    void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var btn = GetComponent<UnityEngine.UI.Button>();
        if (btn != null) btn.onClick.AddListener(OnClickStandalone);
#endif
    }

    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(settingManager.gameObject.name, "OnWebGLMusicUpload", ".mp3,.wav,.ogg", false);
#endif
    }

    private void OnClickStandalone()
    {
        var filters = new[] { new ExtensionFilter("Sound Files", "mp3", "wav", "ogg") };
        StandaloneFileBrowser.OpenFilePanelAsync("Import Music", "", filters, false, paths =>
        {
            if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                settingManager.OnWebGLMusicUpload(paths[0]); // 复用 WebGL 的成功回调逻辑
            }
        });
    }
}