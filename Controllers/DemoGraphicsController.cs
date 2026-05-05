using Microsoft.AspNetCore.Mvc;
using sample_webapp_azure.Models;

namespace sample_webapp_azure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemographicsController : ControllerBase
    {
        private static List<Demographics> _demographicsDb = new();
      
        private readonly IConfiguration _configuration;

        public DemographicsController(
          
            IConfiguration configuration)
        {
          
            _configuration = configuration;
        }

        // 1. Submit demographics (with validation)
        [HttpPost("submit")]
        public IActionResult SubmitDemographics([FromBody] Demographics data)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(data.Name))
                return BadRequest("Name is required");

            if (string.IsNullOrWhiteSpace(data.Email))
                return BadRequest("Email is required");

            if (!data.Email.Contains("@"))
                return BadRequest("Invalid email format");

            // Calculate age
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - data.DateOfBirth.Year;
            if (data.DateOfBirth > today.AddYears(-age)) age--;

            if (age < 18)
                return BadRequest("Must be 18 years or older");

            // Set metadata
            data.PersonId = _demographicsDb.Count + 1;
            data.SubmittedAt = DateTime.UtcNow;
            data.Status = "Pending";

            // Store (in memory for demo)
            _demographicsDb.Add(data);

            // Return with assigned ID
            return Ok(new
            {
                Message = "Demographics submitted successfully",
                PersonId = data.PersonId,
                Status = data.Status,
                Age = age
            });
        }

        // 2. Get demographics by ID
        [HttpGet("{personId}")]
        public IActionResult GetDemographics(int personId)
        {
            var record = _demographicsDb.FirstOrDefault(d => d.PersonId == personId);
            if (record == null)
                return NotFound($"No record found for PersonId: {personId}");

            return Ok(record);
        }

        // 3. Get all demographics (with optional filtering)
        [HttpGet("all")]
        public IActionResult GetAllDemographics([FromQuery] string? city = null)
        {
            var results = _demographicsDb.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(city))
                results = results.Where(d => d.City?.Equals(city, StringComparison.OrdinalIgnoreCase) == true);

            return Ok(new
            {
                Total = results.Count(),
                Records = results
            });
        }

        // 4. Get statistics (age groups, income brackets)
        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            if (!_demographicsDb.Any())
                return Ok(new { Message = "No data available" });

            var stats = new
            {
                TotalSubmissions = _demographicsDb.Count,
                AverageAge = _demographicsDb.Average(d => CalculateAge(d.DateOfBirth)),
                IncomeBrackets = new
                {
                    Low = _demographicsDb.Count(d => d.AnnualIncome < 50000),
                    Medium = _demographicsDb.Count(d => d.AnnualIncome >= 50000 && d.AnnualIncome < 100000),
                    High = _demographicsDb.Count(d => d.AnnualIncome >= 100000)
                },
                TopCities = _demographicsDb
                    .Where(d => !string.IsNullOrWhiteSpace(d.City))
                    .GroupBy(d => d.City)
                    .Select(g => new { City = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(5)
            };

            return Ok(stats);
        }

        private int CalculateAge(DateOnly birthDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            return age;
        }
    }
}
