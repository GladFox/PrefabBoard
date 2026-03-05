# PrefabBoard

PrefabBoard is an editor-only Unity tool for visual prefab organization on an infinite board.

## Repository Layout

- `Packages/com.gladfox.prefabboard`  
  UPM package with PrefabBoard editor code (`Data`, `Services`, `UI`, `Styles`).
- `Demo`  
  Unity demo project consuming the package through local file dependency.

## Demo Package Dependency

`Demo/Packages/manifest.json` includes:

```json
"com.gladfox.prefabboard": "file:../../Packages/com.gladfox.prefabboard"
```
