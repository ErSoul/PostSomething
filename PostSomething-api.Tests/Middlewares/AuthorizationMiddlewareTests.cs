using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

using Moq;

namespace PostSomething_api.Tests.Middlewares
{
    public class AuthorizationMiddlewareTests
    {
        //[Fact]
        //public async Task AuthorizationMiddlewareShouldReturnUnauthorizedAsync()
        //{
        //    using var host = await new HostBuilder()
        //        .ConfigureWebHost(webBuilder =>
        //        {
        //            webBuilder
        //                .UseTestServer()
        //            .ConfigureServices(services =>
        //                {
        //                    services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, AuthorizationMiddleware>();
        //                })
        //            .Configure(app =>
        //                {
        //                    app.UseAuthentication();
        //                    app.UseAuthorization();
        //                });
        //        })
        //        .StartAsync();

        //    var response = await host.GetTestClient().GetAsync("/profile");
        //}

        [Fact]
        public async Task AuthorizationMiddlewareShouldContinueOnSuccess()
        {
            var httpContext = new DefaultHttpContext();
            var nextMock = Mock.Of<RequestDelegate>();
            var requirements = new[] { new AuthorizationRequirement(), new AuthorizationRequirement() };
            var authorizationSchemes = new string[] { "somebody", "someone" };
            var policyMock = new Mock<AuthorizationPolicy>(requirements.AsEnumerable(), authorizationSchemes.AsEnumerable());

            var authMiddleware = new PostSomething_api.Middlewares.AuthorizationMiddleware();

            await authMiddleware.HandleAsync(nextMock, httpContext, policyMock.Object, PolicyAuthorizationResult.Success());

            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task AuthorizationMiddlewareShouldReturnForbidden()
        {
            var httpContext = new DefaultHttpContext();
            var nextMock = Mock.Of<RequestDelegate>();
            var requirements = new[] { new AuthorizationRequirement(), new AuthorizationRequirement() };
            var authorizationSchemes = new string[] { "somebody", "someone" };
            var policyMock = new Mock<AuthorizationPolicy>(requirements.AsEnumerable(), authorizationSchemes.AsEnumerable());

            var authMiddleware = new PostSomething_api.Middlewares.AuthorizationMiddleware();

            await authMiddleware.HandleAsync(nextMock, httpContext, policyMock.Object, PolicyAuthorizationResult.Forbid());

            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
        }

        private class AuthorizationRequirement : IAuthorizationRequirement { }
    }
}