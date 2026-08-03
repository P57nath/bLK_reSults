# University Student Results Management & Blockchain Verification System
### Build Brief for Claude Code — Learning-Mode Enabled

---

## HOW TO USE THIS FILE

1. Save this file as `PROMPT.md` in the root of your blank project folder.
2. Open that folder in VS Code, open the integrated terminal, and run `claude`.
3. Start with:
   > Read PROMPT.md fully, then follow it starting with Phase 0. Do not skip the confirmation checkpoints.
4. Answer the questions it asks at each checkpoint (see "Decision Checkpoints" below — skim these now so you're not caught off guard).
5. After each phase, tell it `continue to next phase` once you've reviewed the output. Don't let it barrel through all 7 phases unattended — the checkpoints only work if you actually read what it asks.

---

## ROLE DEFINITION (paste-through context for Claude Code)

You are acting as a senior Enterprise .NET Solution Architect **and** a Web3/Solidity engineer, pair-programming with a developer who knows .NET reasonably well but is learning blockchain integration from first principles on a real, non-toy codebase. Your two jobs are equally important:

1. **Ship a working, non-placeholder system.**
2. **Teach as you build** — every time you introduce a blockchain-specific concept (hashing for integrity, gas, nonces, ABI, RPC endpoints, wallets/private keys, transaction receipts, event logs, immutability guarantees vs. what blockchain does *not* guarantee), explain it briefly in plain language, either as a code comment or in `LEARNING_NOTES.md`. Don't over-explain things that are already ordinary .NET (that part should move fast).

---

## GROUND RULES (non-negotiable)

1. **Never silently choose between two reasonable stack options.** If you're unsure which of two valid approaches to take, stop and ask me directly, in plain language, with your recommendation stated. See the Decision Checkpoints list — treat that as a minimum, not an exhaustive list.
2. **Work phase by phase.** Finish a phase, summarize what you built and why, then stop and wait for me to say "continue" before starting the next phase. Do not chain all phases into one uninterrupted run.
3. **No placeholder/fake logic**, except the one explicitly-scoped mock: authentication/role simulation in Phase 4 (a real Identity provider is out of scope for this learning project — but say so out loud when you build it, don't just quietly stub it).
4. **Every file that touches blockchain logic gets a short header comment** explaining what it does and why it's structured that way (e.g., why we hash client-side before anchoring, why we store only the hash on-chain and not the full record).
5. **Maintain `LEARNING_NOTES.md`** — append a short entry every time a new blockchain/Web3 concept is introduced. This is the single most important deliverable of this project for me; treat it as seriously as the code.
6. **Prove it runs.** Every phase that produces runnable code ends with you telling me the exact commands to build/run/test it, and what output I should expect to see.

---

## DECISION CHECKPOINTS — confirm before writing code

These are real compatibility/design forks, not busywork. Ask me about each before committing to an approach:

| # | Decision | Why it's not obvious |
|---|----------|----------------------|
| 1 | **Front end:** ASP.NET MVC 5 vs Web Forms | MVC is the more "learnable" and testable choice for BLL/DAL separation; Web Forms is more legacy-realistic if this needs to mimic an actual old university system. State your recommendation, but ask. |
| 2 | **Local database:** SQL Server LocalDB vs SQLite vs in-memory repository | Affects setup friction. LocalDB is more "industrial .NET" but requires SQL Server tooling; SQLite is lighter for a learning sandbox. |
| 3 | **Nethereum version pinning** | Nethereum targets `netstandard2.0`, which *is* consumable from .NET Framework 4.8, but only if the TFM/binding-redirect setup is done correctly (BouncyCastle and Newtonsoft.Json version conflicts are the classic failure mode here). Confirm the exact Nethereum version you intend to pin in `packages.config` and check it against 4.8 compatibility before scaffolding — don't assume the newest version "just works." |
| 4 | **Local chain tooling:** Ganache (GUI or CLI) vs Hardhat node | Hardhat is Node.js/JS tooling sitting outside the .NET solution; Ganache CLI is lighter weight for someone who just wants an RPC endpoint at `127.0.0.1:8545` without learning a JS build tool on top of everything else. Ask which one I have installed or want to install. |
| 5 | **Solidity compilation workflow** | A .NET Framework project has no built-in `solc`. Decide and confirm: compile via Remix IDE and manually paste in the ABI + bytecode, or install Hardhat/solc separately as a one-time step. This decision depends on answer to #4. |
| 6 | **Private key handling for local dev** | Never hardcode a real key. Confirm: encrypted `Web.config` app setting vs. a gitignored local `appSettings.local.config` vs. environment variable read at startup. This is a good moment to teach *why* key management matters even in a sandbox. |
| 7 | **NuGet mode:** `packages.config` vs `PackageReference` in a 4.8 classic project | The brief calls for `packages.config`; confirm that's still what I want once you see how many transitive dependencies Nethereum pulls in (it can be a lot — PackageReference handles transitive resolution more gracefully). |

---

## PHASE 0 — Environment & Tooling Verification (new, do this first)

Before writing anything:
- Confirm installed: .NET Framework 4.8 dev pack, MSBuild or Visual Studio Build Tools, NuGet CLI, Node.js (only if Hardhat is chosen), Ganache (if chosen).
- Confirm I have a wallet/account with test ETH on whichever local chain we pick, and walk me through generating one if not (this is a good first `LEARNING_NOTES.md` entry: how local test accounts differ from mainnet accounts).
- Only after this is confirmed working, move to Phase 1.

---

## PHASE 1 — Architecture & Role Design

Design three roles and a 3-tier structure:

- **Admin** — manages departments, courses, and system users.
- **Registrar/Faculty** — inputs, edits, and officially finalizes ("signs off") student semester results, which triggers the blockchain anchor.
- **External Verifier** (employer/public, anonymous) — public portal to verify a certificate against the blockchain using a Student ID plus a data payload/hash.

Structure:
- **Web layer** (per Checkpoint #1 answer)
- **BLL** — role validation, auditing, hashing orchestration
- **DAL** — local storage (per Checkpoint #2 answer) or SQL scripts

Deliverable: `PLAN.md` describing the exact directory tree you're about to create, and a short explanation of *why* the hash — not the full student record — is what gets anchored on-chain (this is a core Web3-integration concept worth its own `LEARNING_NOTES.md` entry: on-chain data is public and immutable, so you never put PII on-chain, only a fingerprint of it).

---

## PHASE 2 — Smart Contract

Create `Contracts/AcademicRegistry.sol`:

- Maps `string studentId`/`certificateId` → cryptographic hash (bytes32 or string — pick one and explain the tradeoff, e.g. gas cost of string storage vs. bytes32).
- `modifier onlyUniversity` restricting writes to one authorized wallet address, set in the constructor.
- `anchorRecord(string studentId, string dataHash)` — write function, emits an event (explain why emitting events matters for off-chain indexing/auditing).
- `verifyRecord(string studentId) public view returns (string)` — read-only, no gas cost when called via `call` rather than a transaction (explain the difference between a **transaction** and a **call** here — that's a top-tier "aha" moment for blockchain beginners).
- Include NatSpec comments (`/// @notice`, `/// @dev`) — teach me this is Solidity's equivalent of XML doc comments.

Stop here and show me the full contract plus a plain-language walkthrough before moving on.

---

## PHASE 3 — Solution & Project Scaffolding

- Generate `.sln` and `.csproj` files for a .NET Framework 4.8 solution matching the Phase 1 structure, buildable via MSBuild or VS Code + the C# extension.
- Set up `packages.config` (or confirm PackageReference per Checkpoint #7) for `Nethereum.Web3` and `Newtonsoft.Json`, using the version pinned in Checkpoint #3.
- Run an actual `nuget restore` / build after scaffolding and show me it succeeds (or show me the real error and fix it — don't paper over dependency resolution problems, they're one of the most educational parts of this whole exercise).

---

## PHASE 4 — Backend Implementation (C#)

1. `CryptoService.cs` — deterministic SHA-256 hashing:
   - Accepts a student record (ID, Full Name, Course Codes, GPA, Graduation Date).
   - Sorts attributes alphabetically before serializing/hashing (explain *why*: canonical ordering prevents two logically-identical records producing different hashes due to property order — a common real bug in hash-anchoring systems).
   - Outputs a clean lowercase hex string.
   - Fully unit-testable — no static randomness, no timestamps baked into the hash unless explicitly part of the record.

2. `BlockchainService.cs` using Nethereum:
   - Initializes `Web3` against the RPC endpoint from Checkpoint #4/#6.
   - Async call to `anchorRecord` signed with the configured private key — explain gas estimation and what a transaction receipt/hash actually confirms (inclusion in a block, not necessarily "success" — that's `receipt.Status`).
   - Call to `verifyRecord` for read-back comparison.
   - Real error handling: RPC unreachable, insufficient gas/balance, nonce issues — don't swallow these silently.

3. Mock `AuthorizationHelper`/attribute simulating Admin/Faculty/Verifier roles — explicitly labeled as a simplified stand-in for real ASP.NET Identity, per Ground Rule #3.

---

## PHASE 5 — User Portals

1. **Faculty interface**: list student results, "Authorize and Push to Blockchain" action, display the resulting transaction hash (and a link/format showing how you'd view it on a real block explorer if this weren't a local chain).
2. **Verification portal**: employer enters a Student ID + a payload (JSON or raw text), the app recomputes the hash locally via `CryptoService` and compares it against `verifyRecord`'s on-chain value — match/no-match result shown clearly, with an explanation of *why this proves integrity without needing to trust the university's database* (the actual point of the whole system).

---

## PHASE 6 — Local End-to-End Test

Provide a step-by-step local testing guide assuming Ganache or Hardhat running at `http://127.0.0.1:8545`:
- Start the local chain, deploy the contract (note the deployed address I'll need to paste into config), run the web app, walk through: create a result → finalize/anchor it → verify it via the public portal → tamper with one field and show the verification failing.
- This tamper-and-fail walkthrough is the actual demo of the system's value — make sure it's included, not just the happy path.

---

## PHASE 7 — Learning Recap

Finish `LEARNING_NOTES.md` with a short glossary section (hash, nonce, gas, RPC, ABI, wallet/private key, transaction vs. call, event log, immutability) written in your own explanatory style, plus a "what this system does and does not protect against" honesty section (e.g., it proves a record wasn't altered *after* anchoring — it does not prove the registrar entered correct grades in the first place).

---

## FINAL DELIVERABLES CHECKLIST

- [ ] `PLAN.md`
- [ ] `Contracts/AcademicRegistry.sol`
- [ ] `.sln` + `.csproj` files, restorable and buildable
- [ ] `packages.config` with pinned, verified-compatible versions
- [ ] `CryptoService.cs`, `BlockchainService.cs`, mock auth helper
- [ ] Faculty portal + Verification portal (controllers/views)
- [ ] `LEARNING_NOTES.md`
- [ ] Local testing guide (can live inside `PLAN.md` or as its own `TESTING.md`)

---

**Begin with Phase 0.** Do not proceed past any Decision Checkpoint without my explicit answer.
