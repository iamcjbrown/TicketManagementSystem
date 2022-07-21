namespace TicketManagementSystem
{
    public interface IUserRepository
    {
        void Dispose();

        User GetAccountManager();

        User GetUser(string username);
    }
}

