# NebulaGE

A Linux-first game engine designed around native performance, modern tooling, and full developer control.

## Stack

| Layer | Language | Responsibility |
|---|---|---|
| Engine Core | C++ | Rendering, physics, audio, memory, runtime |
| Launcher + Editor | C# + Avalonia UI | All user-facing UI |
| Tooling | Zig | Asset pipeline, shader compiler, build system, project generators |
| Graphics | Vulkan + GLSL → SPIR-V | Native Linux rendering pipeline |

## Repository Layout

```
NebulaGE/
├── launcher/     # C# + Avalonia UI — launcher and editor shell
├── engine/       # C++ engine core
├── tools/        # Zig tooling (asset pipeline, shader compiler, etc.)
├── templates/    # Project templates
└── docs/         # Documentation
```

## Prerequisites

- .NET 8 SDK
- Zig (for tooling — install via ziglang.org)
- CMake 3.25+ (for engine)
- Vulkan SDK (for rendering)

## Getting Started

### Launcher
```bash
cd launcher
dotnet restore
dotnet run --project NebulaLauncher
```

### CLI (via Zig tooling)
```bash
nebula create MyGame
nebula build
nebula run
```

## License

TBD
