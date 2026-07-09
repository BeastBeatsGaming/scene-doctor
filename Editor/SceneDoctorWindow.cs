using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaiMayank.SceneDoctor
{
    /// <summary>
    /// The Scene Doctor window: one button to scan the open scene(s), a filtered,
    /// grouped list of diagnostics with Select / Fix buttons, and a settings panel
    /// to toggle individual checks. Pure IMGUI to match the rest of the toolkit.
    /// </summary>
    public sealed class SceneDoctorWindow : EditorWindow
    {
        private const string RescanOnSavePref = "SceneDoctor.RescanOnSave";

        [MenuItem("Tools/Scene Doctor")]
        public static void Open()
        {
            var window = GetWindow<SceneDoctorWindow>();
            window.titleContent = new GUIContent("Scene Doctor");
            window.minSize = new Vector2(360, 300);
            window.Show();
        }

        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();
        private bool _hasScanned;
        private int _checksRun;

        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;
        private bool _showSettings;
        private bool _rescanOnSave;
        private bool _pendingRescan;

        private Vector2 _scroll;

        // Styles / icons are built lazily inside OnGUI (EditorStyles is only valid there).
        private bool _stylesReady;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIContent _errorIcon;
        private GUIContent _warnIcon;
        private GUIContent _infoIcon;

        private void OnEnable()
        {
            _rescanOnSave = EditorPrefs.GetBool(RescanOnSavePref, true);
            EditorSceneManager.sceneSaved += OnSceneSaved;
            Scan();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        private void OnSceneSaved(Scene scene)
        {
            if (_rescanOnSave)
            {
                Scan();
                Repaint();
            }
        }

        private void Scan()
        {
            _diagnostics.Clear();
            _diagnostics.AddRange(SceneDoctorRunner.RunAll(out _checksRun));
            _hasScanned = true;
        }

        private void QueueRescan() => _pendingRescan = true;

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
            _sectionStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                margin = new RectOffset(2, 2, 8, 2)
            };
            _errorIcon = EditorGUIUtility.IconContent("console.erroricon");
            _warnIcon = EditorGUIUtility.IconContent("console.warnicon");
            _infoIcon = EditorGUIUtility.IconContent("console.infoicon");

            _stylesReady = true;
        }

        private void OnGUI()
        {
            EnsureStyles();

            int errors = 0, warnings = 0, infos = 0;
            foreach (var d in _diagnostics)
            {
                switch (d.Severity)
                {
                    case DiagnosticSeverity.Error: errors++; break;
                    case DiagnosticSeverity.Warning: warnings++; break;
                    default: infos++; break;
                }
            }

            DrawToolbar(errors, warnings, infos);

            if (_showSettings)
                DrawSettings();
            else
                DrawResults(errors, warnings, infos);

            // All scan triggers from within OnGUI are deferred to here so we never rebuild the list while it's being laid out.
            if (_pendingRescan)
            {
                _pendingRescan = false;
                Scan();
                Repaint();
            }
        }

        private void DrawToolbar(int errors, int warnings, int infos)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Scan Scene", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    QueueRescan();

                GUILayout.Space(6);

                _showErrors = GUILayout.Toggle(_showErrors,
                    new GUIContent($" {errors}", _errorIcon.image), EditorStyles.toolbarButton,
                    GUILayout.Width(46));
                _showWarnings = GUILayout.Toggle(_showWarnings,
                    new GUIContent($" {warnings}", _warnIcon.image), EditorStyles.toolbarButton,
                    GUILayout.Width(46));
                _showInfo = GUILayout.Toggle(_showInfo,
                    new GUIContent($" {infos}", _infoIcon.image), EditorStyles.toolbarButton,
                    GUILayout.Width(46));

                GUILayout.FlexibleSpace();

                bool rescan = GUILayout.Toggle(_rescanOnSave, "Rescan on save", EditorStyles.toolbarButton);

                if (rescan != _rescanOnSave)
                {
                    _rescanOnSave = rescan;
                    EditorPrefs.SetBool(RescanOnSavePref, rescan);
                }

                _showSettings = GUILayout.Toggle(_showSettings, "Checks",
                    EditorStyles.toolbarButton, GUILayout.Width(58));
            }
        }

        private void DrawResults(int errors, int warnings, int infos)
        {
            // Summary line.
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("Press \u201cScan Scene\u201d to check the open scene.",
                    MessageType.Info);

                return;
            }

            if (_diagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No problems found. {_checksRun} check(s) ran on the open scene(s).",
                    MessageType.Info);
                
                return;
            }

            string summary = $"{errors} error(s), {warnings} warning(s), {infos} info \u2014 " +
                             $"{_checksRun} check(s) ran.";
            var summaryType = errors > 0 ? MessageType.Error
                : warnings > 0 ? MessageType.Warning
                : MessageType.Info;
            EditorGUILayout.HelpBox(summary, summaryType);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            bool any = false;
            DiagnosticSeverity? lastSeverity = null;

            foreach (var d in _diagnostics) // already sorted Error -> Warning -> Info
            {
                if (!PassesFilter(d.Severity)) continue;
                any = true;

                if (lastSeverity != d.Severity)
                {
                    lastSeverity = d.Severity;
                    GUILayout.Label(SectionLabel(d.Severity), _sectionStyle);
                }

                DrawRow(d);
            }

            if (!any) EditorGUILayout.HelpBox("Nothing matches the current filter.", MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private string SectionLabel(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => "ERRORS",
                DiagnosticSeverity.Warning => "WARNINGS",
                _ => "INFO",
            };

        }

        private bool PassesFilter(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => _showErrors,
                DiagnosticSeverity.Warning => _showWarnings,
                _ => _showInfo,
            };

        }

        private GUIContent IconFor(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => _errorIcon,
                DiagnosticSeverity.Warning => _warnIcon,
                _ => _infoIcon,
            };

        }

        private void DrawRow(Diagnostic d)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(IconFor(d.Severity).image, GUILayout.Width(18), GUILayout.Height(18));
                    GUILayout.Label(d.Title, _titleStyle);
                    GUILayout.FlexibleSpace();

                    if (d.Target != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(54)))
                    {
                        Selection.activeObject = d.Target;
                        EditorGUIUtility.PingObject(d.Target);
                    }

                    if (d.HasFix && GUILayout.Button(d.FixLabel, EditorStyles.miniButton)) ApplyFix(d);
                }

                if (!string.IsNullOrEmpty(d.Detail)) GUILayout.Label(d.Detail, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void ApplyFix(Diagnostic d)
        {
            try
            {
                d.Fix?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scene Doctor] Fix \u201c{d.FixLabel}\u201d failed: {e}");
            }

            // Re-scan after the list changes; deferred so we don't mutate mid-layout.
            QueueRescan();
        }

        private void DrawSettings()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("\u2190 Back to results", GUILayout.Width(130)))
                    _showSettings = false;

                if (GUILayout.Button("Re-scan now"))
                    QueueRescan();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enabled checks", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            string lastCategory = null;

            foreach (var check in SceneDoctorRunner.Checks) // ordered by Category, then name
            {
                if (check.Category != lastCategory)
                {
                    lastCategory = check.Category;
                    GUILayout.Label(lastCategory, _sectionStyle);
                }

                bool enabled = SceneDoctorRunner.IsEnabled(check);
                bool now = EditorGUILayout.ToggleLeft(check.DisplayName, enabled);
                
                if (now != enabled)
                {
                    SceneDoctorRunner.SetEnabled(check, now);
                    QueueRescan();
                }

                if (!string.IsNullOrEmpty(check.Description))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(18);
                        GUILayout.Label(check.Description, EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
