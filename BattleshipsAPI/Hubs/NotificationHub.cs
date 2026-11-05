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
        private static readonly ConcurrentDictionary<string, string> Connections = new();

        public override Task OnConnectedAsync()
        {
            var login = Context.GetHttpContext()?.Request.Query["login"].ToString();
            //Console.WriteLine(Context.GetHttpContext()?.Request.Query["path"].ToString());
            
            if (!string.IsNullOrWhiteSpace(login))
                Connections[Context.ConnectionId] = login;

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.GetHttpContext()?.Request.Query["path"].ToString() == "CreateGame")
            {
                var login = Context.GetHttpContext()?.Request.Query["login"].ToString();
                _Games.TryRemove(login!);
            }
            Connections.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public static IEnumerable<string> GetConnections(string login) =>
            Connections.Where(x => x.Value == login).Select(x => x.Key);
    }
}
