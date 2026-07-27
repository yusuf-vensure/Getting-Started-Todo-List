using System.Data;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS so Angular (localhost:4200) can talk to this API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Configure Redis
var redisHost = builder.Configuration["Redis:Host"] ?? "redis";
var redisConnection = ConnectionMultiplexer.Connect($"{redisHost}:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

var app = builder.Build();

app.UseCors();

// Connection String for MSSQL
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=db;Database=db1;User Id=sa;Password=YourStrong!Password123;TrustServerCertificate=True;";

// 1. Retry loop to wait for MSSQL to start up
Console.WriteLine("Waiting for MSSQL to be ready...");
bool connected = false;

while (!connected)
{
    try
    {
        // FIX: Temporarily swap the connection string to target 'master' so we can create db1
        var setupConnectionString = connectionString.Replace("Database=db1;", "Database=master;");
        using var connection = new SqlConnection(setupConnectionString);
        connection.Open();
        
        // Ensure Database & Users Table exist
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'db1')
            BEGIN
                CREATE DATABASE db1;
            END;
        ";
        cmd.ExecuteNonQuery();

        // Switch to db1 and create users table
        connection.ChangeDatabase("db1");
        cmd.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            BEGIN
                CREATE TABLE users (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100)
                );
            END;
        ";
        cmd.ExecuteNonQuery();

        Console.WriteLine("Connected to MSSQL successfully!");
        connected = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database not ready, retrying in 3 seconds... ({ex.Message})");
        Thread.Sleep(3000);
    }
}

// --- API ENDPOINTS ---

// Real Redis logic to track views
app.MapGet("/page-views", (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    long views = db.StringIncrement("page_views");
    return $"This page has been viewed {views} times!";
});

// Real Redis logic to track views
app.MapGet("/api/stats", (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    long views = db.StringIncrement("page_views");
    return Results.Ok(new
    {
        message = "Welcome to the Todo App!",
        total_page_loads = views
    });
});

// Equivalent to @app.route('/hello')
app.MapGet("/hello", () => "Live reload is working!");

// Equivalent to @app.route('/add-user', methods=['POST'])
app.MapPost("/add-user", (HttpRequest request) =>
{
    var name = request.Form["name"].ToString();

    using var connection = new SqlConnection(connectionString);
    connection.Open();
    using var cmd = connection.CreateCommand();
    
    // MSSQL parameter syntax uses '@name' instead of '%s'
    cmd.CommandText = "INSERT INTO users (name) VALUES (@name)";
    cmd.Parameters.AddWithValue("@name", name);
    cmd.ExecuteNonQuery();

    return Results.Ok($"Saved {name}!");
});

app.Run();