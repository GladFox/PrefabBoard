# PrefabBoard UPM Package

`com.gladfox.prefabboard` contains the editor implementation of PrefabBoard.

## Installation (local file package)

In Unity project `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gladfox.prefabboard": "file:../../Packages/com.gladfox.prefabboard"
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
