# PLAN.md — Architecture & Directory Structure

## Decisions locked in (Checkpoints #1, #2, and DAL tech)

- **Front end:** ASP.NET MVC 5 (.NET Framework 4.8)
- **Local database:** SQL Server LocalDB
- **Data access:** Entity Framework 6, Code First (migrations own the schema)
- **Local chain:** Ganache CLI, RPC at `127.0.0.1:8545`

## Roles

| Role | Access | Responsibility |
|---|---|---|
| **Admin** | Authenticated (mock) | Manage departments, courses, system users |
| **Registrar/Faculty** | Authenticated (mock) | Input/edit student results, "finalize" a result — this triggers the blockchain anchor |
| **External Verifier** | Anonymous, public | Enter Student ID + payload, get a match/no-match against the on-chain hash |

Role checking is a mock in Phase 4 (Ground Rule #3) — labeled explicitly in code, not a silent stub.

## Directory tree

```
AcademicRegistry/
├── AcademicRegistry.sln
├── PROMPT.md
├── PLAN.md
├── LEARNING_NOTES.md
├── Contracts/
│   └── AcademicRegistry.sol              # Phase 2 — Solidity source, compiled externally
├── src/
│   ├── AcademicRegistry.Web/             # ASP.NET MVC 5 — presentation only
│   │   ├── Controllers/
│   │   │   ├── AdminController.cs
│   │   │   ├── FacultyController.cs      # "Authorize and Push to Blockchain" lives here
│   │   │   ├── VerifyController.cs       # anonymous public verification portal
│   │   │   └── AccountController.cs      # mock role login (Phase 4)
│   │   ├── Views/
│   │   ├── Models/                       # ViewModels only — no domain logic
│   │   ├── App_Start/
│   │   ├── Web.config                    # RPC endpoint, contract address (Checkpoint #6 for key)
│   │   └── AcademicRegistry.Web.csproj
│   │
│   ├── AcademicRegistry.BLL/             # role validation, auditing, hashing orchestration
│   │   ├── Services/
│   │   │   ├── ResultService.cs          # CRUD + finalize workflow for results
│   │   │   ├── AnchorOrchestrator.cs     # ties CryptoService + BlockchainService together
│   │   │   ├── VerificationService.cs    # recompute hash, compare to on-chain value
│   │   │   ├── AuditService.cs
│   │   │   └── AuthorizationHelper.cs    # MOCK role check — explicitly labeled (Ground Rule #3)
│   │   └── AcademicRegistry.BLL.csproj
│   │
│   ├── AcademicRegistry.DAL/             # EF6 Code First against LocalDB
│   │   ├── Entities/                     # Student, Course, Department, Result, User, AuditLog
│   │   ├── AcademicRegistryContext.cs
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   └── AcademicRegistry.DAL.csproj
│   │
│   ├── AcademicRegistry.Blockchain/      # only layer that knows Nethereum exists
│   │   ├── CryptoService.cs              # Phase 4.1 — deterministic SHA-256
│   │   ├── BlockchainService.cs          # Phase 4.2 — Nethereum Web3 wrapper
│   │   ├── ContractDefinitions/          # ABI json, typed function/event DTOs
│   │   └── AcademicRegistry.Blockchain.csproj
│   │
│   └── AcademicRegistry.Tests/
│       ├── CryptoServiceTests.cs         # must run with no live chain — pure hashing logic
│       └── AcademicRegistry.Tests.csproj
│
└── packages/                              # NuGet packages.config restore target
```

Dependency direction: `Web → BLL → DAL` and `Web → BLL → Blockchain`. BLL is the only layer that talks to both DAL and Blockchain — Web never touches Nethereum or EF6 directly. This is what makes "swap LocalDB for something else" or "swap Ganache for a testnet" a one-layer change instead of a rewrite.

## Why only a hash goes on-chain, not the record

The chain (even a local Ganache one, and definitely a real one) is effectively a **public, append-only, permanent database**. Anything written to it:

- Is readable by anyone who can query the RPC endpoint — there's no access control on read.
- Cannot be deleted or edited later, even by the contract owner, unless the contract is specifically coded to allow overwriting (and even then the *old* value is still in history).

Student records contain PII (name, GPA, graduation date) — that can never go on a public ledger, local demo or not; the habit has to be right from day one.

The fix: hash the record client-side (`CryptoService`, canonical field ordering so the same data always produces the same hash — see Phase 4), and anchor **only the hash** via `anchorRecord(studentId, dataHash)`. The full record stays in LocalDB, which has normal access control.

This gets you the actual guarantee the system needs — proof the record hasn't been altered since it was signed off — without ever exposing the record itself. Verification works by recomputing the hash from a payload someone provides and comparing it to the on-chain value: if they match, the payload is provably identical to what was anchored; if not, something changed. The chain never needs to "know" what a GPA is.

---

**Next:** Phase 2 — `Contracts/AcademicRegistry.sol`.
