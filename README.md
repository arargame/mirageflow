# Mirage Flow

![Mirage Flow](screenshot.png)

Mirage Flow is a dynamic, logic-based physics puzzle game developed using **MonoGame** framework. The core gameplay revolves around a specialized cellular automata-based sand falling physics engine and color grouping algorithms. Players must extract specific colored sand particles (pixels) from a granular image to solve the puzzle.

## 🚀 Features

- **Cellular Automata Physics Engine:** Custom-built data-oriented implementation (1D Array of structs) for high-performance falling sand simulation.
- **Dynamic Level Generation:** The game dynamically loads an image (`.png`) and translates it pixel-by-pixel into resting sand particles without compromising its original visual layout.
- **Color Quantization Filter:** Before spawning, an algorithmic color-distance filter merges thousands of unique picture shades into definitive base palette colors, automatically limiting required buckets.
- **Auto-Scaling UX Grid:** Calculates the required bucket capacities by counting the occurrences of each color and perfectly maps them onto a dynamic bottom-grid layout.
- **Moving Conveyor Mechanics:** Tap-to-place buckets slide across an active conveyor belt, continuously scanning bounding-box coordinates to absorb cascading sand directly underneath the physics grid.
- **Debug & Time Mechanics:** Integrated overlay tracking dynamic FPS, active sand-grain count, unique RGB palette counts, alongside a score-calculating timer upon level completion.

## ⚙️ Technologies Used

- **C# / .NET 9.0**
- **MonoGame Framework** (DesktopGL)
- **State Pattern** (IScreen, ScreenManager for scene handling)

## 🎮 How to Play

1. Run the game natively on Desktop.
2. The game will actively parse the `.png` level into sleeping sand particles.
3. Tap on a matching colored bucket from the lower grid to drop it onto the conveyor belt.
4. As the bucket slides, it will absorb the physics grains matching its color directly above it, triggering gravity across the resting sand.
5. Absorb all active particles into buckets to clear the level and achieve a high score based on your speed!

## 🏃‍♂️ How to Run

Navigate to the project root and start the application via the .NET CLI:
```bash
cd MirageFlow.Desktop
dotnet run
```
