namespace Shared.Models;
public record TransactionMessage(long TransactionId,long AccountId,long? MerchantId,decimal Amount,string Currency,string Country,string? DeviceId,DateTime CreatedUtc);
public record FraudFeaturesDto(long TransactionId,long AccountId,IDictionary<string, object> Features);
public record FraudScoreDto(string ModelVersion,double Score,string[] ReasonCodes);
