using System;
using System.Collections.Generic;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// Id checks shared by the catalog assets. Catalog lookups return the first match, so a duplicate id silently
    /// hides the later entry and an empty id can never be found; OnValidate warns about both in the Editor instead.
    /// </summary>
    internal static class ConfigIdCheck
    {
        /// <summary>Ids used more than once (ordinal; empty ids ignored), each listed once, in first-seen order.</summary>
        public static List<string> FindDuplicates(IEnumerable<string> ids)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            var duplicates = new List<string>();
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!seen.Add(id) && reported.Add(id)) duplicates.Add(id);
            }
            return duplicates;
        }

        /// <summary>Logs one warning per duplicate id and one for entries without an id. Never throws.</summary>
        public static void Warn(UnityEngine.Object context, string label, IEnumerable<string> ids)
        {
            var list = new List<string>(ids);
            int empty = 0;
            foreach (var id in list)
                if (string.IsNullOrWhiteSpace(id)) empty++;

            if (empty > 0)
                UnityEngine.Debug.LogWarning($"{context.name}: {empty} {label} entr{(empty == 1 ? "y has" : "ies have")} no id and cannot be looked up.", context);

            foreach (var id in FindDuplicates(list))
                UnityEngine.Debug.LogWarning($"{context.name}: duplicate {label} id '{id}'; lookups return only the first one.", context);
        }
    }
}
