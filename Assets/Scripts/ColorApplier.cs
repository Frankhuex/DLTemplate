using UnityEngine;

public class ThemeColorReceiver : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    void Start()
    {
        ApplyThemeColor();
    }

    public void ApplyThemeColor()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // 💡 使用 material 临时修改，URP 管线下标准颜色属性为 "_BaseColor"
            meshRenderer.material.SetColor("_BaseColor", SettingManager.lineColor);
        }
    }
}