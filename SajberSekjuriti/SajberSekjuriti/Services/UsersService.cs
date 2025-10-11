using MongoDB.Driver;
namespace SajberSekjuriti.Services
{
    public class UsersService
    {
        private readonly IMongoCollection<User> _users;
        public UsersService(IMongoClient client, IConfiguration config)
        {
            var dbName = config["MongoDbSettings:DatabaseName"];
            var colName = config["MongoDbSettings:UsersCollectionName"];
            var database = client.GetDatabase(dbName);
            _users = database.GetCollection<User>(colName);
        }
        public async Task<User?> GetByUsernameAsync(string username) => await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        public async Task CreateAsync(User user) => await _users.InsertOneAsync(user);
    }
}
