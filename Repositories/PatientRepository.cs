using MySql.Data.MySqlClient;
using JwtLoginSystem.Data;
using JwtLoginSystem.Models;

namespace JwtLoginSystem.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly DatabaseContext _dbContext;

        public PatientRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Save one patient to database
        public async Task AddPatientAsync(Patient patient)
        {
            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO Patients (Name, DateOfBirth, Age, LoadedBy, CreatedAt)
                VALUES (@Name, @DateOfBirth, @Age, @LoadedBy, @CreatedAt)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", patient.Name);
            command.Parameters.AddWithValue("@DateOfBirth", patient.DateOfBirth);
            command.Parameters.AddWithValue("@Age", patient.Age);
            command.Parameters.AddWithValue("@LoadedBy", patient.LoadedBy);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        // Get all patients from database
        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            var patients = new List<Patient>();

            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT Id, Name, DateOfBirth, Age, LoadedBy, CreatedAt FROM Patients ORDER BY CreatedAt DESC";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                patients.Add(new Patient
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DateOfBirth = reader.GetDateTime(2),
                    Age = reader.GetInt32(3),
                    LoadedBy = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                });
            }

            return patients;
        }

        // Get patients uploaded by a specific user
        public async Task<List<Patient>> GetPatientsByUploaderAsync(string uploaderName)
        {
            var patients = new List<Patient>();

            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT Id, Name, DateOfBirth, Age, LoadedBy, CreatedAt FROM Patients WHERE LoadedBy = @LoadedBy ORDER BY CreatedAt DESC";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@LoadedBy", uploaderName);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                patients.Add(new Patient
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DateOfBirth = reader.GetDateTime(2),
                    Age = reader.GetInt32(3),
                    LoadedBy = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                });
            }

            return patients;
        }
    }
}