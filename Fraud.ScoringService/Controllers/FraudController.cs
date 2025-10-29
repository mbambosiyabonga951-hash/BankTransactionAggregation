using Microsoft.AspNetCore.Mvc; using Shared.Models;
namespace Fraud.ScoringService.Controllers;
[ApiController][Route("health")] public class HealthController:ControllerBase{ [HttpGet] public IActionResult Get()=>Ok("OK"); }
[ApiController][Route("api/fraud")] public class FraudController:ControllerBase
{
    [HttpPost("score")] public IActionResult Score([FromBody] FraudFeaturesDto dto)
    { double score=0.01; var reasons=new List<string>();
      if(dto.Features.TryGetValue("txn_amount", out var a) && double.TryParse(a.ToString(), out var amt)){ if(amt>400){score+=0.5; reasons.Add("high_amount");} else if(amt>200){score+=0.25; reasons.Add("mid_amount");} }
      if(dto.Features.TryGetValue("count_24h", out var c) && double.TryParse(c.ToString(), out var cnt) && cnt>10){ score+=0.3; reasons.Add("velocity_24h"); }
      if(dto.Features.TryGetValue("country", out var country) && country?.ToString()!="ZA"){ score+=0.2; reasons.Add("country_mismatch"); }
      score=Math.Min(0.99, score); return Ok(new FraudScoreDto("baseline-stub", score, reasons.ToArray())); }
}
