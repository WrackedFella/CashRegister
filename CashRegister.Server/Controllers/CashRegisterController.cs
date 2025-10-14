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
        public async Task<IActionResult> CalculateChange(IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file provided or file is empty");
                    return BadRequest(new { error = "No file provided" });
                }

                if (!file.ContentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid file type: {ContentType}", file.ContentType);
                    return BadRequest(new { error = "Invalid file type. Only text files are supported." });
                }

                if (file.Length > 1024 * 1024) // 1MB limit
                {
                    _logger.LogWarning("File too large: {FileSize} bytes", file.Length);
                    return BadRequest(new { error = "File too large. Maximum size is 1MB." });
                }

                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("File is empty or contains only whitespace");
                    return BadRequest(new { error = "File is empty" });
                }

                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0)
                {
                    _logger.LogWarning("No valid lines found in file");
                    return BadRequest(new { error = "No valid transactions found in file" });
                }

                // Validate format of each line
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    
                    if (!line.Contains(','))
                    {
                        _logger.LogWarning("Invalid format on line {LineNumber}: missing comma", i + 1);
                        return BadRequest(new { error = $"Invalid format on line {i + 1}: expected 'amountOwed,amountPaid'" });
                    }
                    
                    var parts = line.Split(',');
                    if (parts.Length != 2)
                    {
                        _logger.LogWarning("Invalid format on line {LineNumber}: too many commas", i + 1);
                        return BadRequest(new { error = $"Invalid format on line {i + 1}: expected exactly one comma" });
                    }
                    
                    if (!decimal.TryParse(parts[0].Trim(), out _) || !decimal.TryParse(parts[1].Trim(), out _))
                    {
                        _logger.LogWarning("Invalid format on line {LineNumber}: non-numeric values", i + 1);
                        return BadRequest(new { error = $"Invalid format on line {i + 1}: amounts must be numeric" });
                    }
                }

                _logger.LogInformation("Processing file with {LineCount} lines from {RemoteIpAddress}", lines.Length, HttpContext.Connection.RemoteIpAddress);

                var results = _cashRegisterService.CalculateChangeForTransactions(lines);
                _logger.LogInformation("Successfully calculated change for {ResultCount} transactions", results.Length);
                
                return Ok(new { results = string.Join(Environment.NewLine, results) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing file upload");
                return StatusCode(500, new { error = "An error occurred while processing the file" });
            }
        }
    }
}