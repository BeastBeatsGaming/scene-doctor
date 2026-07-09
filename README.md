# Scene Doctor

One-click health check for your Unity scenes. Open **Tools ▸ Scene Doctor**, hit **Scan Scene**, and get a grouped, filterable list of the most common beginner mistakes — many with a one-click fix.

Editor-only. No runtime code ships in your build.

---

## Install

**Option A — Package Manager (from disk)**
1. Window ▸ Package Manager
2. **+** ▸ *Add package from disk…*
3. Select the `package.json` inside `com.saimayank.scene-doctor/`.

**Option B — embedded**
Drop the `com.saimayank.scene-doctor` folder into your project's `Packages/` directory.

Requires Unity **6.4** (6000.4) or newer.

---

## Usage

1. Open the scene you want to check.
2. **Tools ▸ Scene Doctor**.
3. **Scan Scene**.

Each result shows a severity icon, a headline, and a short explanation. Where it makes sense you'll get buttons:

- **Select** — pings and selects the offending object (or script) in the hierarchy/project.
- **Fix** — applies a safe, undoable fix (e.g. *Create EventSystem*, *Remove missing scripts*).

Use the toolbar toggles to filter **Errors / Warnings / Info**, flip to the **Checks** panel to enable or disable individual rules, and turn on **Rescan on save** to re-run automatically every time you save the scene.

---

## What it checks

**Scene**
- **Missing scripts** — components whose script can't be found *(fix: remove them)*.
- **Broken references** — serialized references pointing at deleted objects.
- **Unassigned references** — `None` object references that look like they were meant to be set *(off by default — it's the noisiest rule)*.
- **AudioListener count** — zero (no sound) or more than one (warnings/conflicts).
- **MainCamera tag** — no camera, or cameras present with none / several tagged `MainCamera`.

**UI**
- **Missing EventSystem** — interactive uGUI in the scene but no EventSystem *(fix: create one)*; also flags duplicates.
- **UI outside a Canvas** — uGUI elements with no Canvas above them (they won't render).

**Physics**
- **2D / 3D mismatch** — a `Rigidbody2D` with 3D colliders, or a `Rigidbody` with 2D colliders, on the same object.

**Code**
- **Expensive calls in Update** — heuristic scan of the scripts used in the scene for `GetComponent` / `Find*` / `Camera.main` / `Resources.Load` inside `Update` / `FixedUpdate` / `LateUpdate`.

### Two things worth knowing
- **It scans the open scene(s) only.** Prefabs and assets that aren't instantiated in the loaded scene(s) aren't inspected. Multi-scene editing is supported — every loaded scene is included.
- **The code check is a heuristic, not a compiler.** It blanks out comments/strings and brace-matches before pattern-searching, and only looks at user scripts (under `Assets/`) actually attached in the scene. It can miss cases or, rarely, over-report — treat its results as a prompt to look, not gospel.

---

## Add your own check

Checks are auto-discovered — there's no registry to edit. Derive from `SceneCheck` in any Editor assembly that references `SaiMayank.SceneDoctor.Editor`, and it shows up in the list on the next scan:

```csharp
using System.Collections.Generic;
using UnityEngine;
using SaiMayank.SceneDoctor;

public sealed class NoDisabledCollidersCheck : SceneCheck
{
    public override string Id => "no-disabled-colliders";
    public override string DisplayName => "Disabled colliders";
    public override string Category => "Physics";
    public override string Description => "Colliders that are present but disabled.";

    public override IEnumerable<Diagnostic> Run(SceneContext context)
    {
        foreach (var go in context.AllGameObjects)
        {
            var col = go.GetComponent<Collider>();
            if (col != null && !col.enabled)
            {
                yield return new Diagnostic(Id, DiagnosticSeverity.Info,
                    $"'{go.name}': collider is disabled",
                    "This collider won't participate in physics until it's enabled.",
                    go);
            }
        }
    }
}
```

Optional bits on each `Diagnostic`:
- `Target` — the object to select/ping.
- `FixLabel` + `Fix` — show a fix button; the action should register `Undo` and mark the scene dirty.

Override `EnabledByDefault => false` for noisier rules so they're opt-in.

---

## License

Do what you like with it. Attribution appreciated but not required.
