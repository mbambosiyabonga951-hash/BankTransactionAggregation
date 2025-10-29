using System.Net.Http.Json;
using System.Net.Http;
using Confluent.Kafka;
using Aggregator.Application.Abstractions;
using Aggregator.Domain.Entities; 
using Shared.Models;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
// <-- Add this using directive

var builder = Host.CreateApplicationBuilder(args);
var cs=builder.Configuration.GetConnectionString("DefaultConnection")??"Server=localhost,1433;Database=aggregator;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
builder.Services.AddSingleton<Aggregator.Infrastructure.IDbConnectionFactory>(_=>new Aggregator.Infrastructure.SqlConnectionFactory(cs));
builder.Services.AddScoped<ITransactionsRepository, Aggregator.Infrastructure.TransactionsRepository>();
builder.Services.AddScoped<IFraudStore, Aggregator.Infrastructure.FraudStore>();
builder.Services.AddSingleton<IConsumer<string,string>>(_=>{ var cfg=new ConsumerConfig{ BootstrapServers=builder.Configuration["Kafka:BootstrapServers"]??"localhost:9092", GroupId="aggregator-worker", AutoOffsetReset=AutoOffsetReset.Earliest, EnableAutoCommit=true }; return new ConsumerBuilder<string,string>(cfg).Build(); });
builder.Services.AddHostedService<Worker>(); builder.Services.AddHttpClient("fraud", c=> c.BaseAddress=new Uri(builder.Configuration["FraudScoring:BaseUrl"]??"http://localhost:5010"));
var app = builder.Build(); await app.RunAsync();

class Worker:BackgroundService
{
    private readonly ILogger<Worker> _log;
    private readonly IConsumer<string,string> _consumer; 
    private readonly ITransactionsRepository _repo; 
    private readonly IFraudStore _fraud; 
    private readonly IHttpClientFactory _http;
    public Worker(ILogger<Worker> log, IConsumer<string,string> consumer, ITransactionsRepository repo, IFraudStore fraud, IHttpClientFactory http){_log=log;_consumer=consumer;_repo=repo;_fraud=fraud;_http=http;}
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var topic=Environment.GetEnvironmentVariable("Kafka__Topic")??"bank-transactions"; _consumer.Subscribe(topic);
        while(!ct.IsCancellationRequested){
            try{
                var cr=_consumer.Consume(ct); if(cr is null) continue;
                var m=JsonSerializer.Deserialize<TransactionMessage>(cr.Message.Value); if(m is null) continue;
                await _repo.UpsertAccountAsync(m.AccountId, m.Country, ct); if(m.MerchantId.HasValue) await _repo.UpsertMerchantAsync(m.MerchantId.Value, m.Country, ct);
                var txn=new Transaction{ AccountId=m.AccountId, MerchantId=m.MerchantId, Amount=m.Amount, Currency=m.Currency, Country=m.Country, DeviceId=m.DeviceId, CreatedUtc=m.CreatedUtc };
                var txnId=await _repo.InsertAsync(txn, ct);
                var (cnt,sum)=await _repo.GetLast24hAsync(m.AccountId, ct);
                var features=new Dictionary<string,object>{{"txn_amount",m.Amount},{"txn_hour",m.CreatedUtc.Hour},{"count_24h",cnt},{"sum_24h",sum},{"country",m.Country}};
                var http=_http.CreateClient("fraud"); var resp=await http.PostAsJsonAsync("/api/fraud/score", new FraudFeaturesDto(txnId,m.AccountId,features), ct);
                var result=await resp.Content.ReadFromJsonAsync<FraudScoreDto>(cancellationToken: ct) ?? new FraudScoreDto("baseline",0.01,new[]{"default"});
                var decision = result.Score>=0.90?"block": result.Score>=0.75?"review":"allow";
                await _fraud.InsertFeaturesAsync(txnId, m.AccountId, JsonSerializer.Serialize(features), ct);
                await _fraud.InsertScoreAsync(txnId, result.ModelVersion, result.Score, result.ReasonCodes, ct);
                await _fraud.InsertDecisionAsync(txnId, decision, 0.75, ct);
                _log.LogInformation("Txn {id} score {s} -> {d}", txnId, result.Score, decision);
            } catch(OperationCanceledException){} catch(Exception ex){ _log.LogError(ex, "Worker error"); await Task.Delay(500, ct); }
        }
    }
}
