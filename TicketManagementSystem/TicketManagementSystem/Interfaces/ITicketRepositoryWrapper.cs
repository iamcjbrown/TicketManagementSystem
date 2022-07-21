namespace TicketManagementSystem
{
    public interface ITicketRepositoryWrapper
    {
        int CreateTicket(Ticket ticket);

        Ticket GetTicket(int id);

        void UpdateTicket(Ticket ticket);
    }
}