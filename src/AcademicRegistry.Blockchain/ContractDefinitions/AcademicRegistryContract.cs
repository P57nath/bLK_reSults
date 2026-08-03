using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AcademicRegistry.Blockchain.ContractDefinitions
{
    /// <summary>
    /// Typed C# mirrors of AcademicRegistry.sol's functions and event, used by Nethereum instead
    /// of a raw ABI JSON file. Each [Function]/[Parameter] attribute below encodes exactly what
    /// the Solidity signature says — Nethereum uses reflection over these attributes to build the
    /// ABI-encoded call data itself, so there's no separate ABI JSON to keep in sync by hand.
    /// If these ever drift from AcademicRegistry.sol (a renamed parameter, a changed type), calls
    /// against the real contract fail at the RPC layer with a decoding error — that mismatch is
    /// exactly what NatSpec + keeping this file next to the .sol file is meant to prevent.
    /// </summary>

    [Function("anchorRecord")]
    public class AnchorRecordFunction : FunctionMessage
    {
        [Parameter("string", "studentId", 1)]
        public string StudentId { get; set; }

        [Parameter("string", "dataHash", 2)]
        public string DataHash { get; set; }
    }

    [Function("verifyRecord", "string")]
    public class VerifyRecordFunction : FunctionMessage
    {
        [Parameter("string", "studentId", 1)]
        public string StudentId { get; set; }
    }

    [Event("RecordAnchored")]
    public class RecordAnchoredEventDto : IEventDTO
    {
        [Parameter("string", "studentId", 1, false)]
        public string StudentId { get; set; }

        [Parameter("string", "dataHash", 2, false)]
        public string DataHash { get; set; }

        [Parameter("uint256", "timestamp", 3, false)]
        public BigInteger Timestamp { get; set; }
    }
}
