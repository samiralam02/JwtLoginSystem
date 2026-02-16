using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JwtLoginSystem.Services;

namespace JwtLoginSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // All endpoints require authentication
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            try
            {
                var uploadedBy = User.FindFirst("FullName")?.Value;

                if (string.IsNullOrEmpty(uploadedBy))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User name not found. Please login again."
                    });
                }

                var result = await _patientService.UploadPatientsFromExcelAsync(file, uploadedBy);

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    data = new
                    {
                        totalPatients = result.TotalPatients,
                        uploadedBy = result.UploadedBy
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while uploading patients records",
                    error = ex.Message
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllPatients()
        {
            try
            {
                var patients = await _patientService.GetAllPatientsAsync();

                return Ok(new
                {
                    success = true,
                    message = "Patients retrieved successfully",
                    data = patients,
                    total = patients.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving patients",
                    error = ex.Message
                });
            }
        }

        [HttpGet("my-uploads")]
        public async Task<IActionResult> GetMyUploads()
        {
            try
            {
                var uploaderName = User.FindFirst("FullName")?.Value;

                if (string.IsNullOrEmpty(uploaderName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User name not found"
                    });
                }

                var patients = await _patientService.GetPatientsByUploaderAsync(uploaderName);

                return Ok(new
                {
                    success = true,
                    message = "Your uploads retrieved successfully",
                    data = patients,
                    total = patients.Count,
                    uploadedBy = uploaderName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving your uploads",
                    error = ex.Message
                });
            }
        }
    }
}