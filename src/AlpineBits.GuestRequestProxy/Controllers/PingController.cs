using Microsoft.AspNetCore.Mvc;

namespace AlpineBits.GuestRequestProxy.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : Controller
    {
        public IActionResult Index()
        {
            return Content("Pong");
        }
    }
}
