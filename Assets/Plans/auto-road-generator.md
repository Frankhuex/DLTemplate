# Project Overview
- **Game Title:** Dancing Line (Prototype with Auto-Mapping)
- **High-Level Concept:** An automatic 3D track generator that reads a midi/chart timing file (`Assets/Charts/GrayEfflorescence.txt`) containing timestamps (in ms), and generates a perfectly aligned, seamless 3D zig-zag track in the scene using a set movement speed.
- **Players:** Single-player.
- **Render Pipeline:** URP.

---

# Game Mechanics
## Core Gameplay Loop
1. **Auto-Mapping:** The game imports millisecond turn-stamps, converts them to world coordinates using $S \times \Delta t$ (speed $\times$ time difference), and spawns consecutive $+Z$ and $+X$ horizontal tracks.
2. **Turn Aligning:** On each turn timestamp, the track alternates its direction and creates a perfect 90-degree overlapping corner.
3. **Execution Workflows:**
   - **Edit-Mode Workflow:** An editor inspector button reads the file and spawns static permanent GameObjects into a dedicated group in the Scene instantly.
   - **Play-Mode Workflow:** The road spawns chunk-by-chunk dynamically as the game timer progresses, matching the music timing, allowing copy-paste of temporary objects.

---

# Key Asset & Context

### 1. `RoadGenerator.cs` (Script)
This component reads the text file, parses timestamps, and handles building the road.
- **Key Fields:**
  - `string chartPath = "Assets/Charts/GrayEfflorescence.txt"`: Path to the timing file.
  - `float speed = 6f`: Speed of player/road segment generation.
  - `float pathWidth = 2.0f`, `float pathThickness = 0.5f`: Dimensions of the generated track.
  - `GameObject floorPrefab`: The material/cube prefab used for the floor segment.
  - `GameObject obstaclePrefab`: Optional (red obstacles can be generated at the outside corners).
- **Key Methods:**
  - `List<float> ParseChartTimestamps()`: Parses the file, skips `timegroup` lines, and returns a list of seconds.
  - `void GenerateRoadInEditor()`: Calculates corner positions and instantiates permanent, nested floor segments in Edit Mode.
  - `void ClearGeneratedRoad()`: Deletes previous edits to allow quick rebuilds and speed tuning.

### 2. `RoadGeneratorEditor.cs` (Editor Script)
Adds a custom Inspector interface with a "Generate Road" button and a "Clear Road" button to allow Edit-Mode execution with 1-click.

---

# Implementation Steps

### Step 1: Create RoadGenerator Script
- **Description:** Implement `Assets/Scripts/RoadGenerator.cs` which holds the core mathematical logic for coordinate calculation and segment instantiation.
- **Assigned Role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Implement RoadGenerator Editor Custom Inspector
- **Description:** Implement `Assets/Scripts/Editor/RoadGeneratorEditor.cs` to add 1-click editor buttons for instant Edit-Mode map creation.
- **Assigned Role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Link Scene and Test
- **Description:** Create an empty GameObject `RoadGenerator` in `SampleScene`, attach the script, and assign materials/prefabs. Run the editor button and verify the generated track.
- **Assigned Role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

---

# Verification & Testing
- **Timestamp Accuracy:** Check if timestamps (e.g., $5535$ ms) map exactly to calculated lengths ($L = 6.0 \times 5.535 = 33.21$ units).
- **Overlapping Corners:** Confirm that corners have no visual gaps or Z-fighting, thanks to our symmetric overlap scaling formula.
- **Edit-Mode Persistence:** Verify that clicking "Generate Road" in Edit-Mode populates the Hierarchy with permanent GameObjects that can be saved directly into the scene file without running the game.
