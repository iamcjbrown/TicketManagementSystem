using EmailService;
using Moq;
using NUnit.Framework;

namespace TicketManagementSystem.Test;

public partial class TicketServiceTests
{
    private TicketService ticketService;
    private Mock<IUserRepository> mockUserRepo;
    private Mock<ITicketRepositoryWrapper> mockTicketRepo;
    private Mock<IEmailService> mockEmailService;

    [SetUp]
    public void Setup()
    {
        this.mockUserRepo = new Mock<IUserRepository>();
        this.mockTicketRepo = new Mock<ITicketRepositoryWrapper>();
        this.mockEmailService = new Mock<IEmailService>();
        this.ticketService = new TicketService(this.mockUserRepo.Object, this.mockTicketRepo.Object, this.mockEmailService.Object);
    }

    [TestCase("")]
    [TestCase(null)]
    public void CreateTicket_NullOrEmptyDesc_ThrowsException(string desc)
    {
        Assert.Throws<InvalidTicketException>(() => this.ticketService.CreateTicket("title", Priority.Medium, null, desc, DateTime.Now, false));
    }

    [TestCase("")]
    [TestCase(null)]
    public void CreateTicket_NullOrEmptyTitle_ThrowsException(string title)
    {
        var ex = Assert.Throws<InvalidTicketException>(() => this.ticketService.CreateTicket(title, Priority.Medium, null, "test", DateTime.Now, false));
    }

    [TestCase("")]
    [TestCase(null)]
    public void CreateTicket_NullOrEmptyAssignedTo_ThrowsException(string assignedTo)
    {
        var ex = Assert.Throws<UnknownUserException>(() => this.ticketService.CreateTicket("title", Priority.Medium, assignedTo, "test", DateTime.Now, false));
        Assert.That(ex.Message, Is.EqualTo($"User {assignedTo} not found"));
    }

    [Test]
    public void CreateTicket_GetUserReturnsNull_ThrowsException()
    {
        // Arrange
        var userName = "testUser";

        // Act
        var ex = Assert.Throws<UnknownUserException>(() => this.ticketService.CreateTicket("title", Priority.Medium, userName, "test", DateTime.Now, false));
        Assert.That(ex.Message, Is.EqualTo($"User {userName} not found"));
    }

    [TestCase(Priority.Medium, Priority.High, 2)]
    [TestCase(Priority.Low, Priority.Medium, 2)]
    [TestCase(Priority.Medium, Priority.Medium, 0)]
    [TestCase(Priority.Low, Priority.Low, 0)]
    public void CreateTicket_Date_TicketHasCorrectPriority(Priority priority, Priority expectedPriority, double hour)
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName});

        // Act
        this.ticketService.CreateTicket("title", priority, userName, "test", DateTime.UtcNow - TimeSpan.FromHours(hour), false);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.Priority == expectedPriority)), Times.Once);
    }

    [TestCase(Priority.Medium, Priority.High, "Crash")]
    [TestCase(Priority.Medium, Priority.High, "Important")]
    [TestCase(Priority.Medium, Priority.High, "Failure")]
    [TestCase(Priority.Medium, Priority.Medium, "Title")]
    [TestCase(Priority.Low, Priority.Medium, "Crash")]
    [TestCase(Priority.Low, Priority.Medium, "Important")]
    [TestCase(Priority.Low, Priority.Medium, "Failure")]
    [TestCase(Priority.Low, Priority.Low, "Title")]
    public void CreateTicket_TitleContainsValue_TicketHasCorrectPriority(Priority priority, Priority expectedPriority, string title)
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName});

        // Act
        this.ticketService.CreateTicket(title, priority, userName, "test", DateTime.UtcNow, false);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.Priority == expectedPriority)), Times.Once);
    }

    [TestCase(Priority.Medium, Priority.High, 2)]
    public void CreateTicket_DateCausesPriorityToBeRaisedToHigh_CallsSendEmailToAdministrator(Priority priority, Priority expectedPriority, double hour)
    {
        // Arrange
        var userName = "testUser";
        var title = "title";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket("title", priority, userName, title, DateTime.UtcNow - TimeSpan.FromHours(hour), false);

        // Assert
        this.mockEmailService.Verify(x => x.SendEmailToAdministrator(title,userName), Times.Once);
    }

    [TestCase(Priority.Medium, Priority.High, "Crash")]
    [TestCase(Priority.Medium, Priority.High, "Important")]
    [TestCase(Priority.Medium, Priority.High, "Failure")]
    public void CreateTicket_TitleCausesPriorityToBeRaisedToHigh_CallsSendEmailToAdministrator(Priority priority, Priority expectedPriority, string title)
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket(title, priority, userName, "test", DateTime.UtcNow, false);

        // Assert
        this.mockEmailService.Verify(x => x.SendEmailToAdministrator(title, userName), Times.Once);
    }

    [Test]
    public void CreateTicket_GetUserReturnsUser_TicketHasUser()
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName});

        // Act
        this.ticketService.CreateTicket("title", Priority.Medium, userName, "test", DateTime.UtcNow, false);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.AssignedUser.Username == userName)), Times.Once);
    }

    [TestCase(Priority.Low, 50)]
    [TestCase(Priority.Medium, 50)]
    [TestCase(Priority.High, 100)]
    public void CreateTicket_IsPayingCustomerTrue_TicketHasCorrectPrice(Priority priority, double price)
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });
        this.mockUserRepo.Setup(x => x.GetAccountManager()).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket("title", priority, userName, "test", DateTime.UtcNow, true);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.PriceDollars == price)), Times.Once);
    }

    [TestCase(Priority.Low, 0)]
    [TestCase(Priority.Medium, 0)]
    [TestCase(Priority.High, 0)]
    public void CreateTicket_IsPayingCustomerFalse_TicketHasCorrectPrice(Priority priority, double price)
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });
        this.mockUserRepo.Setup(x => x.GetAccountManager()).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket("title", priority, userName, "test", DateTime.UtcNow, false);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.PriceDollars == price)), Times.Once);
    }

    [Test]
    public void CreateTicket_IsPayingCustomerTrue_TicketHasCorrectAccountManager()
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });
        this.mockUserRepo.Setup(x => x.GetAccountManager()).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket("title", Priority.Medium, userName, "test", DateTime.UtcNow, true);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.AccountManager.Username == userName)), Times.Once);
    }

    [Test]
    public void CreateTicket_IsPayingCustomerFalse_TicketHasCorrectAccountManager()
    {
        // Arrange
        var userName = "testUser";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket("title", Priority.Medium, userName, "test", DateTime.UtcNow, false);

        // Assert
        this.mockTicketRepo.Verify(x => x.CreateTicket(It.Is<Ticket>(y => y.AccountManager == null)), Times.Once);
    }

    [TestCase(Priority.Low, 0)]
    [TestCase(Priority.Medium, 0)]
    [TestCase(Priority.High, 1)]
    public void CreateTicket_Priority_EmailsAdmin(Priority priority, int times)
    {
        // Arrange
        var userName = "testUser";
        var title = "title";
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });
        this.mockUserRepo.Setup(x => x.GetAccountManager()).Returns(new User() { Username = userName });

        // Act
        this.ticketService.CreateTicket(title, priority, userName, "test", DateTime.UtcNow, false);

        // Assert
        this.mockEmailService.Verify(x => x.SendEmailToAdministrator(It.Is<string>(a => a == title), It.Is<string>(b => b == userName)), Times.Exactly(times));
    }
}
