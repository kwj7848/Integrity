# Integrity

Unity engineering safety net. Prevent missing references from slipping into Play mode or builds.

## Inspector Reference Check

All `[SerializeField]` private fields must be assigned in the Inspector. If not, Play mode and Build are blocked.

```csharp
public class Player : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;           // must assign
    [SerializeField] private Transform spawnPoint;   // must assign
    [SerializeField, AllowEmpty] private AudioSource sfx; // can be empty
}
```

### Checked
- `[SerializeField]` private fields deriving from `UnityEngine.Object`
- Scenes and prefabs
- User code only (`Assembly-CSharp`)

### Skipped
- `[AllowEmpty]` fields
- `[HideInInspector]` fields
- Value types (`int`, `float`, `bool`, etc.)

## Settings

**Edit → Project Settings → Integrity**

| Setting | Description | Default |
|---------|-------------|---------|
| Enable Validation | Master toggle | On |
| Block Play Mode | Block entering Play mode on missing references | On |
| Block Build | Block build on missing references | On |

Saved to `ProjectSettings/IntegritySettings.json`.

## Install

### Git URL

**Window → Package Manager → + → Add package from git URL...**

```
https://github.com/kwj7848/Integrity.git
```

### Manual

```
cd YourProject/Packages
git clone https://github.com/kwj7848/Integrity.git com.jeanlab.integrity
```

## Requirements

Unity 2021.3+

## License

[MIT](LICENSE.md)
