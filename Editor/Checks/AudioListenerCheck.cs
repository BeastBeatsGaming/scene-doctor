using System.Collections.Generic;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class AudioListenerCheck : SceneCheck
    {
        public override string Id => "audio-listener";
        public override string DisplayName => "AudioListener count";
        public override string Category => "Scene";
        public override string Description => "A scene should have exactly one active AudioListener \u2014 usually on the main camera.";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            var listeners = new List<AudioListener>();
            
            foreach (var go in context.AllGameObjects)
            {
                var l = go.GetComponent<AudioListener>();
                if (l != null) listeners.Add(l);
            }

            if (listeners.Count == 0)
            {
                yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                    "No AudioListener in the scene",
                    "Without an AudioListener you won't hear any positional audio. Add one " +
                    "(typically on the main camera). Ignore this for additively-loaded sub-scenes.");
            }
            else if (listeners.Count > 1)
            {
                foreach (var l in listeners)
                    yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                        $"Extra AudioListener on '{l.gameObject.name}'",
                        "Multiple AudioListeners make Unity log a warning and ignore all but one. " +
                        "Keep a single listener.",
                        l);
            }
        }
    }
}
