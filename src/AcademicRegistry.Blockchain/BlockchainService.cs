using System;
using System.Net.Http;
using System.Threading.Tasks;
using AcademicRegistry.Blockchain.ContractDefinitions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace AcademicRegistry.Blockchain
{
    /// <summary>
    /// The only class in this solution that talks to Ganache/Ethereum directly. Wraps Nethereum's
    /// Web3 client around the two contract calls AcademicRegistry.sol exposes: anchorRecord
    /// (a transaction — costs gas, needs a signature) and verifyRecord (a call — free, read-only).
    /// See AcademicRegistry.sol's NatSpec and LEARNING_NOTES.md Entry 3 for the full
    /// transaction-vs-call explanation this class is built around.
    /// </summary>
    public sealed class BlockchainService
    {
        private readonly Web3 _web3;
        private readonly string _contractAddress;

        /// <param name="rpcEndpoint">Ganache RPC URL, e.g. http://127.0.0.1:8545.</param>
        /// <param name="contractAddress">Deployed AcademicRegistry contract address (Phase 6).</param>
        /// <param name="universityPrivateKeyHex">
        /// The university wallet's private key. Checkpoint #6: this is read by the caller from the
        /// ACADEMICREGISTRY_PRIVATE_KEY environment variable, never from a config file — see
        /// Program/Global startup wiring. BlockchainService itself doesn't know or care where the
        /// key came from, it just needs the hex string to sign transactions with.
        /// </param>
        public BlockchainService(string rpcEndpoint, string contractAddress, string universityPrivateKeyHex)
        {
            if (string.IsNullOrWhiteSpace(rpcEndpoint))
            {
                throw new ArgumentException("RPC endpoint is required.", nameof(rpcEndpoint));
            }

            if (string.IsNullOrWhiteSpace(universityPrivateKeyHex))
            {
                throw new ArgumentException(
                    "No private key was provided. Set the ACADEMICREGISTRY_PRIVATE_KEY environment " +
                    "variable to one of the private keys Ganache prints on startup.",
                    nameof(universityPrivateKeyHex));
            }

            _contractAddress = contractAddress;

            // An Account wraps a private key and handles transaction signing locally, in-process —
            // the private key itself is never sent to the RPC node. Only the signed transaction
            // bytes are. This is what "self-custody" means in practice: Ganache never sees the key.
            var account = new Account(universityPrivateKeyHex);
            _web3 = new Web3(account, rpcEndpoint);
        }

        /// <summary>
        /// Signs and sends the anchorRecord transaction, then waits for it to be mined.
        /// This is the expensive, slow path (relative to VerifyRecordAsync): it costs gas and
        /// won't return until the transaction is included in a block.
        /// </summary>
        public async Task<AnchorResult> AnchorRecordAsync(string studentId, string dataHash)
        {
            var transactionHandler = _web3.Eth.GetContractTransactionHandler<AnchorRecordFunction>();
            var function = new AnchorRecordFunction { StudentId = studentId, DataHash = dataHash };

            TransactionReceipt receipt;
            try
            {
                // SendRequestAndWaitForReceiptAsync estimates gas automatically (calling
                // eth_estimateGas) before sending — if the university wallet lacks the ETH to
                // cover that estimate, or the transaction would revert (e.g. wrong wallet, so
                // onlyUniversity fails), the node rejects it right here with a JSON-RPC error
                // instead of silently doing nothing.
                receipt = await transactionHandler.SendRequestAndWaitForReceiptAsync(_contractAddress, function);
            }
            catch (HttpRequestException ex)
            {
                throw new BlockchainAnchorException(
                    $"Could not reach the RPC endpoint. Is Ganache running? Details: {ex.Message}", ex);
            }
            catch (RpcResponseException ex)
            {
                // The node's own error message (from Ganache) usually says exactly what went
                // wrong — "insufficient funds for gas", "nonce too low", or a require() revert
                // reason like "AcademicRegistry: caller is not the authorized university wallet".
                // We surface that message as-is rather than replacing it with something generic.
                throw new BlockchainAnchorException($"Transaction rejected by the node: {ex.Message}", ex);
            }

            // Being included in a block only means the network processed the transaction — it does
            // NOT mean it succeeded. receipt.Status is the actual pass/fail flag: 1 = success,
            // 0 = reverted (and gas was still spent). This is the distinction the brief calls out
            // explicitly: inclusion is not success.
            if (receipt.Status == null || receipt.Status.Value == 0)
            {
                throw new BlockchainAnchorException(
                    $"Transaction {receipt.TransactionHash} was mined but reverted (Status = 0). " +
                    "Check that the signing wallet matches the contract's `university` address.");
            }

            return new AnchorResult(receipt.TransactionHash, receipt.BlockNumber.Value, receipt.GasUsed.Value);
        }

        /// <summary>
        /// Reads back the currently anchored hash for a studentId. A call, not a transaction —
        /// free, instant, no signature required (works even if the caller's wallet has zero ETH).
        /// </summary>
        public async Task<string> VerifyRecordAsync(string studentId)
        {
            var queryHandler = _web3.Eth.GetContractQueryHandler<VerifyRecordFunction>();
            var function = new VerifyRecordFunction { StudentId = studentId };

            try
            {
                return await queryHandler.QueryAsync<string>(_contractAddress, function);
            }
            catch (HttpRequestException ex)
            {
                throw new BlockchainAnchorException(
                    $"Could not reach the RPC endpoint. Is Ganache running? Details: {ex.Message}", ex);
            }
        }
    }

    /// <summary>Successful anchor result — what the Faculty portal shows after a push succeeds.</summary>
    public sealed class AnchorResult
    {
        public AnchorResult(string transactionHash, System.Numerics.BigInteger blockNumber, System.Numerics.BigInteger gasUsed)
        {
            TransactionHash = transactionHash;
            BlockNumber = blockNumber;
            GasUsed = gasUsed;
        }

        public string TransactionHash { get; }
        public System.Numerics.BigInteger BlockNumber { get; }
        public System.Numerics.BigInteger GasUsed { get; }
    }

    /// <summary>
    /// Wraps every failure mode BlockchainService can hit (unreachable RPC, rejected transaction,
    /// mined-but-reverted) so callers get one exception type to handle, with the real cause always
    /// preserved in InnerException — nothing here is ever swallowed.
    /// </summary>
    public sealed class BlockchainAnchorException : Exception
    {
        public BlockchainAnchorException(string message) : base(message) { }
        public BlockchainAnchorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
