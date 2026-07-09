using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaiMayank.SceneDoctor
{
    /// <summary>
    /// A snapshot of every GameObject / MonoBehaviour in the currently loaded
    /// scene(s). Built once per scan and shared by all checks, so each rule
    /// doesn't have to re-walk the hierarchy. Multi-scene editing is supported:
    /// every loaded scene is included.
    /// </summary>
    public sealed class SceneContext
    {
        // All GameObjects, including inactive ones, across loaded scenes.
        public readonly List<GameObject> AllGameObjects = new();

        // All non-null MonoBehaviours across loaded scenes.
        public readonly List<MonoBehaviour> AllMonoBehaviours = new();

        public static SceneContext Build()
        {
            var ctx = new SceneContext();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    // includeInactive: true -> disabled objects get checked too.
                    var transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                        ctx.AllGameObjects.Add(t.gameObject);
                }
            }

            foreach (var go in ctx.AllGameObjects)
            {
                var behaviours = go.GetComponents<MonoBehaviour>();
                foreach (var b in behaviours)
                {
                    // Missing scripts show up here as null; skip them (the MissingScriptsCheck handles those).
                    if (b != null) ctx.AllMonoBehaviours.Add(b);
                }
            }

            return ctx;
        }
    }
}
