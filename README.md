# The Matrix Effect

A console application that renders the iconic *Matrix* digital-rain effect — cascading green glyphs with bright leading heads, fading trails, and random flicker — right in your terminal.

![The Matrix digital rain effect](docs/The_Matrix.gif)

## Features

- **Falling glyph drops** — multiple independent drops per column, each with its own speed.
- **Brightness tiers** — bright-white heads fade through bright green, green, and dark green as trails age.
- **Half-width katakana glyphs** plus digits and symbols, for the authentic look.
- **Random flicker** — dim cells randomly swap glyphs for a living, shimmering feel.
- **Live terminal resize** — the effect re-initializes when you resize the window.
- **Fully configurable** via `MatrixConfig` (glyph set, frame delay, drop density, spawn rate, flicker amount).
- **ANSI/VT rendering** — a single buffered write per frame for smooth, flicker-free output.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download) or later
- A terminal with ANSI/VT escape-sequence support (Windows Terminal, modern PowerShell, most Unix terminals)

## Running

From the repository root:

```bash
dotnet run --project Matrix
```

Press any key to exit.

## Configuration

The effect is driven by `MatrixConfig` in [Matrix/MatrixEffect.cs](Matrix/MatrixEffect.cs). Adjust the values in [Matrix/Program.cs](Matrix/Program.cs) before running:

| Property | Default | Description |
|---|---|---|
| `Glyphs` | katakana + digits + symbols | The pool of characters used for the rain. |
| `MaxBrightness` | `20` | Lifespan of a glyph; higher means longer-lasting trails. |
| `FrameDelayMs` | `60` | Delay between frames in milliseconds; lower is faster. |
| `MaxDropsPerColumn` | `3` | Maximum simultaneous drops in a single column. |
| `SpawnChancePercent` | `3` | Per-frame chance (%) a column spawns a new drop. |
| `FlickerDensity` | `2` | Flicker iterations per frame = window width / this value. |

Example — a faster, denser rain:

```csharp
var config = new MatrixConfig
{
    FrameDelayMs = 40,
    MaxDropsPerColumn = 5,
    SpawnChancePercent = 6,
};
```

## Project structure

```
Matrix/
  Program.cs               Entry point; builds the config and runs the effect.
  MatrixEffect.cs          The active effect: MatrixConfig + MatrixEffect.
  ConsoleMatrixRainEffect.cs   Older task-based implementation (not wired up).
Matrix.sln                 Solution file.
docs/                      Screenshots / recordings for this README (The_Matrix.gif, The_Matrix.png).
```

## Media

The animation above is `docs/The_Matrix.gif`; a static still is available at `docs/The_Matrix.png`. To refresh them, run the effect, capture a new screenshot or short recording, overwrite the files in `docs/`, and commit.
