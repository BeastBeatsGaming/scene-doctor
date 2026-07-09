using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class MissingReferencesCheck : SceneCheck
    {
        public override string Id => "missing-references";
        public override string DisplayName => "Missing (broken) references";
        public override string Category => "Scene";
        public override string Description => "Serialized fields that pointed at an object/asset which has since been deleted.";

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

                    // null value BUT a stored instance id => the link is broken, as opposed to simply being left empty (None).
                    if (p.objectReferenceValue == null && p.objectReferenceEntityIdValue != EntityId.None)
                    {
                        results.Add(new Diagnostic(Id, DiagnosticSeverity.Error,
                            $"'{mb.gameObject.name}' \u2192 {mb.GetType().Name}.{p.displayName} is broken",
                            "This field was assigned to something that no longer exists. It will throw a " +
                            "NullReferenceException at runtime \u2014 reassign or clear it.",
                            mb));
                    }

                }
            }

            return results;
        }
    }
}
