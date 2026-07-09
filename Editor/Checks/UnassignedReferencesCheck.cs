using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class UnassignedReferencesCheck : SceneCheck
    {
        public override string Id => "unassigned-references";
        public override string DisplayName => "Unassigned (None) references";
        public override string Category => "Scene";
        public override string Description =>
            "Object-reference fields left as 'None'. Often a forgotten assignment \u2014 but some are " +
            "optional by design, so this one is the noisiest. Disable it if it gets in your way.";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            var results = new List<Diagnostic>();

            foreach (var mb in context.AllMonoBehaviours)
            {
                using var so = new SerializedObject(mb);
                var p = so.GetIterator();
                bool enter = true;
                
                while (p.NextVisible(enter))
                {
                    enter = true;
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (p.name == "m_Script") continue;

                    // Empty (None): a null value with no stored link, as opposed to a broken link where the target was deleted.
                    if (p.objectReferenceValue == null && p.objectReferenceEntityIdValue == EntityId.None)
                    {
                        results.Add(new Diagnostic(Id, DiagnosticSeverity.Warning,
                            $"'{mb.gameObject.name}' \u2192 {mb.GetType().Name}.{p.displayName} is empty",
                            "This field is set to None. If your script reads it without a null-check, " +
                            "expect a NullReferenceException. Assign it, or guard against null.",
                            mb));
                    }

                }
            }

            return results;
        }
    }
}
