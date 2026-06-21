# Project Overview
- **Game Title:** Dancing Line (Prototype)
- **High-Level Concept:** A cube moves at a constant speed on a flat plane, leaving a continuous 3D trail. Pressing the spacebar toggles its movement direction by 90 degrees (alternating between Forward and Right), creating a zig-zag trail.
- **Players:** Single-player.
- **Inspiration / Reference Games:** Dancing Line.
- **Tone / Art Direction:** Minimalist, clean 3D, high-contrast colors (e.g., dark floor, bright neon cube and trail).
- **Target Platform:** PC / Mac (Standalone).
- **Screen Orientation / Resolution:** Landscape (1920x1080).
- **Render Pipeline:** URP (Universal Render Pipeline).

---

# Game Mechanics
## Core Gameplay Loop
1. **Move:** The player cube moves forward automatically at a constant, uniform speed.
2. **Turn:** Pressing Space (or clicking) instantly turns the cube 90 degrees (alternating between $+Z$ and $+X$ directions).
3. **Trail Generation:** As the cube moves, a 3D block (rectangular prism) stretches behind it, originating from the last turning point to the current position. On turn, the current segment is finalized, and a new one starts.
4. **Challenge:** Navigating through narrow paths without falling off or colliding with walls (to be expanded in future stages; this prototype focuses on the core movement and trail generation).

## Controls and Input Methods
- **New Input System:** We will use the pre-configured project-wide `Jump` action (mapped to Spacebar on Keyboard and South Button on Gamepad) from `Assets/InputSystem_Actions.inputactions` to trigger the 90-degree turn.
- **Alternative Input (Optional):** We can also listen to mouse clicks or screen taps to make it playable on mobile or with a mouse.

---

# UI
This core mechanic prototype will run directly in the game scene. We will set up a minimalist HUD (or let it run in pure gameplay mode) to show instructions:
- "Press SPACE to Turn" on-screen text.

---

# Key Asset & Context

### 1. `PlayerController.cs` (Script)
This script handles player movement, listening to input, toggling directions, and updating/spawning trail segments.
- **Key Fields:**
  - `float speed`: Movement speed.
  - `Vector3 directionA = Vector3.forward`: First movement axis ($+Z$).
  - `Vector3 directionB = Vector3.right`: Second movement axis ($+X$).
  - `GameObject trailPrefab`: Prefab for the trail segment (a simple cube).
  - `float trailWidth` & `float trailHeight`: Dimensions of the trail cross-section.
- **Key Methods:**
  - `void Start()`: Initializes input action bindings.
  - `void Update()`: Moves the player and scales/positions the active trail segment.
  - `void Turn()`: Triggered by input. Alternates direction, finalizes the active segment, and spawns a new one.
  - `void UpdateTrailSegment()`: Positions the active trail segment at the midpoint of `startPoint` and current player position, orienting it and scaling its length to match.

### 2. `CameraController.cs` (Script)
A script on the Main Camera to follow the player with a fixed offset, maintaining a clean isometric-like perspective.
- **Key Fields:**
  - `Transform target`: The player cube transform.
  - `Vector3 offset`: Distance from the player.
  - `float smoothTime`: Smoothing factor for camera follow.

### 3. Prefabs & Scene Objects
- **Player Cube:** A simple 3D Cube with a vibrant material.
- **Trail Segment Prefab:** A 3D Cube with pivot at center, used by `PlayerController` to dynamically instantiate segments.
- **Floor:** A large plane or cube acting as the ground.

---

# Implementation Steps

### Step 1: Prepare Scene & Prefabs
- **Description:** 
  1. Open `Assets/Scenes/SampleScene.unity`.
  2. Create a Floor GameObject (e.g., a large 3D Cube at position `(0, -0.5, 0)` with scale `(100, 1, 100)`).
  3. Create a Player GameObject (a simple 3D Cube at `(0, 0.5, 0)`).
  4. Create a Trail Prefab: A simple Cube with a glowing or solid color material. Save it as `Assets/Prefabs/TrailSegment.prefab`.
- **Assigned Role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Implement Player Controller & Trail System
- **Description:** 
  1. Create `Assets/Scripts/PlayerController.cs`.
  2. Bind to the project-wide `Jump` action (`InputSystem.actions.FindAction("Jump")`) to listen for spacebar presses.
  3. On spacebar press, toggle the direction between $+Z$ and $+X$.
  4. On movement, dynamically position, rotate, and scale the active trail segment so it perfectly bridges the previous turning point and the player's current position.
- **Assigned Role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Implement Camera Follow
- **Description:** 
  1. Create `Assets/Scripts/CameraController.cs`.
  2. Attach it to the Main Camera.
  3. Configure it to follow the Player Cube with a smooth, fixed offset (e.g., isometric overhead angle).
- **Assigned Role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** Yes

### Step 4: Fine-Tuning and Visual Polish
- **Description:**
  1. Assign materials to the Player, Floor, and Trail segments.
  2. Adjust lighting and camera angle to match a classic Dancing Line style (high-contrast, clean lines).
  3. Verify that the turn is responsive and the trail scales perfectly without any gaps or misalignments.
- **Assigned Role:** developer
- **Dependencies:** Step 3
- **Parallelizable:** No

---

# Verification & Testing

### 1. Manual Playtest Verification
- **Constant Speed:** Confirm that the player cube moves at a uniform speed without stuttering.
- **90-Degree Turn:** Press Spacebar multiple times. Confirm that the cube turns exactly 90 degrees instantly on press.
- **Seamless Trail:** 
  - Verify that the trail block stretches smoothly as the player moves.
  - Verify that when turning, the old segment stops growing and a new segment starts from the exact turning coordinate.
  - Verify there are no gaps or floating segments at the corners.

### 2. Edge Cases to Check
- **Rapid Clicking / Spamming Space:** Press Space extremely quickly. Ensure the trail segments don't glitch or create disconnected floating boxes. They should form short segments perfectly linked together.
- **Starting State:** Ensure the first trail segment starts exactly at the initial player spawn point.
- **Frame Rate Independence:** Verify movement and trail scaling are smooth and identical regardless of frame rate fluctuations (using `Time.deltaTime` correctly).
