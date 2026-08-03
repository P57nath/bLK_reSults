using System;
using System.Collections.Generic;

namespace AcademicRegistry.Blockchain
{
    /// <summary>
    /// The exact set of fields that get fingerprinted by <see cref="CryptoService"/> and anchored
    /// on-chain as a hash. Deliberately separate from any EF entity in AcademicRegistry.DAL —
    /// this type defines the hashing contract, not the database schema. If a DB column gets added
    /// later that shouldn't affect the on-chain hash (e.g. an internal row ID), the two types are
    /// free to diverge.
    /// </summary>
    public sealed class StudentRecord
    {
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public IReadOnlyList<string> CourseCodes { get; set; }
        public decimal Gpa { get; set; }
        public DateTime GraduationDate { get; set; }
    }
}
