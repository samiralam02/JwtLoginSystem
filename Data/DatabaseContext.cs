using MySql.Data.MySqlClient;

namespace JwtLoginSystem.Data
{
    public class DatabaseContext
    {
        private readonly string _connectionString;

        public DatabaseContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
               ?? throw new InvalidOperationException("Connection string not found");
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                var createUsersTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Email VARCHAR(255) UNIQUE NOT NULL,
                        PasswordHash VARCHAR(255) NOT NULL,
                        FullName VARCHAR(255) NOT NULL,
                        DateOfBirth DATE NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        INDEX idx_email (Email)
                    );";

                using var usersCommand = new MySqlCommand(createUsersTableQuery, connection);
                await usersCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Users table created successfully!");

                var createPatientsTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Patients (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Name VARCHAR(255) NOT NULL,
                        DateOfBirth DATE NOT NULL,
                        Age INT NOT NULL,
                        LoadedBy VARCHAR(255) NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        INDEX idx_loadedby (LoadedBy)
                    );";

                using var patientsCommand = new MySqlCommand(createPatientsTableQuery, connection);
                await patientsCommand.ExecuteNonQueryAsync();
                Console.WriteLine("Patients table created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating database tables: {ex.Message}");
                throw;
            }
        }
    }
}