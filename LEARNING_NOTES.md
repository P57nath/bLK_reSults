# Learning Notes — Blockchain & Web3 Concepts

Running log of blockchain-specific concepts introduced during this build. Ordinary .NET concepts aren't covered here — only the Web3-specific stuff.

---

## Entry 1 — Local test accounts vs. mainnet wallets (Phase 0)

When Ganache CLI starts, it **auto-generates 10 accounts, each pre-funded with 1000 fake ETH**, and prints their addresses + private keys straight to your terminal. That's the local dev experience: `ganache` → done, funded wallet ready.

This is *only* safe because it's a **local, throwaway chain** with no real value attached:

- **Mainnet wallet**: private key controls real money. Generated offline/in a hardware wallet, never printed to a terminal, never committed to a repo. Losing the key = losing the funds, permanently — no "forgot password" reset on a blockchain.
- **Ganache local account**: private key is deterministic/public knowledge (same keys every time by default, or seeded), funds are fake, the whole chain resets to zero the moment you stop the `ganache` process. Copy-pasting one of these keys into a config file is completely fine.

The important habit to build now, even though it doesn't matter yet: **treat "where does this private key come from" as a question you always ask**, so that the instinct is already there the one time it's a real key (see Checkpoint #6 later, on private key storage). We'll use a Ganache-generated key for this whole project — never a real one.

---

## Entry 2 — Why only a hash goes on-chain (Phase 1)

The blockchain is a **public, permanent, append-only** data store — anyone with the RPC endpoint can read everything ever written, and nothing can truly be deleted, even locally on Ganache the same rules apply (it's simulating the real thing).

That means student PII (name, GPA, graduation date) can **never** be written directly to a contract. What we anchor instead is a **SHA-256 hash** of the record — a fixed-length fingerprint that's:

- **One-way**: you can't reverse a hash back into the original data.
- **Deterministic**: the same input always produces the same hash, so anyone can recompute it later to check for a match.
- **Sensitive to any change**: flip one character in the input (e.g. a GPA from 3.5 to 3.6) and the hash comes out completely different — there's no "close enough."

So the chain only ever sees something like `a1b2c3...` — meaningless without the original record, but provably tied to it. The real record lives in LocalDB behind normal access control. Verification = recompute the hash from a submitted payload, compare it to what's on-chain. Match means "this data is exactly what was signed off on that date." No match means something changed since.

---

## Entry 3 — Contract concepts: transaction vs. call, events, access control, NatSpec (Phase 2)

From `Contracts/AcademicRegistry.sol`:

**Transaction vs. call — the single most important distinction in this whole system.**
- `anchorRecord(...)` changes contract storage, so calling it means sending a **transaction**: signed with a private key, costs **gas**, and has to be included in a block before it's final. Until then it's "pending."
- `verifyRecord(...)` only *reads* storage (`view`), so it's invoked via a **call**: no signature, no gas, no waiting for a block — it just asks one node "what's currently stored here" and gets an instant answer.
- Rule of thumb: if a function can change state, it's a transaction. If it only reads state, it's a call. Nethereum exposes this directly as two different methods (`SendTransactionAsync` vs `CallAsync`) — see Phase 4.

**Gas.** The fee paid (in ETH, or fake ETH on Ganache) to get a transaction included and executed. It scales with how much computation/storage the transaction does — writing a `string` costs more gas than writing a `bytes32` because Solidity has to store the length plus chunk the content (see the contract's mapping comment for the full tradeoff). Calls never cost gas because nothing gets written.

**Events.** `RecordAnchored` is emitted every time a record is anchored. Events are cheap to store and are logged separately from contract storage, in a way every node indexes — that's what lets off-chain code (our Faculty portal, an audit dashboard) "subscribe" to activity instead of polling storage in a loop. Note: we deliberately did *not* mark `studentId` as `indexed` — indexing a dynamic type like `string` only stores its hash in the log's searchable topic, not the actual string, which would make the log un-readable for our purposes.

**Access control (`onlyUniversity` modifier).** Solidity has no built-in login system — `msg.sender` is the only "who is calling this" your contract ever gets, and it's just a wallet address. The `onlyUniversity` modifier checks that address against the one wallet recorded at deployment, and any other caller's transaction reverts (fails, gas still partially spent). This is the entire security model for who's allowed to anchor records.

**NatSpec (`/// @notice`, `/// @dev`).** Solidity's equivalent of XML doc comments — `@notice` explains what a human user/caller needs to know, `@dev` is implementation detail for other developers. Tools like Etherscan's "Read/Write Contract" tabs render `@notice` text directly for end users interacting with a deployed contract.

---

## Entry 4 — Signing locally, gas estimation, and what a receipt actually confirms (Phase 4)

From `BlockchainService.cs`:

**Local signing.** `new Account(privateKeyHex)` wraps the key and signs transactions entirely inside the C# process — the raw private key is never transmitted to Ganache or any RPC node. Only the *signed transaction bytes* go over the wire. This is what "self-custody" means: the node verifies a signature is valid without ever seeing the key that produced it. It's also why the key has to come from somewhere the app controls (Checkpoint #6: an environment variable here) rather than being generated or stored by the node itself.

**Gas estimation happens before sending.** Nethereum's `SendRequestAndWaitForReceiptAsync` calls `eth_estimateGas` first. If the transaction would fail — wrong wallet calling `anchorRecord` (the `onlyUniversity` check), or insufficient balance to cover gas — the node often rejects it right there, before anything is broadcast, surfaced in C# as an `RpcResponseException` carrying the node's own error text (e.g. Ganache echoing back the `require()` revert reason). That's why `BlockchainService` doesn't write a generic "something went wrong" message: the node's message is usually the most specific answer available.

**A receipt confirms inclusion, not success.** `TransactionReceipt.Status` is the field that actually matters: `1` means the transaction executed successfully, `0` means it was mined into a block (so it's real, permanent, and gas was still spent) but *reverted* during execution. Code that only checks "did I get a receipt back" instead of checking `Status` will report false successes — this is the single most common blockchain-integration bug in beginner code, which is why `AnchorRecordAsync` throws explicitly on `Status == 0` instead of returning normally.

---
</content>
