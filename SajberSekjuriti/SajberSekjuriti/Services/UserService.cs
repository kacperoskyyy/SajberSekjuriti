using MongoDB.Driver;
using SajberSekjuriti.Model;
namespace SajberSekjuriti.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        //Konstruktor, polaczenie z baza danych
        public UserService(IMongoClient client, IConfiguration config)
        {
            var dbName = config["MongoDbSettings:DatabaseName"];
            var colName = config["MongoDbSettings:UsersCollectionName"];
            var database = client.GetDatabase(dbName);
            _users = database.GetCollection<User>(colName);
        }
        //Pobieranie uzytkownika po nazwie
        public async Task<User?> GetByUsernameAsync(string username) => await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        //Tworzenie nowego uzytkownika
        public async Task CreateAsync(User user) => await _users.InsertOneAsync(user);
        //Aktualizacja uzytkownika
        public async Task UpdateAsync(User userToUpdate) => await _users.ReplaceOneAsync(u => u.Id == userToUpdate.Id, userToUpdate);
        //Pobieranie wszystkich uzytkownikow
        public async Task<List<User>> GetAllAsync() => await _users.Find(_ => true).ToListAsync();
        //Pobieranie uzytkownika po ID
        public async Task<User?> GetByIdAsync(string id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        //Usuwanie uzytkownika po ID
        public async Task DeleteAsync(string id) =>await _users.DeleteOneAsync(u => u.Id == id);
    }
}
