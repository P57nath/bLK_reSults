using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AcademicRegistry.Blockchain
{
    /// <summary>
    /// Turns a StudentRecord into the fixed-length fingerprint that actually gets anchored
    /// on-chain (see AcademicRegistry.sol's `anchorRecord` and LEARNING_NOTES.md Entry 2 for why
    /// only the hash — never the record itself — ever leaves this process).
    ///
    /// The one rule this class exists to enforce: the SAME logical record must ALWAYS produce the
    /// SAME hash, no matter how it was constructed, what machine computed it, or what order its
    /// fields happen to be in memory. That's "canonical ordering" — every field is written into a
    /// fixed, alphabetically-sorted position before hashing, and every value is formatted with an
    /// explicit, culture-invariant format. Skip either of those and you get the classic
    /// hash-anchoring bug: two people describe the identical record, get two different hashes,
    /// and the verification portal reports tampering that never happened.
    /// </summary>
    public static class CryptoService
    {
        /// <summary>
        /// Computes the lowercase hex SHA-256 hash of a student record's canonical representation.
        /// Deterministic and side-effect free — no clock reads, no randomness, no I/O — so it's
        /// fully unit-testable without a live chain.
        /// </summary>
        public static string ComputeRecordHash(StudentRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            string canonical = BuildCanonicalString(record);
            byte[] canonicalBytes = Encoding.UTF8.GetBytes(canonical);

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(canonicalBytes);
                return ToLowercaseHex(hashBytes);
            }
        }

        /// <summary>
        /// Builds the canonical "Field=Value;Field=Value;..." string that gets hashed. Internal
        /// (not private) so CryptoServiceTests can assert on the exact string, not just the
        /// resulting hash — makes a broken canonicalization change fail with a readable diff
        /// instead of just "hash doesn't match" with no clue why.
        /// </summary>
        internal static string BuildCanonicalString(StudentRecord record)
        {
            // SortedDictionary + Ordinal comparer: field names are sorted alphabetically by their
            // exact byte values, not by any locale's idea of alphabetical order — this is the same
            // "don't let culture leak into a determinism-critical calculation" rule that applies
            // to every value below too.
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                // CourseCodes is a list — list order is not part of the record's identity (a
                // transcript with courses in a different order is the same transcript), so we
                // sort it too before joining, otherwise re-fetching the same courses from a
                // different query would silently change the hash.
                ["CourseCodes"] = string.Join(
                    ",",
                    (record.CourseCodes ?? Array.Empty<string>())
                        .Select(code => (code ?? string.Empty).Trim())
                        .OrderBy(code => code, StringComparer.Ordinal)),
                ["FullName"] = (record.FullName ?? string.Empty).Trim(),
                // "F2" + InvariantCulture: without an explicit format, decimal.ToString() honors
                // the current thread's culture (comma vs period as the decimal separator being
                // the classic failure mode) — same server, different Windows locale, different
                // hash. Locking the format removes that variable entirely.
                ["Gpa"] = record.Gpa.ToString("F2", CultureInfo.InvariantCulture),
                ["GraduationDate"] = record.GraduationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["StudentId"] = (record.StudentId ?? string.Empty).Trim(),
            };

            return string.Join(";", fields.Select(kv => kv.Key + "=" + kv.Value));
        }

        private static string ToLowercaseHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }
}
