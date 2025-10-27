using MongoDB.Driver;
using SajberSekjuriti.Model;
namespace SajberSekjuriti.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;
    public UserService(IMongoClient client, IConfiguration config)
    {
        var dbName = config["MongoDbSettings:DatabaseName"];
        var colName = config["MongoDbSettings:UsersCollectionName"];
        var database = client.GetDatabase(dbName);
        _users = database.GetCollection<User>(colName);
    }
    public async Task<User?> GetByUsernameAsync(string username) => await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
    public async Task CreateAsync(User user) => await _users.InsertOneAsync(user);
    public async Task UpdateAsync(User userToUpdate) => await _users.ReplaceOneAsync(u => u.Id == userToUpdate.Id, userToUpdate);
    public async Task<List<User>> GetAllAsync() => await _users.Find(_ => true).ToListAsync();
    public async Task<User?> GetByIdAsync(string id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    public async Task DeleteAsync(string id) =>await _users.DeleteOneAsync(u => u.Id == id);
}
