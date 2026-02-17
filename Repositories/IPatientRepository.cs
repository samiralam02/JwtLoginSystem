using JwtLoginSystem.Models;

namespace JwtLoginSystem.Repositories
{
    public interface IPatientRepository
    {
        Task AddPatientAsync(Patient patient);
        Task<List<Patient>> GetAllPatientsAsync();
        Task<List<Patient>> GetPatientsByUploaderAsync(string uploaderName);
    }
}