using EmailService;
using Moq;
using NUnit.Framework;

namespace TicketManagementSystem.Test;

public partial class TicketServiceTests
{
    [TestCase("")]
    [TestCase(null)]
    public void AssignTicket_NullOrEmptyUserName_ThrowsException(string userName)
    {
        var ex = Assert.Throws<UnknownUserException>(() => this.ticketService.AssignTicket(0, userName));
        Assert.That(ex.Message, Is.EqualTo($"User {userName} not found"));
    }

    [Test]
    public void AssignTicket_GetUserReturnsNull_ThrowsException()
    {
        // Arrange
        var userName = "testUser";

        // Act
        var ex = Assert.Throws<UnknownUserException>(() => this.ticketService.AssignTicket(0, userName));
        Assert.That(ex.Message, Is.EqualTo($"User {userName} not found"));
    }

    [Test]
    public void AssignTicket_GetTicketReturnsNull_ThrowsException()
    {
        // Arrange
        var userName = "testUser";
        var id = 1;
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User());

        // Act
        var ex = Assert.Throws<ApplicationException>(() => this.ticketService.AssignTicket(id, userName));
        Assert.That(ex.Message, Is.EqualTo($"No ticket found for id {id}"));
    }

    [Test]
    public void AssignTicket_GetUserReturnsUser_CallsGetTicket()
    {
        // Arrange
        var userName = "testUser";
        var id = 1;
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });
        this.mockTicketRepo.Setup(x => x.GetTicket(It.IsAny<int>())).Returns(new Ticket() { Id = id, AssignedUser = new User() { Username = userName } });

        // Act
        this.ticketService.AssignTicket(id, userName);

        // Assert
        this.mockTicketRepo.Verify(x => x.GetTicket(id), Times.Once);
    }

    [Test]
    public void AssignTicket_GetTicketReturnsTicket_CallsUpdateTicketWithCorrectUser()
    {
        // Arrange
        var userName = "testUser";
        var id = 1;
        this.mockUserRepo.Setup(x => x.GetUser(It.IsAny<string>())).Returns(new User() { Username = userName });
        this.mockTicketRepo.Setup(x => x.GetTicket(It.IsAny<int>())).Returns(new Ticket() { Id = id, AssignedUser = new User() { Username = "testUser2" } });

        // Act
        this.ticketService.AssignTicket(id, userName);

        // Assert
        this.mockTicketRepo.Verify(x => x.UpdateTicket(It.Is<Ticket>(x => x.AssignedUser.Username == userName)), Times.Once);
    }
}
