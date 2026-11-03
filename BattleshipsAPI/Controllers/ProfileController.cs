using BattleshipsAPI.Attributes;
using BattleshipsAPI.Auth;
using BattleshipsAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BattleshipsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TokenRequired]
    public class ProfileController : ControllerBase
    {
        BattleshipsDBContext db;
        public ProfileController(BattleshipsDBContext battleshipsDBContext)
        {
            db = battleshipsDBContext;
        }
        [HttpGet("me")]
        public ActionResult Me()
        {
            string userLogin = (string)HttpContext.Items["PlayerLogin"]!;
            var user = db.Player.Find(userLogin);
            return Ok(user);
        }
        [HttpPost("changeName")]
        public ActionResult ChangeName([FromBody] string newName)
        {
            if (newName.Length > 20 || string.IsNullOrEmpty(newName))
            {
                return BadRequest("Неправильное имя");
            }
            string userLogin = (string)HttpContext.Items["PlayerLogin"]!;
            var user = db.Player.Find(userLogin)!;
            user.NickName = newName;
            db.SaveChanges();
            return Ok();
        }
        public record PasswordDTO(string OldPassword, string NewPassword);
        [HttpPost("changePassword")]
        public ActionResult ChangePassword([FromBody] PasswordDTO passwords)
        {
            string userLogin = (string)HttpContext.Items["PlayerLogin"]!;
            var user = db.Player.Find(userLogin)!;
            if (user.Password != Hashes.CreateMD5(passwords.OldPassword))
            {
                return Unauthorized("Неправильный пароль");
            }
            user.Password = Hashes.CreateMD5(passwords.NewPassword);
            db.SaveChanges();
            return Ok();
        }
    }
}
