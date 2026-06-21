using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of the cube")]
    public float speed = 9f;

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

    private void Start()
    {
        // Set player local scale to a perfect cube of size trailHeight for a clean look
        transform.localScale = new Vector3(trailHeight, trailHeight, trailHeight);

        // Find and bind project-wide Input Actions
        if (InputSystem.actions != null)
        {
            jumpAction = InputSystem.actions.FindAction("Jump");
            attackAction = InputSystem.actions.FindAction("Attack");
        }
        else
        {
            Debug.LogWarning("Project-wide Input Actions not found. Falling back to direct keyboard spacebar check.");
        }
    }

    private void Update()
    {
        // If dead, do absolutely nothing
        if (isDead) return;

        // 1. Handle Start Game
        if (!isPlaying)
        {
            if (CheckTurnInput())
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
        Vector3 moveStep = currentDirection * (speed * Time.deltaTime);
        if (!isGrounded)
        {
            moveStep += Vector3.up * (verticalVelocity * Time.deltaTime);
        }
        transform.position += moveStep;

        // 5. Update Current Trail Segment size and position
        UpdateCurrentTrail();

        // 6. Check Fall Death Boundary
        if (transform.position.y < fallDeathY)
        {
            Die("Fell out of bounds!");
        }
    }

    private bool CheckTurnInput()
    {
        // Check new Input System actions
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            return true;
        }
        if (attackAction != null && attackAction.WasPressedThisFrame())
        {
            return true;
        }

        // Fallback for keyboard spacebar in case of any issues with actions
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        // Fallback for mouse left click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private void StartGame()
    {
        isPlaying = true;
        isDead = false;
        lastTurnPoint = transform.position;
        SpawnNewTrailSegment();

        // Start playing the music if assigned
        if (musicSource != null)
        {
            musicSource.volume = 1f; // Reset volume to full
            musicSource.time = 0f;   // Seek to beginning
            musicSource.Play();
            Debug.Log("Music started playing!");
        }

        Debug.Log("Dancing Line game started!");
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

    private System.Collections.IEnumerator FadeOutMusic()
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
}
