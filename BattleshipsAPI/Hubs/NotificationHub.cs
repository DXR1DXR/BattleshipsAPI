using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BattleshipsAPI.Hubs
{

    public class NotificationHub : Hub
    {
        public NotificationHub(ActiveGames games)
        {
            _Games = games;
        }
        readonly ActiveGames _Games;
        private static readonly ConcurrentDictionary<string, Tuple<string, string>> Connections = new();

        public override Task OnConnectedAsync()
        {
            string login = Context.GetHttpContext()?.Request.Query["login"].ToString()!;
            string path = Context.GetHttpContext()?.Request.Query["path"].ToString()!;
            //Console.WriteLine(Context.GetHttpContext()?.Request.Query["path"].ToString());

            if (!string.IsNullOrWhiteSpace(login))
            {
                Connections[Context.ConnectionId] = Tuple.Create(login,path);
            }
                

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string login = Context.GetHttpContext()?.Request.Query["login"].ToString()!;
            string path = Context.GetHttpContext()?.Request.Query["path"].ToString()!;
            if (path == "CreateGame")
            {
                _Games.TryRemove(login);
            }
            if (path == "SetShips")
            {
                Console.WriteLine("SetShipsDisconnected");
            }
            
            
            Connections.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public static IEnumerable<string> GetConnections(string login) =>
            Connections.Where(x => x.Value.Item1 == login).Select(x => x.Key);
        public static IEnumerable<string> GetGameConnections(string login) =>
            Connections.Where(x => x.Value.Item1 == login && x.Value.Item2 == "Game").Select(x => x.Key);
        public static string GetConnectionId(string login)
        {
            //TODO: При отправке уведомлений на таймер вызывается fallback id
            return GetConnections(login).FirstOrDefault() ?? "fdhdfghdfhfxcvhgb";
        }
        public static string GetGameConnectionId(string login)
        {
            //TODO: При отправке уведомлений на таймер вызывается fallback id
            return GetGameConnections(login).FirstOrDefault() ?? "fdhdfghdfhfxcvhgb";
        }
    }
}
