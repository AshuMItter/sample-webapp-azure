using Microsoft.AspNetCore.Mvc;
using sample_webapp_azure.Models;

namespace sample_webapp_azure.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        //private static readonly string[] Summaries =
        //[
        //    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        //];

        [HttpGet]
        [Route("hello")]
        public string HelloBrother()
        {
            return "Hello World!";
        }

        // generate one endpoint to receive demographics data and return it as response

        // generate a DTO for Demographics
       
        

        [HttpPost("demographics")]
        public ActionResult<Demographics> ReceiveDemographics([FromBody] Demographics demographics)
        {
            if (demographics is null) 
                return BadRequest("Request body is required.");

            return Ok(demographics);
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<Demographics> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new Demographics
            {
                PersonId = index,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Name = $"SomeName{index.ToString()}",
                Address = $"SomeAddress{index.ToString()}",
                AndSoOn = $"SomeValue{index.ToString()}"
            })
            .ToArray();
        }
    }
}
