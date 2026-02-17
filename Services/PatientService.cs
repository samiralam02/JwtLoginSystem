using OfficeOpenXml;
using JwtLoginSystem.Models;
using JwtLoginSystem.Models.DTOs;
using JwtLoginSystem.Repositories;

namespace JwtLoginSystem.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;

        public PatientService(IPatientRepository patientRepository)
        {
            _patientRepository = patientRepository;
        }

        public async Task<PatientUploadResponseDto> UploadPatientsFromExcelAsync(IFormFile file, string uploadedBy)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file uploaded");

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                throw new InvalidOperationException("Only Excel files (.xlsx, .xls) are allowed");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int patientsAdded = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
                throw new InvalidOperationException("Excel file is empty or has no data rows");

            for (int row = 2; row <= rowCount; row++)
            {
                var name = worksheet.Cells[row, 1].Text;
                var dobText = worksheet.Cells[row, 2].Text;
                var ageText = worksheet.Cells[row, 3].Text;

                if (string.IsNullOrWhiteSpace(name)) continue;

                if (!DateTime.TryParse(dobText, out DateTime dob))
                {
                    Console.WriteLine($"Warning: Invalid date in row {row}, skipping...");
                    continue;
                }

                if (!int.TryParse(ageText, out int age))
                {
                    Console.WriteLine($"Warning: Invalid age in row {row}, skipping...");
                    continue;
                }

                var patient = new Patient
                {
                    Name = name,
                    DateOfBirth = dob,
                    Age = age,
                    LoadedBy = uploadedBy 
                };

                await _patientRepository.AddPatientAsync(patient);
                patientsAdded++;
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
            var patients = await _patientRepository.GetAllPatientsAsync();

            return patients.Select(p => new PatientListDto
            {
                Id = p.Id,
                Name = p.Name,
                DateOfBirth = p.DateOfBirth,
                Age = p.Age,
                LoadedBy = p.LoadedBy,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<List<PatientListDto>> GetPatientsByUploaderAsync(string uploaderName)
        {
            var patients = await _patientRepository.GetPatientsByUploaderAsync(uploaderName);

            return patients.Select(p => new PatientListDto
            {
                Id = p.Id,
                Name = p.Name,
                DateOfBirth = p.DateOfBirth,
                Age = p.Age,
                LoadedBy = p.LoadedBy,
                CreatedAt = p.CreatedAt
            }).ToList();
        }
    }
}