using System;
using System.Collections.Generic;
using AcademicRegistry.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AcademicRegistry.Tests
{
    [TestClass]
    public class CryptoServiceTests
    {
        private static StudentRecord SampleRecord()
        {
            return new StudentRecord
            {
                StudentId = "S12345",
                FullName = "Ada Lovelace",
                CourseCodes = new List<string> { "CS201", "MATH101" },
                Gpa = 3.75m,
                GraduationDate = new DateTime(2026, 5, 15),
            };
        }

        [TestMethod]
        public void ComputeRecordHash_SameRecordTwice_ProducesIdenticalHash()
        {
            string first = CryptoService.ComputeRecordHash(SampleRecord());
            string second = CryptoService.ComputeRecordHash(SampleRecord());

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void ComputeRecordHash_CourseCodeOrderDiffers_ProducesIdenticalHash()
        {
            var record = SampleRecord();
            var reordered = SampleRecord();
            reordered.CourseCodes = new List<string> { "MATH101", "CS201" };

            string hashA = CryptoService.ComputeRecordHash(record);
            string hashB = CryptoService.ComputeRecordHash(reordered);

            Assert.AreEqual(hashA, hashB, "Course list order must not affect the hash — it's not part of the record's identity.");
        }

        [TestMethod]
        public void ComputeRecordHash_OneFieldChanges_ProducesDifferentHash()
        {
            var original = SampleRecord();
            var tampered = SampleRecord();
            tampered.Gpa = 3.76m;

            string originalHash = CryptoService.ComputeRecordHash(original);
            string tamperedHash = CryptoService.ComputeRecordHash(tampered);

            Assert.AreNotEqual(originalHash, tamperedHash);
        }

        [TestMethod]
        public void ComputeRecordHash_ReturnsLowercaseHexOf64Characters()
        {
            string hash = CryptoService.ComputeRecordHash(SampleRecord());

            Assert.AreEqual(64, hash.Length, "SHA-256 is 32 bytes = 64 hex characters.");
            Assert.AreEqual(hash.ToLowerInvariant(), hash, "Hash must already be lowercase.");
            foreach (char c in hash)
            {
                Assert.IsTrue(Uri.IsHexDigit(c), $"'{c}' is not a valid hex digit.");
            }
        }

        [TestMethod]
        public void ComputeRecordHash_NullRecord_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => CryptoService.ComputeRecordHash(null));
        }
    }
}
