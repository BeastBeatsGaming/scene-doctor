using System;

namespace SaiMayank.SceneDoctor
{
    /// <summary>
    /// Resolves uGUI / Input System types by name so Scene Doctor never has a
    /// hard compile-time dependency on those packages. If a package is absent,
    /// the matching type is null and the related checks simply find nothing.
    /// </summary>
    internal static class UiReflection
    {
        public static readonly Type EventSystem =
            Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");

        public static readonly Type Selectable =
            Type.GetType("UnityEngine.UI.Selectable, UnityEngine.UI");

        public static readonly Type Graphic =
            Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI");

        public static readonly Type StandaloneInputModule =
            Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");

        public static readonly Type InputSystemUIInputModule =
            Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        public static bool UguiAvailable => EventSystem != null;

        public static bool IsUiElement(UnityEngine.Component component)
        {
            if (component == null) return false;
            if (Graphic != null && Graphic.IsInstanceOfType(component)) return true;
            if (Selectable != null && Selectable.IsInstanceOfType(component)) return true;
            return false;
        }
    }
}
