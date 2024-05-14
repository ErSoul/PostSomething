using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Moq;
using NuGet.Protocol;
using PostSomething_api.Controllers;
using PostSomething_api.Models;
using PostSomething_api.Requests;
using PostSomething_api.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PostSomething_api.Tests.Controllers
{
    public class AuthControllerTests
    {
        private AuthController _authController;
        private readonly Mock<IEmailSender> _mailSender;
        private readonly IConfiguration _config;
        private readonly Mock<IUserManager<ApiUser>> _userManager;

        public AuthControllerTests()
        {
            _userManager = new Mock<IUserManager<ApiUser>>();
            _mailSender = new Mock<IEmailSender>();

            _config = new ConfigurationBuilder().
                AddInMemoryCollection(
                new Dictionary<string, string?> {
                    {"Jwt:Key", "SomethingWithMinimumCharactersTest"},
                    {"Jwt:Issuer", "test"},
                    {"Jwt:Audience", "noOne"}
                }).Build();

            var routeData = new RouteData(new RouteValueDictionary(new { algo = "prueba" }));
            var actionContext = new ActionContext(Mock.Of<HttpContext>(), routeData, Mock.Of<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>(), Mock.Of<ModelStateDictionary>());

            var url = new Mock<IUrlHelper>();
            url.Setup(u => u.Link("Confirmation", It.IsAny<object>())).Returns(new Bogus.Faker().Internet.UrlWithPath());

            var request = new Mock<HttpRequest>();
            request.Setup(_ => _.Scheme).Returns("http");
            var httpContext = Mock.Of<HttpContext>(_ => _.Request == request.Object);
            var controllerContext = new ControllerContext() { HttpContext = httpContext };

            _authController = new AuthController(_userManager.Object, _mailSender.Object, _config) { ControllerContext = controllerContext, Url = url.Object };
        }

        [Fact(DisplayName = "User should be able to register")]
        public async Task RegisterShouldReturnOk()
        {
            var userToRegister = new RegisterUserBody { Email = "test@example.org", Password = "P@ssw0rd", ConfirmationPassword = "P@ssw0rd", UserName = "Test" };
            var faker = new Bogus.Faker();

            _userManager.Setup(um => um.CreateAsync(It.IsAny<ApiUser>(), userToRegister.Password)).ReturnsAsync(IdentityResult.Success).Verifiable();
            _userManager.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApiUser>())).ReturnsAsync(Guid.NewGuid().ToString()).Verifiable();
            _mailSender.Setup(ms => ms.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            var result = await _authController.Register(userToRegister) as JsonResult;

            _userManager.Verify(um => um.CreateAsync(It.IsAny<ApiUser>(), userToRegister.Password), Times.Once);
            _userManager.Verify(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApiUser>()), Times.Once);
            _mailSender.Verify(ms => ms.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            Assert.NotNull(result);
            Assert.Equivalent(new { userToRegister.Email, userToRegister.UserName }, result.Value);
        }


        [Fact(DisplayName = "User should fail to register")]
        public async Task RegisterShouldReturn422()
        {
            var userToRegister = new RegisterUserBody { Email = "test@example.org", Password = "password", ConfirmationPassword = "password", UserName = "Test" };
            var error = new IdentityError[] { new() { Code = "01", Description = "Something Failed" } };

            _userManager.Setup(um => um.CreateAsync(It.IsAny<ApiUser>(), userToRegister.Password))
                .ReturnsAsync(IdentityResult.Failed(
                    error
                )).Verifiable();

            var result = await _authController.Register(userToRegister) as UnprocessableEntityObjectResult;

            _userManager.Verify(um => um.CreateAsync(It.IsAny<ApiUser>(), userToRegister.Password), Times.Once);

            Assert.NotNull(result);
            Assert.Equal(error, result.Value);
        }

        [Fact]
        public async Task RegisterShouldReturnBadRequestWhenConfirmationPasswordDiffer()
        {
            var userToRegister = new RegisterUserBody { Email = "test@example.org", Password = "P@ssw0rd", ConfirmationPassword = "P@ssw0rdDiffer", UserName = "Test" };

            var result = await _authController.Register(userToRegister) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Contains("PasswordsMismatch", result.Value!.ToJson());
            Assert.Contains("The password must match the one above", result.Value!.ToJson());
        }

        [Fact(DisplayName = "Confirming email should return error due to missing parameters")]
        public async Task ConfirmEmailShouldReturn422DueToMissingValues()
        {
            var result = await _authController.ConfirmAccount("ajsdklf;ajfkl", It.IsAny<string>());

            Assert.IsType<UnprocessableEntityResult>(result);
        }

        [Fact(DisplayName = "Confirming email should return error due to user not found")]
        public async Task ConfirmEmailShouldReturnUserNotFound()
        {
            var result = await _authController.ConfirmAccount("ajsdklf;ajfkl", "aasdsklafdjdlfa");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact(DisplayName = "Confirming email should return bad request")]
        public async Task ConfirmEmailShouldReturnBadRequest()
        {
            var userId = "ajsdklf;ajfkl";
            var userToken = "aasdsklafdjdlfa";
            var returnedUser = new ApiUser();

            _userManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(returnedUser).Verifiable();
            _userManager.Setup(um => um.ConfirmEmailAsync(returnedUser, userToken)).ReturnsAsync(IdentityResult.Failed()).Verifiable();

            var result = await _authController.ConfirmAccount(userId, userToken);

            _userManager.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManager.Verify(um => um.ConfirmEmailAsync(returnedUser, userToken), Times.Once);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact(DisplayName = "Confirming email should return ok")]
        public async Task ConfirmEmailShouldReturnOk()
        {
            var userId = "ajsdklf;ajfkl";
            var userToken = "aasdsklafdjdlfa";
            var returnedUser = new ApiUser();

            _userManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(returnedUser).Verifiable();
            _userManager.Setup(um => um.ConfirmEmailAsync(returnedUser, userToken)).ReturnsAsync(IdentityResult.Success).Verifiable();

            var result = await _authController.ConfirmAccount(userId, userToken);

            _userManager.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManager.Verify(um => um.ConfirmEmailAsync(returnedUser, userToken), Times.Once);

            Assert.IsType<OkResult>(result);
        }

        [Fact(DisplayName = "Login Endpoint should return a JWT Token.")]
        public async Task LoginShouldReturnOk()
        {
            var loginRequest = new LoginRequest { Email = "something@xyz.com", Password = "password" };
            var apiUser = new ApiUser { Email = loginRequest.Email, UserName = "Test", PasswordHash = "abcdefgh", EmailConfirmed = true, Address = "somewhere" };

            _userManager.Setup(um => um.FindByEmailAsync(loginRequest.Email)).ReturnsAsync(apiUser);
            _userManager.Setup(um => um.CheckPasswordAsync(apiUser, loginRequest.Password)).ReturnsAsync(true);

            var result = await _authController.Login(loginRequest) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact(DisplayName = "Login Endpoint should return Unauthorized due to wrong credentials.")]
        public async Task LoginShouldReturnUnauthorized()
        {
            var loginRequest = new LoginRequest { Email = "something@xyz.com", Password = "password" };
            var apiUser = new ApiUser { Email = loginRequest.Email, UserName = "Test", PasswordHash = "abcdefgh", EmailConfirmed = true };

            _userManager.Setup(um => um.FindByEmailAsync(loginRequest.Email)).ReturnsAsync(apiUser).Verifiable();

            var result = await _authController.Login(loginRequest) as UnauthorizedObjectResult;

            _userManager.Verify(um => um.FindByEmailAsync(loginRequest.Email), Times.Once);

            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(result.Value, "Wrong credentials.");
        }

        [Fact(DisplayName = "Login Endpoint should return Unauthorized due to user not confirmed.")]
        public async Task LoginShouldReturnForbidConfirm()
        {
            var loginRequest = new LoginRequest { Email = "something@xyz.com", Password = "password" };
            var apiUser = new ApiUser { Email = loginRequest.Email, UserName = "Test", PasswordHash = "abcdefgh", EmailConfirmed = false };

            _userManager.Setup(um => um.FindByEmailAsync(loginRequest.Email)).ReturnsAsync(apiUser);
            _userManager.Setup(um => um.CheckPasswordAsync(apiUser, loginRequest.Password)).ReturnsAsync(true);

            var result = await _authController.Login(loginRequest) as UnauthorizedObjectResult;

            _userManager.Verify(um => um.FindByEmailAsync(loginRequest.Email), Times.Once);
            _userManager.Verify(um => um.CheckPasswordAsync(apiUser, loginRequest.Password), Times.Once);

            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(result.Value, "Wrong credentials.");
        }

        [Fact(DisplayName = "Profile should return the current user information.")]
        public async Task ProfileShouldReturnUserInfo()
        {
            var currentUserEmail = "testing@example.org";
            var returnedUser = new Fake.ApiUser().Generate();
            var request = new Mock<HttpRequest>();

            request.Setup(_ => _.Scheme).Returns("http");
            var httpContext = Mock.Of<HttpContext>
                (
                    _ =>
                        _.Request == request.Object &&
                        _.User == new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                        {
                            new(JwtRegisteredClaimNames.Email, currentUserEmail)
                        }))
                 );
            var controllerContext = new ControllerContext() { HttpContext = httpContext };

            _authController = new AuthController(_userManager.Object, _mailSender.Object, _config) { ControllerContext = controllerContext };

            _userManager.Setup(um => um.FindByEmailAsync(currentUserEmail)).ReturnsAsync(returnedUser).Verifiable();

            var result = await _authController.Profile() as OkObjectResult;

            _userManager.Verify(um => um.FindByEmailAsync(currentUserEmail), Times.Once);

            Assert.NotNull(result);
            Assert.Equal(returnedUser, result.Value);
        }
    }
}