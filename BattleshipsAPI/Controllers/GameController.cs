using BattleshipsAPI.Attributes;
using BattleshipsAPI.Hubs;
using BattleshipsAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BattleshipsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TokenRequired]
    public class GameController : ControllerBase
    {
        BattleshipsDBContext db;
        IHubContext<NotificationHub> hub;
        ActiveGames ActiveGames;
        public GameController(BattleshipsDBContext dBContext, IHubContext<NotificationHub> hub, ActiveGames activeGames)
        {
            db = dBContext;
            this.hub = hub;
            ActiveGames = activeGames;
        }
        
        [HttpGet]
        public ActionResult Get()
        {
            return Ok(ActiveGames.Games.Select(x => new GameDTO(x.StartTimeStamp, x.Creator.NickName)));
        }
        [HttpPost]
        public ActionResult Post()
        {
            string userLogin = (string)HttpContext.Items["PlayerLogin"]!;
            if (ActiveGames.IsUserInGame(userLogin))
            {
                return Conflict("Вы уже в игре");
            }
            ActiveGames.CreateGame(db.Player.First(x => x.Login == userLogin));
            return Created();
        }
        [HttpPost("connect")]
        public ActionResult Connect([FromBody] string creatorNickName)
        {
            if (!ActiveGames.ContainsNickName(creatorNickName)) return NotFound();
            if (ActiveGames.ConnectUserToGame(
                db.Player.First(x => x.Login == (string)HttpContext.Items["PlayerLogin"]!),
                creatorNickName)) return Ok();
            else return StatusCode(500);
        }
        public static bool IsValid(List<Boat> boats)
        {
            if (boats == null)
                return false;

            int[] expected = new int[5];
            expected[1] = 4;
            expected[2] = 3;
            expected[3] = 2;
            expected[4] = 1;

            int[] actual = new int[5];
            int[,] owner = new int[10, 10];

            // 0 means unoccupied; boat index + 1 identifies its boat.
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                    owner[y, x] = 0;

            for (int i = 0; i < boats.Count; i++)
            {
                Boat boat = boats[i];
                if (boat == null)
                    return false;

                int minX = Math.Min(boat.X1, boat.X2);
                int maxX = Math.Max(boat.X1, boat.X2);
                int minY = Math.Min(boat.Y1, boat.Y2);
                int maxY = Math.Max(boat.Y1, boat.Y2);

                // Boat must be inside the 10x10 board.
                if (minX < 0 || maxX >= 10 || minY < 0 || maxY >= 10)
                    return false;

                // A boat must be horizontal, vertical, or a single cell.
                if (minX != maxX && minY != maxY)
                    return false;

                int length = Math.Max(maxX - minX, maxY - minY) + 1;
                if (length > 4)
                    return false;

                actual[length]++;

                // Mark every cell occupied by this boat.
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (owner[y, x] != 0)
                            return false; // Overlap.

                        owner[y, x] = i + 1;
                    }
                }
            }

            for (int length = 1; length <= 4; length++)
            {
                if (actual[length] != expected[length])
                    return false;
            }

            // Different boats must not touch, including diagonally.
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (owner[y, x] == 0)
                        continue;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int ny = y + dy;
                            int nx = x + dx;

                            if (ny < 0 || ny >= 10 || nx < 0 || nx >= 10)
                                continue;

                            if (owner[ny, nx] != 0 && owner[ny, nx] != owner[y, x])
                                return false;
                        }
                    }
                }
            }

            return true;
        }
        [HttpPost("boats")]
        public ActionResult PostBoats([FromBody] List<Boat> boats)
        {
            if (boats is null || boats.Count == 0)
                return BadRequest("Битые данные");
            if (!IsValid(boats))
                return BadRequest("Неправильное поле");
            var playerLogin = (string)HttpContext.Items["PlayerLogin"]!;
            var games = ActiveGames.Games.Where(x => x.Creator.Login == playerLogin || x.Opponent.Login == playerLogin).ToList();
            if (games.Count == 0)
                return Forbid("Вы не в игре");
            if (games.Count > 1)
                return Forbid("Вы в 2х играх (как это произошло?)");
            var game = games.First();
            bool isPlayerCreator = game.Creator.Login == playerLogin;
            if (isPlayerCreator)
            {
                if (game.CreatorBoats.Count > 0)
                    return Forbid("Уже есть данные");
                game.CreatorBoats = boats;
            }
            else
            {
                if (game.OpponentBoats.Count > 0)
                    return Forbid("Уже есть данные");
                game.OpponentBoats = boats;
            }
            return Ok();
        }
        [HttpGet("boats")]
        public ActionResult GetBoats()
        {
            var playerLogin = (string)HttpContext.Items["PlayerLogin"]!;
            var games = ActiveGames.Games.Where(x => x.Creator.Login == playerLogin || x.Opponent.Login == playerLogin).ToList();
            if (games.Count == 0)
                return Forbid("Вы не в игре");
            if (games.Count > 1)
                return Forbid("Вы в 2х играх (как это произошло?)");
            var game = games.First();
            bool isPlayerCreator = game.Creator.Login == playerLogin;
            return Ok(isPlayerCreator ? game.CreatorBoats : game.OpponentBoats);
        }
        public record ShotDTO(bool IsHit, bool IsDestroyed, Boat? Boat);
        [HttpPost("shoot/{x}/{y}")]
        public ActionResult PostShoot(int x, int y)
        {
            var playerLogin = (string)HttpContext.Items["PlayerLogin"]!;
            var games = ActiveGames.Games.Where(x => x.Creator.Login == playerLogin || x.Opponent.Login == playerLogin).ToList();
            if (games.Count == 0)
                return Forbid("Вы не в игре");
            if (games.Count > 1)
                return Forbid("Вы в 2х играх (как это произошло?)");
            var game = games.First();
            bool isPlayerCreator = game.Creator.Login == playerLogin;
            var boats = isPlayerCreator ? game.OpponentBoats : game.CreatorBoats;
            var player = isPlayerCreator ? game.Creator : game.Opponent;
            if (game.Steps.Any(c => c.Player == player && c.X == x && c.Y == y))
                return BadRequest();
            game.Steps.Add(new Step(player, x, y));
            var isHitValue = isHit(boats, x, y);
            var isDestroyedValue = isDestroyed(boats, game.Steps.Where(x => x.Player == player).ToList());
            return Ok(new ShotDTO(
                isHitValue,
                isDestroyedValue,
                isDestroyedValue ? boats.First(c => isBoatHit(c, x, y)) : null
            ));
        }
        bool isBoatHit(Boat boat, int x, int y)
        {
            return boat.Y1 <= y && y <= boat.Y2 && boat.X1 <= x && x <= boat.X2;
        }
        bool isHit(List<Boat> boats, int x, int y) =>
            boats.Any(c => isBoatHit(c, x, y));
        bool isDestroyed(List<Boat> boats, List<Step> playerSteps)
        {
            var lastStep = playerSteps.Last();
            var lastX = lastStep.X;
            var lastY = lastStep.Y;
            if (!isHit(boats, lastX, lastY)) return false;
            var hitBoat = boats.First(c => isBoatHit(c, lastX, lastY));
            List<Tuple<int, int>> boatCoordinates = [];
            if (hitBoat.X1 == hitBoat.X2)
            {
                int start = hitBoat.Y1 > hitBoat.Y2 ? hitBoat.Y2 : hitBoat.Y1;
                int end = hitBoat.Y1 > hitBoat.Y2 ? hitBoat.Y1 : hitBoat.Y2;
                for (int i = start; i <= end; i++)
                    boatCoordinates.Add(Tuple.Create(hitBoat.X1, i));
            }
            else
            {
                int start = hitBoat.X1 > hitBoat.X2 ? hitBoat.X2 : hitBoat.X1;
                int end = hitBoat.X1 > hitBoat.X2 ? hitBoat.X1 : hitBoat.X2;
                for (int i = start; i <= end; i++)
                    boatCoordinates.Add(Tuple.Create(i, hitBoat.Y1));
            }
            return boatCoordinates.All(x => playerSteps.Any(y => y.X == x.Item1 && y.Y == x.Item2));
        }
    }
}
