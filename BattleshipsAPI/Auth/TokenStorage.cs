namespace BattleshipsAPI.Auth
{
    public static class TokenStorage
    {
        static TokenStorage()
        {
            Task.Run(Cleaner);
        }
        private static List<Token> _tokens = [];
        private const int _lifetime_minutes = 60;
        public static string CreateToken(string playerLogin)
        {
            lock (_tokens)
            {
                Token token = new Token(playerLogin);
                _tokens.Add(token);
                return token.Value;
            }
        }
        public static string? GetUserLogin(string tokenString)
        {
            lock (_tokens)
            {
                var token = _tokens.FirstOrDefault(t => t.Value == tokenString);
                if (token is null) return null;
                if (token.IsExpired()) return null;
                token.UpdateDate();
                return token.PlayerLogin;
            }
        }
        private static void Cleaner()
        {
            while (true)
            {
                Thread.Sleep(30000);
                lock (_tokens)
                {
                    var expiredTokens = _tokens.Where(x => x.IsExpired()).ToList();
                    expiredTokens.ForEach(x => _tokens.Remove(x));
                }
            }
        }
        private static bool IsExpired(this Token token)
        {
            return DateTime.Now - token.CreatedAt > new TimeSpan(0, _lifetime_minutes, 0);
        }
    }
}
