using Microsoft.AspNetCore.Mvc;

namespace carCard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HiController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Hello, world!");
    }
}
