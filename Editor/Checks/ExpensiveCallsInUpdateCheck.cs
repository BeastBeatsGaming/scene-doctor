using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SaiMayank.SceneDoctor.Checks
{
    /// <summary>
    /// Heuristic scan of the C# scripts actually used in the open scene, looking
    /// for costly calls placed inside Update / FixedUpdate / LateUpdate \u2014 the most
    /// common beginner performance mistake. This is a text scan, not a compiler,
    /// so it strips comments/strings and brace-matches but can still miss cases or
    /// occasionally over-report; the UI labels it as heuristic.
    /// </summary>
    public sealed class ExpensiveCallsInUpdateCheck : SceneCheck
    {
        private const string IdConst = "expensive-update-calls";
        public override string Id => IdConst;
        public override string DisplayName => "Expensive calls in Update";
        public override string Category => "Code";
        public override string Description =>
            "Heuristic scan of scripts used in the scene for costly per-frame calls " +
            "(GetComponent / Find / Camera.main / Resources.Load) inside Update/FixedUpdate/LateUpdate.";

        private static readonly Regex MethodHeader =
            new(@"\bvoid\s+(Update|FixedUpdate|LateUpdate)\s*\(\s*\)", RegexOptions.Compiled);

        private readonly struct Pattern
        {
            public readonly Regex Regex;
            public readonly string Label;
            public Pattern(string p, string label)
            {
                Regex = new Regex(p, RegexOptions.Compiled);
                Label = label;
            }
        }

        // Deliberately a tight, low-false-positive set.
        private static readonly Pattern[] Patterns =
        {
            new(@"\bGetComponents?(?:InChildren|InParent)?\s*[<(]", "GetComponent"),
            new(@"\bFindObjects?(?:OfType|ByType)\s*[<(]", "FindObjectOfType"),
            new(@"\bGameObject\.Find\s*\(", "GameObject.Find"),
            new(@"\bFind(?:GameObjectsWithTag|GameObjectWithTag|WithTag)\s*\(", "Find\u2026WithTag"),
            new(@"\bCamera\.main\b", "Camera.main"),
            new(@"\bResources\.Load\s*[<(]", "Resources.Load"),
        };

        private struct Hit
        {
            public int Line;
            public string Method;
            public string Label;
        }

        public override IEnumerable<Diagnostic> Run(SceneContext context)
        {
            var scanned = new HashSet<string>();

            foreach (var mb in context.AllMonoBehaviours)
            {
                var mono = MonoScript.FromMonoBehaviour(mb);
                if (mono == null) continue;

                string path = AssetDatabase.GetAssetPath(mono);
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.StartsWith("Assets/")) continue; // user scripts only, not packages
                if (!path.EndsWith(".cs")) continue;
                if (!scanned.Add(path)) continue;          // scan each file at most once

                string source = mono.text;
                if (string.IsNullOrEmpty(source)) continue;

                var hits = ScanSource(source);
                if (hits.Count == 0) continue;

                yield return BuildDiagnostic(mono, path, hits);
            }
        }

        private static List<Hit> ScanSource(string source)
        {
            var hits = new List<Hit>();
            string blanked = CodeScanUtil.Blank(source);

            foreach (Match header in MethodHeader.Matches(blanked))
            {
                string method = header.Groups[1].Value;
                int after = header.Index + header.Length;

                // Locate the method body's opening brace, skipping abstract members (';') and expression-bodied members ('=>').
                int j = after;
                while (j < blanked.Length && char.IsWhiteSpace(blanked[j])) j++;
                if (j >= blanked.Length) continue;
                if (blanked[j] == ';') continue;
                if (blanked[j] == '=' && j + 1 < blanked.Length && blanked[j + 1] == '>') continue;
                if (blanked[j] != '{') continue;

                int bodyEnd = CodeScanUtil.MatchBrace(blanked, j);
                if (bodyEnd < 0) continue;

                int length = bodyEnd - j + 1;
                foreach (var pat in Patterns)
                {
                    // Regex.Match over a region; m.Index is still relative to the full string, so line numbers line up with the source.
                    var m = pat.Regex.Match(blanked, j, length);
                    while (m.Success)
                    {
                        hits.Add(new Hit
                        {
                            Line = CodeScanUtil.LineAt(source, m.Index),
                            Method = method,
                            Label = pat.Label
                        });
                        m = m.NextMatch();
                    }
                }
            }

            hits.Sort((a, b) => a.Line.CompareTo(b.Line));
            return hits;
        }

        private static Diagnostic BuildDiagnostic(MonoScript mono, string path, List<Hit> hits)
        {
            string fileName = System.IO.Path.GetFileName(path);

            var sb = new StringBuilder();
            sb.Append("These search the scene or allocate every frame. Cache the result in a field in ");
            sb.Append("Awake/Start and reuse it. Heuristic scan \u2014 double-check before changing.");

            const int maxList = 12;
            int shown = System.Math.Min(hits.Count, maxList);

            for (int i = 0; i < shown; i++)
            {
                var h = hits[i];
                sb.Append($"\n\u2022 {h.Method}() line {h.Line} \u2014 {h.Label}");
            }
            
            if (hits.Count > shown)
                sb.Append($"\n\u2022 \u2026and {hits.Count - shown} more");

            string title = hits.Count == 1
                ? $"'{fileName}': expensive call in Update"
                : $"'{fileName}': {hits.Count} expensive calls in Update";

            return new Diagnostic(IdConst, DiagnosticSeverity.Warning, title, sb.ToString(), mono);
        }
    }
}
