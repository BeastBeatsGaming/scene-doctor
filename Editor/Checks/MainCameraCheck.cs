using System.Collections.Generic;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class MainCameraCheck : SceneCheck
    {
        public override string Id => "main-camera";
        public override string DisplayName => "MainCamera tag";
        public override string Category => "Scene";
        public override string Description => "Camera.main only works when exactly one active camera is tagged 'MainCamera'.";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            var cameras = new List<Camera>();
            var tagged = new List<Camera>();

            foreach (var go in context.AllGameObjects)
            {
                var cam = go.GetComponent<Camera>();
                if (cam == null) continue;
                cameras.Add(cam);
                if (go.CompareTag("MainCamera")) tagged.Add(cam);
            }

            if (cameras.Count == 0)
            {
                yield return new Diagnostic(Id, DiagnosticSeverity.Info,
                    "No Camera in the scene",
                    "Nothing will render unless another loaded scene provides one. Safe to ignore for " +
                    "UI-only or additively-loaded scenes.");
                yield break;
            }

            if (tagged.Count == 0)
            {
                yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                    "No camera tagged 'MainCamera'",
                    "Camera.main returns null and will throw if you use it. Tag your primary camera " +
                    "as 'MainCamera'.",
                    cameras[0]);
            }
            else if (tagged.Count > 1)
            {
                foreach (var cam in tagged)
                    yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                        $"'{cam.gameObject.name}' is also tagged 'MainCamera'",
                        "Several cameras share the 'MainCamera' tag. Camera.main returns whichever Unity " +
                        "finds first \u2014 keep the tag on just one.",
                        cam);
            }
        }
    }
}
