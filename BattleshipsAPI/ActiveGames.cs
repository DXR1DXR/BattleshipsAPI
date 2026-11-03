using BattleshipsAPI.Hubs;
using BattleshipsAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.ObjectModel;

namespace BattleshipsAPI
{
    public class ActiveGames
    {
        public ActiveGames(IHubContext<NotificationHub> hub)
        {
            this.hub = hub;
        }
        private readonly IHubContext<NotificationHub> hub;
        private object locker = new();
        private List<Game> _Games = new List<Game>();
        public ReadOnlyCollection<Game> Games { get => _Games.AsReadOnly(); }
        public void UpdatePing(string userLogin)
        {
            lock (locker)
            {
                var game = _Games.FirstOrDefault(x => x.Creator.Login == userLogin);
                if (game is null) return;
                game!.LastPingTimeStamp = DateTime.Now;
                Console.WriteLine($"{DateTime.Now} Ping updated");
            }
        }
        public bool IsUserInGame(string userLogin)
        {
            lock (locker)
                return Games.Any(x => x.Creator.Login == userLogin);
        }
        public void CreateGame(Player player)
        {
            lock (locker)
            {
                var game = new Game(player, hub);
                game.PropertyChanged += Game_PropertyChanged;
                Console.WriteLine($"{DateTime.Now} Game Created");
                _Games.Add(game);
                hub.Clients.All.SendAsync("NewGame", new GameDTO(game.StartTimeStamp, game.Creator.NickName));
            }
        }
        public bool ContainsNickName(string nickName)
        {
            lock (locker)
                return Games.Any(x => x.Creator.NickName == nickName);
        }
        public bool ConnectUserToGame(Player connectingPlayer, string hostNickName)
        {
            lock (locker)
            {
                var game = _Games.FirstOrDefault(x => hostNickName == x.Creator.NickName);
                if (game is null) return false;
                game.Opponent = connectingPlayer;
            }
            return true;
        }
        public void CancelGame(string playerLogin)
        {
            lock (locker)
            {
                var game = _Games.FirstOrDefault(x => x.Creator.Login == playerLogin);
                if (game is null) return;
                game.CancellationPending = true;
                _Games.Remove(game);
            }
            
        }

        private void Game_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            lock (locker)
            {
                var game = (Game)sender!;
                hub.Clients.All.SendAsync("RemovedGame", new GameDTO(game.StartTimeStamp, game.Creator.NickName));
                _Games.Remove(_Games.First(x => x.Creator.Login == game.Creator.Login));
            }
        }
    }
}
