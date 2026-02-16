using JwtLoginSystem.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace JwtLoginSystem.Services
{
    public interface IPatientService
    {
        Task<PatientUploadResponseDto> UploadPatientsFromExcelAsync(IFormFile file, string uploadedBy);
        Task<List<PatientListDto>> GetAllPatientsAsync();
        Task<List<PatientListDto>> GetPatientsByUploaderAsync(string uploaderName);
    }
}