using System;
using System.Text;

namespace SaiMayank.SceneDoctor
{
    /// <summary>
    /// Lightweight text utilities for the heuristic code check. This is NOT a
    /// real C# parser \u2014 it blanks out comments/strings and brace-matches so
    /// simple pattern searches don't trip over literals. Good enough to flag
    /// obvious beginner mistakes; it can still miss cases or (rarely) over-report.
    /// </summary>
    internal static class CodeScanUtil
    {
        /// <summary>
        /// Replaces the contents of comments, string literals and char literals
        /// with spaces. Length and newline positions are preserved exactly, so
        /// character indexes and line numbers stay valid against the original.
        /// </summary>
        public static string Blank(string src)
        {
            var sb = new StringBuilder(src.Length);
            int i = 0;
            int n = src.Length;

            while (i < n)
            {
                char c = src[i];
                char next = i + 1 < n ? src[i + 1] : '\0';

                // Line comment //...
                if (c == '/' && next == '/')
                {
                    while (i < n && src[i] != '\n') { sb.Append(' '); i++; }
                    continue;
                }

                // Block comment /* ... */
                if (c == '/' && next == '*')
                {
                    sb.Append("  ");
                    i += 2;

                    while (i < n && !(src[i] == '*' && i + 1 < n && src[i + 1] == '/'))
                    {
                        sb.Append(src[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    if (i < n) { sb.Append("  "); i += 2; } continue;
                }

                // Verbatim string @"..."
                if (c == '@' && next == '"')
                {
                    sb.Append("  ");
                    i += 2;

                    while (i < n)
                    {
                        if (src[i] == '"')
                        {
                            // "" is an escaped quote inside verbatim strings.
                            if (i + 1 < n && src[i + 1] == '"') { sb.Append("  "); i += 2; continue; }
                            sb.Append(' '); i++; break;
                        }

                        sb.Append(src[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    continue;
                }

                // Regular string "..."
                if (c == '"')
                {
                    sb.Append(' ');
                    i++;

                    while (i < n && src[i] != '"')
                    {
                        if (src[i] == '\\' && i + 1 < n) { sb.Append("  "); i += 2; continue; }
                        sb.Append(src[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    if (i < n) { sb.Append(' '); i++; } continue;
                }

                // Char literal '.'
                if (c == '\'')
                {
                    sb.Append(' ');
                    i++;

                    while (i < n && src[i] != '\'')
                    {
                        if (src[i] == '\\' && i + 1 < n) { sb.Append("  "); i += 2; continue; }
                        sb.Append(src[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    if (i < n) { sb.Append(' '); i++; } continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        // 1-based line number for a character index.
        public static int LineAt(string src, int index)
        {
            int line = 1;
            int max = Math.Min(index, src.Length);
            for (int i = 0; i < max; i++)
                if (src[i] == '\n') line++;
            return line;
        }

        /// <summary>
        /// Given the index of an opening brace, returns the index of the matching
        /// closing brace (inclusive). Assumes <paramref name="blanked"/> has had
        /// strings/comments removed. Returns -1 if unbalanced.
        /// </summary>
        public static int MatchBrace(string blanked, int openIndex)
        {
            int depth = 0;

            for (int i = openIndex; i < blanked.Length; i++)
            {
                if (blanked[i] == '{') depth++;
                else if (blanked[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            
            return -1;
        }
    }
}
