using System.Collections.Generic;
using UnityEditor;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class MissingScriptsCheck : SceneCheck
    {
        public override string Id => "missing-scripts";
        public override string DisplayName => "Missing scripts";
        public override string Category => "Scene";
        public override string Description => "GameObjects with a component whose script is missing (the yellow 'Missing (Mono Script)' bar).";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            foreach (var go in context.AllGameObjects)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing <= 0) continue;

                var target = go;
                yield return new Diagnostic(Id, DiagnosticSeverity.Error,
                    $"'{go.name}' has {missing} missing script{(missing > 1 ? "s" : "")}",
                    "A component points at a script that no longer exists (renamed, moved or deleted). " +
                    "Reassign the correct script, or remove the empty component.",
                    target)
                {
                    FixLabel = "Remove missing",
                    Fix = () =>
                    {
                        Undo.RegisterCompleteObjectUndo(target, "Remove Missing Scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
                        EditorUtility.SetDirty(target);
                    }
                };
            }
        }
    }
}
