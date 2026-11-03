using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpeedStars.Tools.Migration
{
    /// <summary>
    /// A Unity YAML file as text: a preamble and documents ("--- !u!114 &amp;id"). Only what the converter needs (class id, fileID,
    /// top-level key lookup); it never re-emits YAML, so every line it does not touch comes back byte for byte.
    /// </summary>
    public sealed class UnityYamlFile
    {
        private static readonly Regex HeaderPattern = new Regex(@"^--- !u!(\d+) &(-?\d+)( stripped)?\s*$", RegexOptions.Compiled);

        private bool _crlf;
        private bool _endsWithNewline;

        public List<string> Preamble { get; } = new List<string>();
        public List<UnityYamlDoc> Docs { get; } = new List<UnityYamlDoc>();

        public static UnityYamlFile Parse(string text)
        {
            var file = new UnityYamlFile
            {
                _crlf = text.Contains("\r\n"),
                _endsWithNewline = text.EndsWith("\n", StringComparison.Ordinal)
            };

            string[] pieces = text.Split('\n');
            int count = file._endsWithNewline ? pieces.Length - 1 : pieces.Length;
            UnityYamlDoc current = null;
            for (int i = 0; i < count; i++)
            {
                string line = pieces[i];
                if (line.EndsWith("\r", StringComparison.Ordinal)) line = line.Substring(0, line.Length - 1);

                if (line.StartsWith("--- ", StringComparison.Ordinal))
                {
                    current = new UnityYamlDoc(line);
                    file.Docs.Add(current);
                }
                else if (current == null) file.Preamble.Add(line);
                else current.Lines.Add(line);
            }

            return file;
        }

        public string Write()
        {
            string nl = _crlf ? "\r\n" : "\n";
            var lines = new List<string>(Preamble);
            foreach (UnityYamlDoc doc in Docs)
                if (!doc.Removed) lines.AddRange(doc.Lines);
            return string.Join(nl, lines) + (_endsWithNewline ? nl : string.Empty);
        }

        internal static bool TryParseHeader(string line, out int classId, out long fileId, out bool stripped)
        {
            Match m = HeaderPattern.Match(line);
            classId = 0;
            fileId = 0;
            stripped = false;
            if (!m.Success) return false;
            classId = int.Parse(m.Groups[1].Value);
            fileId = long.Parse(m.Groups[2].Value);
            stripped = m.Groups[3].Success;
            return true;
        }
    }

    /// <summary>One YAML document. Lines[0] is the "--- !u!..." header.</summary>
    public sealed class UnityYamlDoc
    {
        public List<string> Lines { get; } = new List<string>();
        public int ClassId { get; }
        public long FileId { get; }
        public bool Stripped { get; }
        public bool Removed { get; set; }

        public UnityYamlDoc(string header)
        {
            Lines.Add(header);
            UnityYamlFile.TryParseHeader(header, out int classId, out long fileId, out bool stripped);
            ClassId = classId;
            FileId = fileId;
            Stripped = stripped;
        }

        /// <summary>Value of a top-level (indent 2) key, trimmed; null when the key is not there.</summary>
        public string Get(string key) => GetAt(key, "  ");

        /// <summary>Value of a key one level deeper (indent 4), e.g. inside m_FontData.</summary>
        public string GetNested(string key) => GetAt(key, "    ");

        private string GetAt(string key, string indent)
        {
            string prefix = indent + key + ":";
            foreach (string line in Lines)
                if (line.StartsWith(prefix, StringComparison.Ordinal)) return line.Substring(prefix.Length).Trim();
            return null;
        }

        /// <summary>GUID of the m_Script reference; null for docs without one.</summary>
        public string ScriptGuid()
        {
            string v = Get("m_Script");
            if (v == null) return null;
            Match m = Regex.Match(v, @"guid: ([0-9a-fA-F]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>fileID inside a "{fileID: N}" value of a top-level key (0 when missing or not a plain reference).</summary>
        public long GetRef(string key)
        {
            string v = Get(key);
            if (v == null) return 0;
            Match m = Regex.Match(v, @"^\{fileID: (-?\d+)\}$");
            return m.Success ? long.Parse(m.Groups[1].Value) : 0;
        }
    }
}
