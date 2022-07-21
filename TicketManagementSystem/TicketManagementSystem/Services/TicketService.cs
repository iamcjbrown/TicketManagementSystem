using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using EmailService;

namespace TicketManagementSystem
{
    public class TicketService
    {
        private readonly string[] priorityTitleFlags = { "Crash", "Important", "Failure" };

        private readonly IUserRepository userRepository;
        private readonly ITicketRepositoryWrapper ticketRepository;
        private readonly IEmailService emailService;

        private const string TitleExceptionMessage = "Title or description were null";

        public TicketService(IUserRepository userRepository = null, ITicketRepositoryWrapper ticketRepository = null, IEmailService emailService = null)
        {
            this.userRepository = userRepository ?? new UserRepository();
            this.ticketRepository = ticketRepository ?? new TicketRepositoryWrapper();
            this.emailService = emailService ?? new EmailServiceProxy();
        }

        public int CreateTicket(string title, Priority priority, string assignedTo, string desc, DateTime date, bool isPayingCustomer)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(desc))
            {
                throw new InvalidTicketException(TitleExceptionMessage);
            }

            var user = this.GetUser(assignedTo);

            this.RaisePriority(ref priority, date, title);

            this.EmailAdmin(priority, title, assignedTo);

            var price = isPayingCustomer ? priority.Price() : 0;

            var accountManager = isPayingCustomer ? this.userRepository.GetAccountManager() : null;

            var ticket = new Ticket()
            {
                Title = title,
                AssignedUser = user,
                Priority = priority,
                Description = desc,
                Created = date,
                PriceDollars = price,
                AccountManager = accountManager
            };

            var id = this.ticketRepository.CreateTicket(ticket);

            // Return the id
            return id;
        }

        public void AssignTicket(int id, string username)
        {
            var user = this.GetUser(username);

            var ticket = this.ticketRepository.GetTicket(id) ?? throw new ApplicationException($"No ticket found for id {id}");

            ticket.AssignedUser = user;

            this.ticketRepository.UpdateTicket(ticket);
        }

        private Priority RaisePriority(ref Priority priority, DateTime date, string title) =>
            priority = priority.RaisePriority(date < DateTime.UtcNow - TimeSpan.FromHours(1) || priorityTitleFlags.Any(title.Contains));

        private void EmailAdmin(Priority priority, string title, string assignedTo)
        {
            if (priority == Priority.High)
            {
                this.emailService.SendEmailToAdministrator(title, assignedTo);
            }
        }

        private User GetUser(string userName)
        {
            var user = !string.IsNullOrEmpty(userName)
                ? this.userRepository.GetUser(userName)
                : null;

            return user ?? throw new UnknownUserException($"User {userName} not found"); ;
        }
    }
}
