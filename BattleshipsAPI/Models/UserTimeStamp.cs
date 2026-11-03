namespace BattleshipsAPI.Models
{
    public class UserTimeStamp
    {
        public UserTimeStamp(string userLogin)
        {
            UserLogin = userLogin;
            TimeStamp = DateTime.Now;
        }

        public string UserLogin { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
