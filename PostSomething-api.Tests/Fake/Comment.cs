using Bogus;

namespace PostSomething_api.Tests.Fake
{
    public class Comment : Faker<Models.Comment>
    {
        public Comment(int? postID = null, string? content = null)
        {
            RuleFor(obj => obj.Id, faker => faker.Random.Number())
                .RuleFor(obj => obj.Content, faker => content ?? faker.Lorem.Paragraph())
                .RuleFor(obj => obj.Post, new Post(postID).Generate())
                .RuleFor(obj => obj.Author, new ApiUser().Generate());
        }
    }
}