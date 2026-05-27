# NebulaGE Project Templates

Templates are used by the launcher and the `nebula create` command to scaffold new projects.

## Available Templates

| Template | Description |
|---|---|
| `empty` | Bare project with no content — just the folder structure and project file |
| `2d-game` | *(coming soon)* 2D starter with basic scene and orthographic camera |
| `3d-game` | *(coming soon)* 3D starter with basic scene and perspective camera |

## Template Structure

Each template directory contains:
- `.gitignore.template` — rendered and written as `.gitignore` in the new project
- `project.nebula.template` — project file template (name/version substituted on creation)
- Any starter asset, scene, or script files

## Adding a Template

1. Create a directory under `templates/`
2. Add a `.gitignore.template` and `project.nebula.template`
3. Register the template name in `launcher/NebulaLauncher/Models/ProjectTemplate.cs`
