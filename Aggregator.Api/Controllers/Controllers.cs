using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Aggregator.Application.Services;
namespace Aggregator.Api.Controllers;
[ApiController][Route("health")] public class HealthController:ControllerBase{ [HttpGet][AllowAnonymous] public IActionResult Get()=>Ok("OK"); }
[ApiController][Route("api/transactions")][Authorize(Policy="api.read")] public class TransactionsController:ControllerBase
{ private readonly ITransactionQueries _q; public TransactionsController(ITransactionQueries q)=>_q=q;
  [HttpGet("summary")] public async Task<IActionResult> Summary(CancellationToken ct)=>Ok(await _q.GetSummaryAsync(ct));
  [Authorize(Policy="analyst")][HttpGet("{id:long}/risk")] public async Task<IActionResult> Risk(long id,CancellationToken ct)=> (await _q.GetRiskAsync(id,ct)) is { } r ? Ok(r) : NotFound();
}
