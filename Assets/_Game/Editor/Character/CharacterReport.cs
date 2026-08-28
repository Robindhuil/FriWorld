using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 1 — Report. Reads everything, writes nothing.
    ///
    /// It never guesses. A name it cannot place is reported, not resolved to something similar —
    /// the same discipline the object type registry runs on, and for the same reason: a silent
    /// near-match is a bug you find months later on one NPC.
    /// </summary>
    public static class CharacterReport
    {
        /// <summary>Runs the checks and returns the error count, so Bake can refuse on it.</summary>
        public static int Run(bool log = true)
        {
            var missing = new List<string>();
            var bodies = CharacterScan.ReadBoth(missing);

            var issues = CharacterValidation.Check(
                CharacterRegistries.LoadClasses(),
                CharacterRegistries.LoadColorways(),
                CharacterRegistries.LoadPresets(),
                bodies);

            var report = new StringBuilder();
            report.AppendLine("Character Report");
            report.AppendLine();

            foreach (string path in missing)
                report.AppendLine($"ERROR MISSING base prefab {path} does not exist");

            int errors = missing.Count;
            int notes = 0;

            foreach (var issue in issues)
            {
                report.AppendLine(issue.ToString());
                if (issue.severity == Severity.Error) errors++;
                else notes++;
            }

            report.AppendLine();
            report.AppendLine($"{errors} errors, {notes} notes, {bodies.Count} bodies scanned.");

            if (log)
            {
                if (errors > 0) Debug.LogError(report.ToString());
                else Debug.Log(report.ToString());
            }

            return errors;
        }
    }
}
