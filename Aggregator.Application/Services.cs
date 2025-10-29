using Aggregator.Application.Abstractions;
namespace Aggregator.Application.Services;
public record TransactionSummaryDto(long AccountId,int Count,decimal Total);
public interface ITransactionQueries{Task<IEnumerable<TransactionSummaryDto>> GetSummaryAsync(CancellationToken ct);Task<object?> GetRiskAsync(long transactionId,CancellationToken ct);}
public sealed class TransactionQueries:ITransactionQueries{private readonly ITransactionsRepository _repo; private readonly IFraudStore _fraud; public TransactionQueries(ITransactionsRepository repo, IFraudStore fraud){_repo=repo;_fraud=fraud;} public async Task<IEnumerable<TransactionSummaryDto>> GetSummaryAsync(CancellationToken ct)=> (await _repo.GetSummaryAsync(ct)).Select(r=>new TransactionSummaryDto(r.AccountId,r.Count,r.Total)); public Task<object?> GetRiskAsync(long id,CancellationToken ct)=>_fraud.GetRiskAsync(id,ct);}
