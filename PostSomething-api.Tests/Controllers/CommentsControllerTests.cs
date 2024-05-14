using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PostSomething_api.Controllers;
using PostSomething_api.Requests;
using PostSomething_api.Services.Interface;
using System.Net;
using System.Security.Claims;

namespace PostSomething_api.Tests.Controllers
{
    public class CommentsControllerTests
    {
        private const int MAX_COMMENTS = 5000;
        private readonly Random random = new();
        private readonly Mock<ICommentsService> _commentsServiceStub;
        private readonly CommentsController _commentsControllerMock;
        public CommentsControllerTests()
        {
            _commentsServiceStub = new Mock<ICommentsService>();
            _commentsControllerMock = new CommentsController(_commentsServiceStub.Object);
        }

        [Theory]
        [MemberData(nameof(DummyCommentsSamePostID), parameters: 5)]
        public async Task GetCommentsShouldReturnSuccess(int postID, Models.Comment[] comments)
        {
            _commentsServiceStub.Setup(cs => cs.GetCommentsFromPost(postID)).ReturnsAsync(comments);

            var response = await _commentsControllerMock.GetComments(postID);

            _commentsServiceStub.Verify(cs => cs.GetCommentsFromPost(postID), Times.Once());

            Assert.True(response.Any());
            Assert.Equal(comments.Length, response.Count());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1234213)]
        [InlineData(431)]
        [InlineData(673)]
        public async Task GetCommentShouldReturnSuccess(int commentID)
        {
            var comment = new Fake.Comment().Generate();
            comment.Id = commentID;
            _commentsServiceStub.Setup(cs => cs.GetComment(commentID)).ReturnsAsync(comment);

            var response = await _commentsControllerMock.Get(commentID);
            _commentsServiceStub.Verify(cs => cs.GetComment(commentID), Times.Once());
            Assert.NotNull(response);

            var result = response as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);

            var resultObject = result.Value as DTO.Comment;
            Assert.NotNull(resultObject);
            Assert.Equal(commentID, resultObject.Id);
        }

        [Fact]
        public async Task GetCommentShouldReturnNotFound()
        {
            var commentID = random.Next();
            _commentsServiceStub.Setup(cs => cs.GetComment(commentID)).ReturnsAsync(It.IsAny<Models.Comment>());

            var response = await _commentsControllerMock.Get(commentID) as NotFoundResult;

            _commentsServiceStub.Verify(cs => cs.GetComment(commentID), Times.Once());

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1234213)]
        [InlineData(454)]
        [InlineData(673)]
        public async Task CreateCommentShouldSuccess(int postID)
        {
            var userMock = new Mock<ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirst(It.IsAny<string>())).Returns(new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sid, new Bogus.Faker().Random.Guid().ToString()));
            var httpContext = Mock.Of<HttpContext>(_ => _.User == userMock.Object);
            var controllerContext = new ControllerContext() { HttpContext = httpContext };
            var commentController = new CommentsController(_commentsServiceStub.Object) { ControllerContext = controllerContext };

            var commentRequest = new CommentRequest { Content = new Bogus.Faker().Lorem.Paragraph() };
            var commentModel = new Fake.Comment(postID, commentRequest.Content).Generate();

            _commentsServiceStub.Setup(cs => cs.CreateCommentFromPost(postID, commentRequest, It.IsAny<string>())).ReturnsAsync(commentModel);

            var response = await commentController.CreateCommentFromPost(commentRequest, postID) as JsonResult;
            _commentsServiceStub.Verify(cs => cs.CreateCommentFromPost(postID, commentRequest, It.IsAny<string>()), Times.Once);
            Assert.NotNull(response);

            var result = response.Value as DTO.Comment;
            Assert.NotNull(result);
            Assert.Equal(commentRequest.Content, result.Content);
            Assert.Equal(postID, result.Post!.Id);
        }

        [Fact]
        public async Task DeleteShouldReturnNoContent()
        {
            var commentID = random.Next();
            _commentsServiceStub.Setup(cs => cs.Delete(commentID));

            var response = await _commentsControllerMock.Delete(commentID) as NoContentResult;

            _commentsServiceStub.Verify(cs => cs.Delete(commentID), Times.Once());
            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NoContent, response.StatusCode);
        }

        public static IEnumerable<object[]> DummyCommentsSamePostID(int count)
        {
            var data = new List<object[]>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                var post = new Fake.Post().Generate();
                var comments = new Fake.Comment(post.Id).Generate(random.Next(MAX_COMMENTS));

                comments.ElementAt(random.Next(comments.Count)).Parent = new Fake.Comment(post.Id).Generate();
                comments.ElementAt(random.Next(comments.Count)).Parent = new Fake.Comment(post.Id).Generate();
                comments.ElementAt(random.Next(comments.Count)).Parent = new Fake.Comment(post.Id).Generate();
                comments.ElementAt(random.Next(comments.Count)).Parent = new Fake.Comment(post.Id).Generate();

                data.Add(new object[] { post.Id, comments.ToArray() });
            }

            return data;
        }
    }
}