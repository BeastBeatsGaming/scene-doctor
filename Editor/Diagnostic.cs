using System;

namespace SaiMayank.SceneDoctor
{
    public enum DiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// A single problem reported by a <see cref="SceneCheck"/>.
    /// </summary>
    public sealed class Diagnostic
    {
        /// <summary>Id of the check that produced this (matches <see cref="SceneCheck.Id"/>).</summary>
        public string CheckId;

        // One-line headline shown in bold.
        public string Title;

        // Longer explanation: why it matters / how to fix it.
        public string Detail;

        public DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        // Object to select / ping when the user clicks "Select". May be null.
        public UnityEngine.Object Target;

        // Label for the optional one-click fix button (null = no fix).
        public string FixLabel;

        // The fix action. Should register Undo and mark the scene dirty.
        public Action Fix;

        public bool HasFix => Fix != null && !string.IsNullOrEmpty(FixLabel);

        public Diagnostic() { }

        public Diagnostic(string checkId, DiagnosticSeverity severity, string title, string detail,
            UnityEngine.Object target = null)
        {
            CheckId = checkId;
            Severity = severity;
            Title = title;
            Detail = detail;
            Target = target;
        }
    }
}
