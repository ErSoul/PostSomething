using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using PostSomething_api.Controllers;
using PostSomething_api.Repositories.Interface;

namespace PostSomething_api.Tests.Controllers
{
    public class PostControllerTests
    {
        private readonly PostsController _postController;
        private readonly Mock<IPostRepository> _postRepository;

        public PostControllerTests()
        {
            _postRepository = new Mock<IPostRepository>();
            _postController = new PostsController(_postRepository.Object);
        }

        [Fact]
        public void IndexShouldReturnAllPosts()
        {
            var testPosts = new List<Models.Post>()
            {
                new() {
                    Id = 1,
                    Title = "How to do something",
                    Description = "Dummy",
                    Author = new Models.ApiUser{
                        Id = Guid.NewGuid().ToString(),
                        Email = "xyz@xyz.org",
                        UserName = "Test"
                    }
                }
            };

            _postRepository.Setup(pr => pr.GetList()).Returns(testPosts.AsQueryable());
            var response = _postController.Index() as OkObjectResult;
            _postRepository.Verify(pr => pr.GetList(), Times.Once);

            Assert.NotNull(response);
            var result = response.Value as IQueryable<DTO.Post>;
            Assert.IsAssignableFrom<IQueryable<DTO.Post>>(result);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(testPosts.First().Title, result.First().Title);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4452)]
        [InlineData(28592)]
        public async void GetPostShouldReturnOnePost(int postID)
        {
            _postRepository.Setup(pr => pr.Find(p => p.Id == postID))
                .ReturnsAsync(new Models.Post()
                {
                    Id = postID,
                    Title = "Dummy Title",
                    Author = new Models.ApiUser { Id = Guid.NewGuid().ToString(), Email = "xyz@xyz.org" }
                });

            var response = await _postController.Get(postID) as JsonResult;
            _postRepository.Verify(pr => pr.Find(p => p.Id == postID), Times.Once);

            Assert.NotNull(response);
            var result = response.Value as DTO.Post;
            Assert.NotNull(result);
            Assert.Equal(postID, result.Id);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4452)]
        [InlineData(28592)]
        public async void GetPostShouldReturnNotFound(int postID)
        {
            _postRepository.Setup(pr => pr.Find(p => p.Id == postID))
                .ReturnsAsync(It.IsAny<Models.Post>());

            var response = await _postController.Get(postID) as ObjectResult;
            _postRepository.Verify(pr => pr.Find(p => p.Id == postID), Times.Once);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async void CreatePostSuccess()
        {
            var userMock = new Mock<ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirst(It.IsAny<string>())).Returns(It.IsAny<Claim>());
            var httpContext = Mock.Of<HttpContext>(_ => _.User == userMock.Object);
            var controllerContext = new ControllerContext() { HttpContext = httpContext };
            var postController = new PostsController(_postRepository.Object) { ControllerContext = controllerContext };

            var postRequest = new Requests.PostRequest { Title = "Dummy Post", Body = "This is a Dummy Post" };
            var postEntity = new Models.Post { Id = 1, Title = postRequest.Title, Description = postRequest.Body, Author = new Models.ApiUser { Id = Guid.NewGuid().ToString(), Email = "xyz@xyz.org" } };
            _postRepository.Setup(pr => pr.CreatePost(postRequest)).ReturnsAsync(postEntity);

            var response = await postController.Post(postRequest) as JsonResult;
            userMock.Verify(u => u.FindFirst(It.IsAny<string>()), Times.Once);
            _postRepository.Verify(pr => pr.CreatePost(postRequest), Times.Once);

            Assert.NotNull(response);
            var result = response.Value as DTO.Post;
            Assert.NotNull(result);
            Assert.Equal(postRequest.Title, result.Title);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(204349)]
        [InlineData(604396429)]
        public async void DeletePostShouldReturnNoContent(int postID)
        {
            var postEntity = new Models.Post { Id = postID, Title = "Dummy Title", Description = "Dummy description", Author = new Models.ApiUser { Id = Guid.NewGuid().ToString(), Email = "xyz@xyz.org" } };
            _postRepository.Setup(pr => pr.Find(p => p.Id == postID)).ReturnsAsync(postEntity);
            _postRepository.Setup(pr => pr.Delete(postEntity));

            var response = await _postController.Delete(postID) as NoContentResult;
            _postRepository.Verify(pr => pr.Find(p => p.Id == postID), Times.Once);
            _postRepository.Verify(pr => pr.Delete(postEntity), Times.Once);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(204349)]
        [InlineData(604396429)]
        public async void DeletePostShouldReturnNotFound(int postID)
        {
            _postRepository.Setup(pr => pr.Find(p => p.Id == postID)).ReturnsAsync(It.IsAny<Models.Post>());

            var response = await _postController.Delete(postID) as NotFoundResult;
            _postRepository.Verify(pr => pr.Find(p => p.Id == postID), Times.Once);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}