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

        public int CreateTicket(string title, Priority priority, string assignedTo, string desc, DateTime date, bool isPayingCustomer) =>
            this.ticketRepository.CreateTicket(new Ticket()
            {
                Title = string.IsNullOrWhiteSpace(title) ? throw new InvalidTicketException(TitleExceptionMessage) : title,
                Description = string.IsNullOrWhiteSpace(desc) ? throw new InvalidTicketException(TitleExceptionMessage) : desc,
                AssignedUser = this.GetUser(assignedTo),
                Priority = priority = this.RaisePriority(priority, date, title, assignedTo),
                Created = date,
                PriceDollars = isPayingCustomer ? priority.Price() : 0,
                AccountManager = isPayingCustomer ? this.userRepository.GetAccountManager() : null
            });

        public void AssignTicket(int id, string username)
        {
            var user = this.GetUser(username);

            var ticket = this.ticketRepository.GetTicket(id) ?? throw new ApplicationException($"No ticket found for id {id}");

            ticket.AssignedUser = user;

            this.ticketRepository.UpdateTicket(ticket);
        }

        private Priority RaisePriority(Priority priority, DateTime date, string title, string assignedTo)
        {
            priority = priority.RaisePriority(date < DateTime.UtcNow - TimeSpan.FromHours(1) || priorityTitleFlags.Any(title.Contains));

            this.EmailAdmin(priority, title, assignedTo);

            return priority;
        }

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
