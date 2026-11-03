using BattleshipsAPI.Auth;
using BattleshipsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class PlayersController : ControllerBase
{
    private readonly BattleshipsDBContext db;
    public PlayersController(BattleshipsDBContext context)
    {
        db = context;
    }
    public record PlayerDTO(string Login, string Password);
    [HttpPost("register")]
    public ActionResult Register([FromBody] PlayerDTO playerDTO)
    {
        if (string.IsNullOrEmpty(playerDTO.Login) || string.IsNullOrEmpty(playerDTO.Password))
        {
            return BadRequest("Логин или пароль пустой");
        }
        if (playerDTO.Login.Length > 20)
        {
            return BadRequest("Логин слишком длинный");
        }
        if (playerDTO.Password.Length > 20)
        {
            return BadRequest("Пароль слишком длинный");
        }
        if (db.Player.FirstOrDefault(x => x.Login == playerDTO.Login) is not null)
        {
            return Conflict("Такой логин уже есть");
        }
        db.Player.Add(new Player(playerDTO.Login, Hashes.CreateMD5(playerDTO.Password)));
        db.SaveChanges();
        return StatusCode(201, "Аккаунт создан");
    }
    [HttpPost("authorize")]
    public ActionResult Authorize([FromBody] PlayerDTO playerDTO)
    {
        if (string.IsNullOrEmpty(playerDTO.Login) || string.IsNullOrEmpty(playerDTO.Password))
        {
            return BadRequest("Логин или пароль пустой");
        }
        var player = db.Player.FirstOrDefault(x => x.Login == playerDTO.Login);
        if (player is null)
        {
            return NotFound("Аккаунт не найден");
        }
        if (player.Password != Hashes.CreateMD5(playerDTO.Password))
        {
            return Unauthorized("Пароль неверный");
        }
        return Ok(TokenStorage.CreateToken(player.Login));
    }
}
