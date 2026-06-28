using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems; 
using System;
using SFB;
using UnityEngine.Networking;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    public static Vector3 startPosition = new(0, 0.25f, 0);
    public TMPro.TMP_Text notice;
    private bool isLoading = false;

    [Tooltip("Gravity acceleration (should be smaller for a floaty flat-projectile fall)")]
    public float gravity = -10f;

    [Header("Trail Settings")]
    [Tooltip("Prefab for the trail segment (should be a simple Cube)")]
    public GameObject trailPrefab;

    [Tooltip("Width of the trail segment")]
    public float trailWidth = 0.5f;

    [Tooltip("Height of the trail segment")]
    public float trailHeight = 0.5f;

    [Header("Death Settings")]
    [Tooltip("The Y position below which the player is considered dead (fall death)")]
    public float fallDeathY = -10f;

    [Header("Audio Settings")]
    [Tooltip("AudioSource for playing the background music.")]
    public AudioSource musicSource;

    [Tooltip("Fade-out duration of the music when player dies (in seconds).")]
    public float fadeOutDuration = 1.2f;

    private Vector3 currentDirection = Vector3.forward;
    private Vector3 lastTurnPoint;
    private GameObject currentTrailSegment;
    private List<GameObject> spawnedTrails = new List<GameObject>();
    
    private bool isPlaying = false;
    private bool isGrounded = true;
    private float verticalVelocity = 0f;
    private bool isDead = false;

    // Input Actions
    private InputAction jumpAction;
    private InputAction attackAction;
    private MeshRenderer playerRenderer;

    public static string musicPath = "Gray Efflorescence.wav";

    public void ApplyLineColor()
    {
        if (playerRenderer == null) return;
        playerRenderer.material.SetColor("_BaseColor", SettingManager.lineColor);
        
    }

    public void ApplyGroundColor()
    {
        GameObject generatedRoadTrack = GameObject.Find(RoadGenerator.GENERATED_ROAD_TRACK_STR);
        if (generatedRoadTrack != null) {
            foreach (Transform segment in generatedRoadTrack.transform)
            {
                segment.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", SettingManager.groundColor);
            }
        }
    }

    private void Start()
    {
        notice.gameObject.SetActive(false);
        playerRenderer = GetComponent<MeshRenderer>();
        ApplyLineColor();

        // Set player local scale
        transform.localScale = new Vector3(trailHeight, trailHeight, trailHeight);

        // Find and bind Input Actions
        if (InputSystem.actions != null)
        {
            jumpAction = InputSystem.actions.FindAction("Jump");
            attackAction = InputSystem.actions.FindAction("Attack");
        }
        else
        {
            Debug.LogWarning("Project-wide Input Actions not found. Falling back to direct keyboard spacebar check.");
        }

        // =================================================================
        // 💡 绝对路径修复：动态获取 index.html 所在目录的真实网址
        // =================================================================
#if UNITY_WEBGL && !UNITY_EDITOR
        if (musicSource != null)
        {
            musicSource.clip = null; // 依然保持开机清空，防止时钟锁死
        }

        string initialPath = musicPath;

        if (!string.IsNullOrEmpty(initialPath))
        {
            // 💡 核心黑魔法：借用内置的 Application.absoluteURL 
            // 在 WebGL 端，它返回的就是当前网页的完整网址（例如 http://localhost:51389/index.html）
            string baseUrl = Application.absoluteURL;

            // 完美的字符串裁剪：剥离掉 "index.html"，拿到纯粹的网页根目录 URL
            if (baseUrl.EndsWith("index.html"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - "index.html".Length);
            }
            else if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }

            // 强行拼接！确保最终发给 UnityWebRequest 的路径一定是：
            // http://localhost:xxxx/你的默认歌名.wav
            initialPath = baseUrl + initialPath;
        }

        if (!string.IsNullOrEmpty(initialPath))
        {
            Debug.Log($"[WebGL绝对寻址触发] 正在请求 index.html 同目录下的静态音频，绝对网址: {initialPath}");
            StartCoroutine(LoadAudioAndAssign(initialPath));
        }
#else
        // 编辑器/PC端保持原样，直接用 Inspector 拖好的音频资产测试
        Debug.Log("[PC/编辑器模式] 直接采用 AudioSource 挂载的默认 Clip 进行测试。");
#endif
    }
    private void Update()
    {
        // If dead, do absolutely nothing
        if (isDead) return;

        // 1. Handle Start Game
        if (!isPlaying)
        {
            if (CheckTurnInput() && !SettingManager.isImportPanelOpen && !SettingManager.isSettingPanelOpen && !isLoading)
            {
                StartGame();
            }
            return;
        }

        // 2. Handle Turn Input during gameplay (ONLY allowed when grounded!)
        if (isGrounded && CheckTurnInput())
        {
            Turn();
        }

        // 3. Ground & Falling Detection
        HandlePhysicsAndFalling();

        // 4. Move Player (Horizontal + Vertical)
        Vector3 moveStep = currentDirection * (RoadGenerator.speed * Time.deltaTime);
        if (!isGrounded)
        {
            moveStep += Vector3.up * (verticalVelocity * Time.deltaTime);
        }
        transform.position += moveStep;

        // 5. Update Current Trail Segment size and position
        UpdateCurrentTrail();
    }

    private bool CheckTurnInput()
{
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
    {
        return false;
    }
    // Fallback for keyboard spacebar
    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;

    // Fallback for mouse left click
    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

    return false;
}

    private void StartGame()
    {
        isPlaying = true;
        isDead = false;
        lastTurnPoint = transform.position;
        SpawnNewTrailSegment();

        PlayMusic();

        Debug.Log("Dancing Line game started!");
    }

    private void PlayMusic()
    {
        if (musicSource == null || musicSource.clip == null) return;

        musicSource.volume = 1f; // 重置音量

        // 💡 核心微调 A：在任何 Play 指令发出之前，先把磁带拨到正确的位置
        if (SettingManager.latency < 0f)
        {
            // 负延迟（例如 -0.5s）：音乐需要裁剪掉开头的 0.5s，从 0.5s 处作为静态起点起播
            float seekTime = Mathf.Abs(SettingManager.latency);
            
            // 安全保护：确保不会切过头
            musicSource.time = Mathf.Min(seekTime, musicSource.clip.length - 0.1f);
            
            // 指针定死后，再踏踏实实开播，WebGL 绝对不会再触发升调、加速
            musicSource.Play();
            Debug.Log($"[WebGL负延迟免打架模式] 音乐已在静止状态对齐至 {musicSource.time:F2}s 并起播");
        }
        else if (SettingManager.latency > 0f)
        {
            // 正延迟（例如 +0.5s）：线条先走，音乐推迟 0.5s 播放
            musicSource.time = 0f;
            musicSource.PlayDelayed(SettingManager.latency);
            Debug.Log($"[正延迟模式] 音乐将在 {SettingManager.latency} 秒后延时播放");
        }
        else
        {
            // 零延迟
            musicSource.time = 0f;
            musicSource.Play();
            Debug.Log("[零延迟模式] 音乐即时起播");
        }
    }
    private void Turn()
    {
        // Finalize current trail segment before turning
        UpdateCurrentTrail();

        // Toggle direction between Forward (+Z) and Right (+X)
        if (currentDirection == Vector3.forward)
        {
            currentDirection = Vector3.right;
        }
        else
        {
            currentDirection = Vector3.forward;
        }

        // Update turn point to current player position
        lastTurnPoint = transform.position;

        // Spawn a new segment for the new direction
        SpawnNewTrailSegment();
    }

    private void HandlePhysicsAndFalling()
    {
        // We start the ray slightly inside the player and project downwards.
        // We exclude the player's own collider by starting the ray below the player's bottom skin.
        Vector3 rayOrigin = transform.position + Vector3.down * (trailHeight * 0.5f - 0.01f);
        
        // Ray length checks just beneath the player's feet
        float rayLength = 0.03f;
        
        if (!isGrounded)
        {
            // If falling, look ahead by the vertical distance we will cover this frame
            rayLength += Mathf.Abs(verticalVelocity * Time.deltaTime);
        }

        RaycastHit hit;
        bool hasHitGround = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength);

        if (hasHitGround)
        {
            if (!isGrounded)
            {
                // LANDING EVENT
                isGrounded = true;
                verticalVelocity = 0f;

                // Snap player position perfectly to the floor height
                Vector3 pos = transform.position;
                pos.y = hit.point.y + (trailHeight * 0.5f);
                transform.position = pos;

                // Start a brand new flat grounded segment from the landing point
                lastTurnPoint = transform.position;
                SpawnNewTrailSegment();
                
                Debug.Log("Landed on floor: " + hit.collider.name + ". Started new trail segment.");
            }
        }
        else
        {
            if (isGrounded)
            {
                // FALLING OFF EDGE EVENT
                isGrounded = false;
                verticalVelocity = 0f; // Start with 0 vertical velocity for perfect horizontal projection

                // Finalize the previous grounded segment exactly at the edge
                UpdateCurrentTrail();

                // Set currentTrailSegment to null so NO trail is rendered/stretched in mid-air
                currentTrailSegment = null;

                Debug.Log("Fell off the platform! Trail paused in mid-air.");
            }

            // Apply gravity
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void SpawnNewTrailSegment()
    {
        if (trailPrefab == null)
        {
            Debug.LogError("Trail Prefab is not assigned in PlayerController!");
            return;
        }

        // Spawn oriented along current horizontal direction
        currentTrailSegment = Instantiate(trailPrefab, transform.position, Quaternion.LookRotation(currentDirection));
        currentTrailSegment.name = $"TrailSegment_{spawnedTrails.Count + 1}";

        // Set initial scale to trailWidth length to fill the corner block perfectly
        currentTrailSegment.transform.localScale = new Vector3(trailWidth, trailHeight, trailWidth);

        spawnedTrails.Add(currentTrailSegment);
    }

    private void UpdateCurrentTrail()
    {
        if (currentTrailSegment == null) return;

        Vector3 currentPos = transform.position;
        float distance = Vector3.Distance(lastTurnPoint, currentPos);
        Vector3 midpoint = (lastTurnPoint + currentPos) * 0.5f;

        // Position the trail segment exactly in the middle between the turning point/edge and the player
        currentTrailSegment.transform.position = midpoint;

        // Orient the segment along the actual vector between lastTurnPoint and player
        // This ensures that when falling, the trail box slants downwards beautifully!
        Vector3 directionVector = currentPos - lastTurnPoint;
        if (directionVector.sqrMagnitude > 0.001f)
        {
            currentTrailSegment.transform.rotation = Quaternion.LookRotation(directionVector);
        }
        else
        {
            currentTrailSegment.transform.rotation = Quaternion.LookRotation(currentDirection);
        }

        // Scale the segment: X=width, Y=height, Z=length (distance + trailWidth for perfect corner overlap)
        currentTrailSegment.transform.localScale = new Vector3(trailWidth, trailHeight, distance + trailWidth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Die("Hit an obstacle (Collision): " + collision.gameObject.name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Obstacle"))
        {
            Die("Hit an obstacle (Trigger): " + other.gameObject.name);
        }
        else if (other.CompareTag("DeathZone"))
        {
            Die("Entered a death trigger zone: " + other.gameObject.name);
        }
    }

    private void Die(string reason)
    {
        if (isDead) return;

        isDead = true;
        isPlaying = false;
        
        Debug.LogWarning("Player Died! Reason: " + reason);

        // Finalize the last trail segment if any
        if (currentTrailSegment != null)
        {
            UpdateCurrentTrail();
        }

        // Smoothly fade out music
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }

        // Trigger GameManager GameOver
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("No GameManager instance found in the scene to notify GameOver!");
        }
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    public IEnumerator LoadAudioAndAssign(string path)
    {
        string url = path;
        if (!url.Contains("://"))
        {
            url = "file://" + url;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // =================================================================
        // 💡 WebGL 网页端：无论默认音频还是新导入，统一走纯字节流 + 内存解调
        // =================================================================
        Debug.Log($"[WebGL音频加载] 统一采用纯字节流请求，消灭默认音频时钟 Bug: {url}");

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                byte[] audioRawData = uwr.downloadHandler.data;
                string extension = System.IO.Path.GetExtension(path).ToLower();

                // 强制走重新导入时的那套“免 Bug 内存解码逻辑”
                AudioClip downloadedClip = WavAndMp3MemoryDecoder.Decode(audioRawData, extension);

                if (downloadedClip != null && downloadedClip.length > 0)
                {
                    if (musicSource != null)
                    {
                        musicSource.clip = downloadedClip;
                        AudioListener.pause = false; 
                        Debug.Log($"[WebGL初始化成功] 默认音频已完美转为内存 clip，彻底免疫升调变调！");
                    }
                }
                else
                {
                    Debug.LogError("[WebGL加载提示] 默认音频解码失败。请确保 StreamingAssets 下的默认音频是标准 .wav 格式！");
                }
            }
            else
            {
                Debug.LogError($"[WebGL网络错误] 默认音频读取失败: {uwr.error}");
            }
        }
#else
        // =================================================================
        // 💡 PC 端 / 编辑器：保持原样，直接走高效的标准多媒体加载
        // =================================================================
        Debug.Log($"[PC/编辑器本地加载] 正在通过标准多媒体句柄请求音频: {url}");

        AudioType audioType = GetAudioType(path);

        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(uwr);
                if (musicSource != null && downloadedClip != null)
                {
                    musicSource.clip = downloadedClip;
                    Debug.Log($"[PC/编辑器起播成功] 成功导入音乐: {downloadedClip.name}，时长: {downloadedClip.length} 秒");
                }
            }
            else
            {
                Debug.LogError($"[PC/编辑器加载失败] {uwr.error}");
            }
        }
#endif
    }
    // 辅助函数：根据文件扩展名自动判定 AudioType
    public AudioType GetAudioType(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".mp3" => AudioType.MPEG,
            ".wav" => AudioType.WAV,
            ".ogg" => AudioType.OGGVORBIS,
            _ => AudioType.UNKNOWN,
        };
    }

    public void SetIsDead(bool isDead)
    {
        this.isDead = isDead;
    }

    public List<GameObject> GetSpawnedTrails()
    {
        return spawnedTrails;
    }

    public void SetCurrentDirection(Vector3 direction)
    {
        currentDirection = direction;
    }
}
