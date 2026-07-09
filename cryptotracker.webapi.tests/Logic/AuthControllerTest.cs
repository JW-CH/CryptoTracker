using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Controllers;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class AuthControllerTest
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private CryptoTrackerConfig _config;

    [SetUp]
    public void Setup()
    {
        _userManagerMock = MockUserManager();
        _config = new CryptoTrackerConfig { Auth = new CryptoTrackerAuth { Secret = new string('x', 32) } };
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private AuthController CreateController(params ApplicationUser[] existingUsers)
    {
        _userManagerMock.Setup(x => x.Users).Returns(existingUsers.AsQueryable());

        var signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);

        return new AuthController(
            _userManagerMock.Object,
            signInManager.Object,
            _config,
            new JwtService(_config),
            Mock.Of<ILogger<AuthController>>());
    }

    [Test]
    public void RegistrationEnabled_WithoutUsers_IsOpen()
    {
        var controller = CreateController();

        var result = controller.RegistrationEnabled();

        Assert.That(((OkObjectResult)result.Result!).Value, Is.True);
    }

    [Test]
    public void RegistrationEnabled_WithExistingUser_IsClosed()
    {
        var controller = CreateController(new ApplicationUser { UserName = "user" });

        var result = controller.RegistrationEnabled();

        Assert.That(((OkObjectResult)result.Result!).Value, Is.False);
    }

    [Test]
    public void RegistrationEnabled_WithExistingUserButAllowRegistration_IsOpen()
    {
        _config.Auth.AllowRegistration = true;
        var controller = CreateController(new ApplicationUser { UserName = "user" });

        var result = controller.RegistrationEnabled();

        Assert.That(((OkObjectResult)result.Result!).Value, Is.True);
    }

    [Test]
    public async Task Register_WhenClosed_ReturnsForbiddenAndCreatesNoUser()
    {
        var controller = CreateController(new ApplicationUser { UserName = "user" });

        var result = await controller.Register(new AuthController.RegisterRequest("intruder", "i@example.com", "Passw0rd!"));

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }
}
