// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

/// @title AcademicRegistry
/// @notice Anchors a cryptographic fingerprint (hash) of a student's academic record on-chain,
///         so anyone can later verify a record hasn't been altered since it was signed off.
/// @dev NatSpec comments (the /// lines) are Solidity's equivalent of C# XML doc comments —
///      tools like Etherscan and IDE plugins render these as documentation automatically.
///      This contract never stores the actual student record, only a hash of it. See
///      LEARNING_NOTES.md Entry 2: the chain is public and permanent, so PII cannot go here.
contract AcademicRegistry {

    /// @notice Maps a studentId (or certificateId) to the SHA-256 hash of their finalized record.
    /// @dev Storage type: string, not bytes32 — this was a real tradeoff, not a default.
    ///      bytes32 is the gas-cheaper option for fixed-length data (a SHA-256 hash is exactly
    ///      32 bytes): it costs one storage slot to write. A string costs more, because Solidity
    ///      has to also store the length and chunk the content into 32-byte slots — writing a
    ///      64-character hex string this way runs noticeably more gas than a bytes32 write.
    ///      We're using string anyway, to keep the hash format identical end-to-end: the exact
    ///      same lowercase hex string produced by CryptoService.cs, with zero bytes<->hex
    ///      conversion on either side of the C#/Solidity boundary. For a learning project, being
    ///      able to eyeball "does the hash I computed match what's on-chain" without a conversion
    ///      step in between is worth the extra gas. A production system anchoring thousands of
    ///      records per semester would likely switch this to bytes32.
    mapping(string => string) private recordHashes;

    /// @notice The single wallet address allowed to anchor records, set once at deployment.
    /// @dev In a real university system this would likely be a multisig, not one EOA (Externally
    ///      Owned Account, i.e. a normal wallet controlled by one private key) — if that one key
    ///      is lost or compromised, so is the ability to anchor records. One address is enough to
    ///      teach the onlyUniversity access-control pattern here.
    address public university;

    /// @notice Emitted every time a record is anchored, so off-chain systems (like this dApp's
    ///         Faculty portal, or a future audit dashboard) can build a history without having to
    ///         re-query contract storage for every studentId that ever existed.
    /// @dev Events are far cheaper to store than contract state, and every node keeps a searchable
    ///      log of them separately from current storage — this is *the* standard way off-chain
    ///      code stays in sync with on-chain activity ("listen for this event" instead of
    ///      "poll storage on a timer"). Note studentId is intentionally NOT marked `indexed`
    ///      here: indexing a dynamic type like string only stores its keccak256 hash in the log's
    ///      searchable topics, not the raw value — you'd be able to filter by it, but not read it
    ///      back out of the log. Since the whole point of this event is a human-readable audit
    ///      trail, we keep it as plain (non-indexed) event data instead.
    event RecordAnchored(string studentId, string dataHash, uint256 timestamp);

    /// @dev Restricts a function to the address recorded as `university` at deployment.
    ///      A modifier is Solidity's version of a method attribute/decorator — the `_;` marks
    ///      where the wrapped function's body actually runs, after the require() check passes.
    modifier onlyUniversity() {
        require(msg.sender == university, "AcademicRegistry: caller is not the authorized university wallet");
        _;
    }

    /// @notice Sets the deploying address as the sole authorized university wallet.
    /// @dev Runs exactly once, automatically, at deployment. This is how a contract records an
    ///      "owner" — there's no ambient identity system on-chain, so whoever's private key signs
    ///      the deployment transaction becomes `msg.sender` here, permanently, unless the contract
    ///      explicitly adds a function to change it (this one doesn't).
    constructor() {
        university = msg.sender;
    }

    /// @notice Anchors (or overwrites) the hash for a given studentId.
    /// @dev This is a state-changing function, so calling it requires sending a **transaction**,
    ///      not a free read-only call. A transaction costs gas because it asks every node on the
    ///      network to update their copy of contract storage and reach consensus on the new
    ///      state — that consensus process is what you're paying for, not "computation" in the
    ///      abstract. The caller must be `university` (enforced by onlyUniversity) and must sign
    ///      the transaction with that wallet's private key; anyone else's transaction reverts.
    /// @param studentId The student or certificate identifier this hash belongs to.
    /// @param dataHash The hex-encoded SHA-256 hash computed off-chain by CryptoService.cs.
    function anchorRecord(string calldata studentId, string calldata dataHash) external onlyUniversity {
        recordHashes[studentId] = dataHash;
        emit RecordAnchored(studentId, dataHash, block.timestamp);
    }

    /// @notice Reads back the currently anchored hash for a studentId.
    /// @dev This is a read-only function (`view`), which is what makes it callable via a
    ///      **call** instead of a transaction — a call costs zero gas and returns instantly,
    ///      because it only asks one node to read its current local state, without touching the
    ///      network's consensus process at all. This is the core "aha" for beginners: *writes*
    ///      cost gas and take a block to confirm, *reads* of already-anchored data are free and
    ///      instant, from any node, by anyone. On the C# side, Nethereum's `CallAsync` (vs
    ///      `SendTransactionAsync`) is what invokes this distinction — see BlockchainService.cs
    ///      in Phase 4.
    /// @param studentId The student or certificate identifier to look up.
    /// @return The hex-encoded hash last anchored for this studentId, or an empty string if none was ever anchored.
    function verifyRecord(string calldata studentId) public view returns (string memory) {
        return recordHashes[studentId];
    }
}
