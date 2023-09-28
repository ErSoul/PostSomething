using Bogus;

namespace PostSomething_api.Tests.Fake
{
    public class ApiUser : Faker<Models.ApiUser>
    {
        public ApiUser()
        {
            RuleFor(obj => obj.Id, faker => faker.Random.Guid().ToString());
            RuleFor(obj => obj.UserName, faker => faker.Internet.UserName());
            RuleFor(obj => obj.Email, faker => faker.Internet.Email());
            RuleFor(obj => obj.PasswordHash, faker => faker.Internet.Password());
            RuleFor(obj => obj.Address, faker => faker.Address.ToString());
        }
    }
}