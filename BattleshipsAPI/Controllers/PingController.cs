using BattleshipsAPI.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BattleshipsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TokenRequired]
    public class PingController : ControllerBase
    {
        private ActiveGames ActiveGames;
        public PingController(ActiveGames activeGames)
        {
            ActiveGames = activeGames;
        }
        [HttpGet]
        public ActionResult Get()
        {
            string userLogin = (string)HttpContext.Items["PlayerLogin"]!;
            ActiveGames.UpdatePing(userLogin);
            return Ok();
        }
    }
}
