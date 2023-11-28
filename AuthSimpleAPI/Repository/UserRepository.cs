using AuthSimpleAPI.Models;
using MongoDB.Driver;

namespace AuthSimpleAPI.Repository;

public class UserRepository
{
    private readonly IMongoCollection<UserModel> _users;

    public UserRepository(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        _users = database.GetCollection<UserModel>("Users");
    }

    public List<UserModel> GetUsers()
    {
        return _users.Find(user => true).ToList();
    }

    public UserModel GetUserByUsername(string username)
    {
        return _users.Find(user => user.Username == username).FirstOrDefault();
    }

    public UserModel GetUserByID(string ID)
    {
        return _users.Find(user => user.Id == ID).FirstOrDefault();
    }

    public UserModel InsertUser(UserModel userModel)
    {
        _users.InsertOne(userModel);
        return userModel;
    }
}