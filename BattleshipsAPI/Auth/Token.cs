namespace BattleshipsAPI.Auth
{
    public class Token
    {
        public Token(string playerLogin)
        {
            Value = Hashes.CreateMD5($"denispchyola{DateTime.Now.Ticks}{playerLogin}");
            PlayerLogin = playerLogin;
            CreatedAt = DateTime.Now;
        }
        public void UpdateDate()
        {
            CreatedAt = DateTime.Now;
        }

        public string Value { get; private set; }
        public string PlayerLogin { get; private set; }
        public DateTime CreatedAt { get; private set; }
    }
}
