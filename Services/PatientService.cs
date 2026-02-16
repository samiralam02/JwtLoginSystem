using OfficeOpenXml;
using MySql.Data.MySqlClient;
using JwtLoginSystem.Data;
using JwtLoginSystem.Models.DTOs;

namespace JwtLoginSystem.Services
{
    public class PatientService : IPatientService
    {
        private readonly DatabaseContext _dbContext;

        public PatientService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PatientUploadResponseDto> UploadPatientsFromExcelAsync(IFormFile file, string uploadedBy)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("No file uploaded");
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                throw new InvalidOperationException("Only Excel files (.xlsx, .xls) are allowed");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int patientsAdded = 0;

            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // First sheet
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        throw new InvalidOperationException("Excel file is empty or has no data rows");
                    }

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var name = worksheet.Cells[row, 1].Text;
                        var dobText = worksheet.Cells[row, 2].Text;
                        var ageText = worksheet.Cells[row, 3].Text;

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        if (!DateTime.TryParse(dobText, out DateTime dob))
                        {
                            Console.WriteLine($"Warning: Invalid date format in row {row}, skipping...");
                            continue;
                        }

                        if (!int.TryParse(ageText, out int age))
                        {
                            Console.WriteLine($"Warning: Invalid age format in row {row}, skipping...");
                            continue;
                        }

                        var insertQuery = @"
                            INSERT INTO Patients (Name, DateOfBirth, Age, LoadedBy, CreatedAt)
                            VALUES (@Name, @DateOfBirth, @Age, @LoadedBy, @CreatedAt)";

                        using var command = new MySqlCommand(insertQuery, connection);
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@DateOfBirth", dob);
                        command.Parameters.AddWithValue("@Age", age);
                        command.Parameters.AddWithValue("@LoadedBy", uploadedBy);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                        await command.ExecuteNonQueryAsync();
                        patientsAdded++;
                    }
                }
            }

            return new PatientUploadResponseDto
            {
                Success = true,
                Message = $"Successfully uploaded {patientsAdded} patients",
                TotalPatients = patientsAdded,
                UploadedBy = uploadedBy
            };
        }

        public async Task<List<PatientListDto>> GetAllPatientsAsync()
        {
            var patients = new List<PatientListDto>();

            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT Id, Name, DateOfBirth, Age, LoadedBy, CreatedAt FROM Patients ORDER BY CreatedAt DESC";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                patients.Add(new PatientListDto
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

        public async Task<List<PatientListDto>> GetPatientsByUploaderAsync(string uploaderName)
        {
            var patients = new List<PatientListDto>();

            using var connection = _dbContext.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT Id, Name, DateOfBirth, Age, LoadedBy, CreatedAt FROM Patients WHERE LoadedBy = @LoadedBy ORDER BY CreatedAt DESC";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@LoadedBy", uploaderName);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                patients.Add(new PatientListDto
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