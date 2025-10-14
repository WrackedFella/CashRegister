using CashRegister.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace CashRegister.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CashRegisterController : ControllerBase
    {
        private readonly CashRegisterService _cashRegisterService;

        public CashRegisterController(CashRegisterService cashRegisterService)
        {
            _cashRegisterService = cashRegisterService;
        }

        [HttpPost("calculate-change")]
        public IActionResult CalculateChange([FromBody] string[] transactions)
        {
            try
            {
                var results = _cashRegisterService.CalculateChangeForTransactions(transactions);
                return Ok(new { results });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}