using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class EventSystemCheck : SceneCheck
    {
        public override string Id => "event-system";
        public override string DisplayName => "EventSystem for UI";
        public override string Category => "UI";
        public override string Description => "Interactive uGUI (buttons, toggles, sliders\u2026) needs exactly one EventSystem to receive input.";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            if (!UiReflection.UguiAvailable) yield break;

            var eventSystems = new List<MonoBehaviour>();
            bool hasInteractiveUi = false;

            foreach (var mb in context.AllMonoBehaviours)
            {
                if (UiReflection.EventSystem.IsInstanceOfType(mb)) eventSystems.Add(mb);
                
                if (UiReflection.Selectable != null && UiReflection.Selectable.IsInstanceOfType(mb)) hasInteractiveUi = true;
            }

            if (hasInteractiveUi && eventSystems.Count == 0)
            {
                yield return new Diagnostic(Id, DiagnosticSeverity.Error,
                    "Interactive UI but no EventSystem",
                    "Your scene has buttons / toggles / sliders but no EventSystem, so clicks and " +
                    "keyboard navigation won't register. Add one.",
                    null)
                {
                    FixLabel = "Create EventSystem",
                    Fix = CreateEventSystem
                };
            }

            if (eventSystems.Count > 1)
            {
                foreach (var es in eventSystems)
                    yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                        $"Duplicate EventSystem on '{es.gameObject.name}'",
                        "More than one EventSystem is in the scene. Unity logs a warning and keeps only " +
                        "one active \u2014 delete the extras.",
                        es);
            }
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            go.AddComponent(UiReflection.EventSystem);

            // Prefer the new Input System module if that package is installed.
            if (UiReflection.InputSystemUIInputModule != null)
                go.AddComponent(UiReflection.InputSystemUIInputModule);
            else if (UiReflection.StandaloneInputModule != null)
                go.AddComponent(UiReflection.StandaloneInputModule);

            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
