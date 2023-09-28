using System.Linq.Expressions;

using Microsoft.AspNetCore.Http;

using Moq;

using PostSomething_api.Repositories.Interface;
using PostSomething_api.Requests;
using PostSomething_api.Services.Implementation;
using PostSomething_api.Services.Interface;

namespace PostSomething_api.Tests.Services
{
    public class CommentServiceTest
    {
        private const int MAX_COMMENTS = 2000;
        private readonly Random random = new();
        private readonly Mock<ICommentRepository> _commentRepositoryStub;
        private readonly Mock<IPostRepository> _postRepositoryStub;
        private readonly Mock<IUserManager<Models.ApiUser>> _userManagerStub;
        private readonly CommentsService _commentsServiceMock;

        public CommentServiceTest()
        {
            _commentRepositoryStub = new Mock<ICommentRepository>();
            _postRepositoryStub = new Mock<IPostRepository>();
            _userManagerStub = new Mock<IUserManager<Models.ApiUser>>();
            _commentsServiceMock = new CommentsService(_commentRepositoryStub.Object, _postRepositoryStub.Object, _userManagerStub.Object);
        }

        [Fact]
        public async void GetCommentsFromPostShouldReturnSuccess()
        {
            int postID = random.Next();
            var commentsReturned = new Fake.Comment(postID).Generate(random.Next(MAX_COMMENTS));

            _commentRepositoryStub.Setup(cr => cr.GetCommentsFromPost(It.IsAny<int>())).ReturnsAsync(commentsReturned);

            var response = await _commentsServiceMock.GetCommentsFromPost(postID);

            Assert.NotNull(response);
            Assert.Equal(commentsReturned, response);
        }

        [Fact]
        public async void GetCommentShouldReturnSuccess()
        {
            var comment = new Fake.Comment().Generate();

            _commentRepositoryStub.Setup(cr => cr.GetComment(comment.Id)).ReturnsAsync(comment);

            var response = await _commentsServiceMock.GetComment(comment.Id);

            Assert.NotNull(response);
            Assert.Equal(comment.Id, response.Id);
        }

        [Fact]
        public async void CreateCommentFromPostShouldReturnSuccess()
        {
            var dummyPost = new Fake.Post(random.Next()).Generate();
            var dummyUser = new Fake.ApiUser().Generate();
            var dummyComment = new Fake.Comment(dummyPost.Id).Generate();
            dummyComment.Parent = new Fake.Comment(dummyPost.Id).Generate();

            var commentRequest = new CommentRequest { Content = dummyComment.Content };

            _postRepositoryStub.Setup(pr => pr.Find(It.IsAny<Expression<Func<Models.Post, bool>>>())).ReturnsAsync(dummyPost);
            _userManagerStub.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(dummyUser);
            _commentRepositoryStub.Setup(cr => cr.Find(It.IsAny<Expression<Func<Models.Comment, bool>>>())).ReturnsAsync(dummyComment.Parent);

            _commentRepositoryStub.Setup(cr => cr.CreateComment(commentRequest, dummyUser, dummyPost, dummyComment.Parent)).ReturnsAsync(dummyComment);

            var response = await _commentsServiceMock.CreateCommentFromPost(dummyPost.Id, commentRequest, dummyUser.Id);

            _postRepositoryStub.Verify(pr => pr.Find(It.IsAny<Expression<Func<Models.Post, bool>>>()));
            _userManagerStub.Verify(um => um.FindByIdAsync(It.IsAny<string>()));
            _commentRepositoryStub.Verify(cr => cr.Find(It.IsAny<Expression<Func<Models.Comment, bool>>>()));
            _commentRepositoryStub.Verify(cr => cr.CreateComment(commentRequest, dummyUser, dummyPost, dummyComment.Parent));

            Assert.NotNull(response);
            Assert.Equal(dummyComment, response);
        }

        [Fact(DisplayName = "Create comment from post should return argument null exception, because post could not be found")]
        public async void CreateCommentFromPostShouldReturnArgumentNullException()
        {
            var dummyPost = new Fake.Post(random.Next()).Generate();
            var dummyUser = new Fake.ApiUser().Generate();
            var dummyComment = new Fake.Comment(dummyPost.Id).Generate();
            dummyComment.Parent = new Fake.Comment().Generate();

            var commentRequest = new CommentRequest { Content = dummyComment.Content };

            _postRepositoryStub.Setup(pr => pr.Find(It.IsAny<Expression<Func<Models.Post, bool>>>())).ReturnsAsync(dummyPost);
            _userManagerStub.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(dummyUser);
            _commentRepositoryStub.Setup(cr => cr.Find(It.IsAny<Expression<Func<Models.Comment, bool>>>())).ReturnsAsync(dummyComment.Parent);

            _commentRepositoryStub.Setup(cr => cr.CreateComment(commentRequest, dummyUser, dummyPost, dummyComment.Parent)).ReturnsAsync(dummyComment);

            await Assert.ThrowsAsync<BadHttpRequestException>(() => _commentsServiceMock.CreateCommentFromPost(dummyPost.Id, commentRequest, dummyUser.Id));

            _postRepositoryStub.Verify(pr => pr.Find(It.IsAny<Expression<Func<Models.Post, bool>>>()));
            _userManagerStub.Verify(um => um.FindByIdAsync(It.IsAny<string>()));
            _commentRepositoryStub.Verify(cr => cr.Find(It.IsAny<Expression<Func<Models.Comment, bool>>>()));
            _commentRepositoryStub.Verify(cr => cr.CreateComment(commentRequest, dummyUser, dummyPost, dummyComment.Parent), Times.Never);
        }

        [Fact(DisplayName = "Create comment from post should return bad request exception, because parent comment must belong to the same post as child.")]
        public async void CreateCommentFromPostShouldReturnBadRequest()
        {
            var dummyPost = new Fake.Post(random.Next()).Generate();
            var dummyUser = new Fake.ApiUser().Generate();
            var dummyComment = new Fake.Comment(dummyPost.Id).Generate();
            dummyComment.Parent = new Fake.Comment(dummyPost.Id).Generate();

            var commentRequest = new CommentRequest { Content = dummyComment.Content };

            _postRepositoryStub.Setup(pr => pr.Find(It.IsAny<Expression<Func<Models.Post, bool>>>())).ReturnsAsync(It.IsAny<Models.Post>());
            _userManagerStub.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(dummyUser);
            _commentRepositoryStub.Setup(cr => cr.Find(It.IsAny<Expression<Func<Models.Comment, bool>>>())).ReturnsAsync(dummyComment.Parent);

            _commentRepositoryStub.Setup(cr => cr.CreateComment(commentRequest, dummyUser, dummyPost, dummyComment.Parent)).ReturnsAsync(dummyComment);

            await Assert.ThrowsAsync<ArgumentNullException>(() => _commentsServiceMock.CreateCommentFromPost(dummyPost.Id, commentRequest, dummyUser.Id));

            _postRepositoryStub.Verify(pr => pr.Find(It.IsAny<Expression<Func<Models.Post, bool>>>()));
            _userManagerStub.Verify(um => um.FindByIdAsync(It.IsAny<string>()));
            _commentRepositoryStub.Verify(cr => cr.Find(It.IsAny<Expression<Func<Models.Comment, bool>>>()));
            _commentRepositoryStub.Verify(cr => cr.CreateComment(commentRequest, dummyUser, dummyPost, dummyComment.Parent), Times.Never);
        }

        [Theory]
        [InlineData(129)]
        [InlineData(51553)]
        public async void DeleteShouldSuccess(int commentID)
        {
            var comment = new Fake.Comment().Generate();
            comment.Id = commentID;

            _commentRepositoryStub.Setup(cr => cr.Find(comment => comment.Id == commentID)).ReturnsAsync(comment);
            _commentRepositoryStub.Setup(cr => cr.Delete(comment));

            await _commentsServiceMock.Delete(commentID);
            _commentRepositoryStub.Verify(cr => cr.Find(comment => comment.Id == commentID), Times.Once);
            _commentRepositoryStub.Verify(cr => cr.Delete(comment), Times.Once);
        }

        [Fact]
        public async void DeleteShouldThrowArgumentNullException()
        {
            var comment = new Fake.Comment().Generate();

            _commentRepositoryStub.Setup(cr => cr.Find(c => c.Id == comment.Id)).ReturnsAsync(comment);

            await Assert.ThrowsAsync<ArgumentNullException>(() => _commentsServiceMock.Delete(comment.Id));
            _commentRepositoryStub.Verify(cr => cr.Find(c => c.Id == comment.Id), Times.Never);
        }
    }
}