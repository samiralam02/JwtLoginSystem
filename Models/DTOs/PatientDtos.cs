namespace JwtLoginSystem.Models.DTOs
{
    public class PatientUploadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalPatients { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
    }

    public class PatientListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string LoadedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}