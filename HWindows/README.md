# Hohong123 Windows

Editor-only umbrella package for custom editor windows within the HCUP suite.

## Structure

The package hosts multiple sub-modules, each with its own asmdef. Sub-modules do not share an assembly — this keeps optional dependencies (e.g., Odin) isolated.

| Sub-module | asmdef | Purpose | Optional deps |
|---|---|---|---|
| `NodeWindow` | `Hohong123.HWindows.NodeWindow.Editor` | GraphView-based editor window base | — |
| `FileBrowser` (planned) | `Hohong123.HWindows.FileBrowser.Editor` | Odin-powered asset browser | Sirenix.OdinInspector |

## NodeWindow (L1)

- Menu: `Window ▸ HWindows ▸ Node Window ▸ Graph Editor`
- `HGraphWindow` — EditorWindow shell
- `HGraphCanvas` — `GraphView` adapter with `GridBackground`, pan, zoom, selection

L1 contains NO nodes, ports, edges, save/load, or ScriptableObject binding. It is the reusable base that domain-specific windows (dialogue tree, skill tree, buff graph — all in `HGame.Editor`) consume.

## Dependencies

- `Hohong123.HUtil` / `Hohong123.HUtil.Editor`
- `Hohong123.HDiagnosis` (HLogger)

## Compatibility

- Tested on Unity 6000.3.11f1
- `package.json` declares `"unity": "2021.3"` to match other HCUP packages
- Uses `UnityEditor.Experimental.GraphView` — version migration happens at the adapter boundary (`HGraphCanvas.cs` inside `NodeWindow/Core/`)

## Boundary Rule

External code must NOT `using UnityEditor.Experimental.GraphView`. Only `HGraphWindow.cs` and `HGraphCanvas.cs` inside `NodeWindow/Core/` may reference that namespace. Consume only the `HWindows.NodeWindow.Editor` C# namespace.

## See also

- Design spec: `docs/superpowers/specs/2026-04-21-hwindows-base-l1-design.md`
- Implementation plan: `docs/superpowers/plans/2026-04-21-hwindows-nodewindow-base-l1.md`
