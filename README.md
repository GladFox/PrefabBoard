# PrefabBoard

PrefabBoard is an editor-only Unity tool for visual prefab organization on an infinite board.

## Repository Layout

- `Packages/com.gladfox.prefabboard`  
  UPM package with PrefabBoard editor code (`Data`, `Services`, `UI`, `Styles`).
- `Demo`  
  Unity demo project consuming the package through Git UPM dependency.

## Install In Unity (Git URL)

Add to your Unity project `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gladfox.prefabboard": "https://github.com/GladFox/PrefabBoard.git?path=/Packages/com.gladfox.prefabboard#main"
  }
}
```

## Demo Package Dependency

`Demo/Packages/manifest.json` includes:

```json
"com.gladfox.prefabboard": "https://github.com/GladFox/PrefabBoard.git?path=/Packages/com.gladfox.prefabboard#main"
```
