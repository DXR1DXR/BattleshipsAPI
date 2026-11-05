using BattleshipsAPI.Hubs;
using BattleshipsAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BattleshipsAPI
{
    public class ActiveGames
    {
        public ActiveGames(IHubContext<NotificationHub> hub)
        {
            this.hub = hub;
            _Games.CollectionChanged += _Games_CollectionChanged;
        }
        private readonly IHubContext<NotificationHub> hub;
        private object locker = new();
        private ObservableCollection<Game> _Games = new ObservableCollection<Game>();
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
        public void TryRemove(string creatorLogin)
        {
            lock (locker)
            {
                var game = _Games.FirstOrDefault(x => x.Creator.Login == creatorLogin);
                if (game is null) return;
                if (game.Opponent is not null) return;
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
        private void _Games_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var game = (e.NewItems![0] as Game)!;
                hub.Clients.All.SendAsync("NewGame", new GameDTO(game.StartTimeStamp, game.Creator.NickName));
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var game = (e.OldItems![0] as Game)!;
                hub.Clients.All.SendAsync("RemovedGame", new GameDTO(game.StartTimeStamp, game.Creator.NickName));
            }
        }
    }
}
