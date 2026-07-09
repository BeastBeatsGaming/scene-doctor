using System.Collections.Generic;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    public sealed class PhysicsDimensionCheck : SceneCheck
    {
        public override string Id => "physics-dimension";
        public override string DisplayName => "2D / 3D physics mismatch";
        public override string Category => "Physics";
        public override string Description =>
            "A 2D Rigidbody paired with 3D colliders (or vice-versa) on the same object \u2014 the two " +
            "physics systems never interact.";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            foreach (var go in context.AllGameObjects)
            {
                bool rb3d = go.GetComponent<Rigidbody>() != null;
                bool rb2d = go.GetComponent<Rigidbody2D>() != null;
                bool col3d = go.GetComponent<Collider>() != null;
                bool col2d = go.GetComponent<Collider2D>() != null;

                if (rb2d && col3d && !col2d)
                {
                    yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                        $"'{go.name}': Rigidbody2D with a 3D Collider",
                        "A Rigidbody2D only collides with 2D colliders, so this 3D collider is ignored. " +
                        "Swap it for a Collider2D (e.g. BoxCollider2D).",
                        go);
                }
                else if (rb3d && col2d && !col3d)
                {
                    yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                        $"'{go.name}': Rigidbody (3D) with a 2D Collider",
                        "A 3D Rigidbody only collides with 3D colliders, so this 2D collider is ignored. " +
                        "Use a 3D Collider (e.g. BoxCollider), or switch to Rigidbody2D.",
                        go);
                }
            }
        }
    }
}
