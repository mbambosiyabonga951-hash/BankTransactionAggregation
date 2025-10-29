using Microsoft.AspNetCore.Mvc; using Confluent.Kafka; using Shared.Models; using System.Text.Json;
namespace BankA.Api.Controllers;
[ApiController][Route("health")] public class HealthController:ControllerBase{ [HttpGet] public IActionResult Get()=>Ok("OK"); }
[ApiController][Route("api/transactions")] public class TransactionsController:ControllerBase
{
    [HttpPost("generate")] public async Task<IActionResult> Generate()
    { var cfg=new ProducerConfig{ BootstrapServers=Environment.GetEnvironmentVariable("Kafka__BootstrapServers")??"localhost:9092"}; var topic=Environment.GetEnvironmentVariable("Kafka__Topic")??"bank-transactions";
      using var p=new ProducerBuilder<string,string>(cfg).Build(); var rnd=new Random(); var now=DateTime.UtcNow;
      for(int i=0;i<25;i++){ var m=new TransactionMessage(0,rnd.Next(1,5),rnd.Next(1,4),(decimal)(rnd.NextDouble()*500+5),"ZAR","ZA","web",now.AddSeconds(-rnd.Next(0,3600))); await p.ProduceAsync(topic, new Message<string,string>{ Key=m.AccountId.ToString(), Value=JsonSerializer.Serialize(m)}); }
      return Ok(new { generated=25, topic }); }
}
