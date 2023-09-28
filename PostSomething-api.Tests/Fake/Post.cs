using Bogus;

namespace PostSomething_api.Tests.Fake
{
    public class Post : Faker<Models.Post>
    {
        public Post(int? postID = null)
        {
            RuleFor(obj => obj.Id, faker => postID ?? faker.Random.Int())
                .RuleFor(obj => obj.Title, faker => faker.Lorem.Sentence())
                .RuleFor(obj => obj.Description, faker => faker.Lorem.Paragraph())
                .RuleFor(obj => obj.Author, new ApiUser().Generate());
        }
    }
}