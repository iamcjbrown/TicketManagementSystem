namespace TicketManagementSystem
{
    public class TicketRepositoryWrapper : ITicketRepositoryWrapper
    {
        public int CreateTicket(Ticket ticket) => TicketRepository.CreateTicket(ticket);

        public Ticket GetTicket(int id) => TicketRepository.GetTicket(id);

        public void UpdateTicket(Ticket ticket) => TicketRepository.UpdateTicket(ticket);
    }
}

