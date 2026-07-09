using System.Collections.Generic;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    /// <summary>
    /// uGUI graphics and selectables (Image, Text, Button, ...) only render and
    /// receive input when they live underneath a Canvas. A UI element sitting
    /// outside any Canvas is invisible at runtime \u2014 a classic "why is my button
    /// not showing up?" beginner trap.
    /// </summary>
    public sealed class UiOutsideCanvasCheck : SceneCheck
    {
        public override string Id => "ui-outside-canvas";
        public override string DisplayName => "UI element outside a Canvas";
        public override string Category => "UI";
        public override string Description => "uGUI components that have no Canvas anywhere above them in the hierarchy.";

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            // No uGUI package -> no Graphic/Selectable types exist -> nothing to check.
            if (!UiReflection.UguiAvailable) yield break;

            foreach (var go in context.AllGameObjects)
            {
                if (!HasUiElement(go)) continue;

                // Fine if a Canvas exists on this object or any ancestor.
                if (HasCanvasInParents(go)) continue;

                // Only report the top-most offender in a detached UI subtree, so a
                // misplaced panel full of children produces one diagnostic, not ten.
                var parent = go.transform.parent;
                if (parent != null &&
                    HasUiElement(parent.gameObject) &&
                    !HasCanvasInParents(parent.gameObject))
                {
                    continue;
                }

                yield return new Diagnostic(Id, DiagnosticSeverity.Warning,
                    $"'{go.name}': UI element is not under a Canvas",
                    "uGUI components only render and receive input inside a Canvas. Move this " +
                    "object (and its children) under a Canvas, or add a Canvas component to a parent.",
                    go);
            }
        }

        private static bool HasUiElement(GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                // c can be null when a script reference is missing; skip those.
                if (c != null && UiReflection.IsUiElement(c)) return true;
            }
            
            return false;
        }

        private static bool HasCanvasInParents(GameObject go)
        {
            // includeInactive: inactive parents still count as a valid Canvas root.
            return go.GetComponentInParent<Canvas>(true) != null;
        }
    }
}
