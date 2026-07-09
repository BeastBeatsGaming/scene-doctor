using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace SaiMayank.SceneDoctor
{
    /// <summary>Discovers, configures and runs every <see cref="SceneCheck"/>.</summary>
    public static class SceneDoctorRunner
    {
        private const string PrefPrefix = "SceneDoctor.Check.";

        private static List<SceneCheck> _checks;

        public static IReadOnlyList<SceneCheck> Checks
        {
            get
            {
                if (_checks == null) Discover();
                return _checks;
            }
        }

        private static void Discover()
        {
            _checks = new List<SceneCheck>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<SceneCheck>())
            {
                if (type.IsAbstract) continue;

                try
                {
                    _checks.Add((SceneCheck)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[Scene Doctor] Could not instantiate check '{type.Name}': {e.Message}");
                }
            }

            _checks = _checks
                .OrderBy(c => c.Category)
                .ThenBy(c => c.DisplayName)
                .ToList();
        }

        public static bool IsEnabled(SceneCheck check) =>
            EditorPrefs.GetBool(PrefPrefix + check.Id, check.EnabledByDefault);

        public static void SetEnabled(SceneCheck check, bool enabled) =>
            EditorPrefs.SetBool(PrefPrefix + check.Id, enabled);

        // Runs all enabled checks; returns diagnostics sorted error-first.
        public static List<Diagnostic> RunAll(out int checksRun)
        {
            var results = new List<Diagnostic>();
            var context = SceneContext.Build();
            checksRun = 0;

            foreach (var check in Checks)
            {
                if (!IsEnabled(check)) continue;
                checksRun++;

                try
                {
                    var diagnostics = check.Run(context);
                    if (diagnostics != null) results.AddRange(diagnostics.Where(d => d != null));
                }
                catch (Exception e)
                {
                    results.Add(new Diagnostic(
                        check.Id,
                        DiagnosticSeverity.Error,
                        $"Check '{check.DisplayName}' threw an exception",
                        e.ToString()));
                }
            }

            // Error -> Warning -> Info.
            return results.OrderByDescending(d => (int)d.Severity).ToList();
        }
    }
}
