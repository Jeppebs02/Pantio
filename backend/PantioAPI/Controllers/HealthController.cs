using Microsoft.AspNetCore.Mvc;

namespace PantioAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Healthy");
    }
}
