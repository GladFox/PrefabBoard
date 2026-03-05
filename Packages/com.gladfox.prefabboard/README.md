# PrefabBoard UPM Package

`com.gladfox.prefabboard` contains the editor implementation of PrefabBoard.

## Installation (Git URL)

In Unity project `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gladfox.prefabboard": "https://github.com/GladFox/PrefabBoard.git?path=/Packages/com.gladfox.prefabboard#main"
  }
}
```

## Scope

- Editor window (`Window/Prefab Board`)
- Board data model and services
- UI Toolkit canvas/cards/groups
- Prefab preview and external drag helpers

## Notes

- Demo-specific assets (boards, prefabs, preview rig settings) are expected in the Demo project `Assets`.
