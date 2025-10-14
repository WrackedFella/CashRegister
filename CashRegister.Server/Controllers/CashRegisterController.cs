using CashRegister.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace CashRegister.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CashRegisterController : ControllerBase
    {
        private readonly CashRegisterService _cashRegisterService;
        private readonly ILogger<CashRegisterController> _logger;

        public CashRegisterController(CashRegisterService cashRegisterService, ILogger<CashRegisterController> logger)
        {
            _cashRegisterService = cashRegisterService;
            _logger = logger;
        }

        [HttpPost("calculate-change")]
        public IActionResult CalculateChange([FromBody] string[] transactions)
        {
            _logger.LogInformation("Received calculate-change request with {TransactionCount} transactions from {RemoteIpAddress}", 
                transactions?.Length ?? 0, HttpContext.Connection.RemoteIpAddress);

            try
            {
                var results = _cashRegisterService.CalculateChangeForTransactions(transactions ?? Array.Empty<string>());
                
                _logger.LogInformation("Successfully calculated change for {ResultCount} transactions", results.Length);
                
                return Ok(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating change for transactions");
                throw; // Let the exception middleware handle this
            }
        }
    }
}