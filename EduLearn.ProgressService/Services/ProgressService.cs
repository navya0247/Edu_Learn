using EduLearn.ProgressService.DTOs;
using EduLearn.ProgressService.Entities;
using EduLearn.ProgressService.Interfaces;
using System.Text;
using System.Text.Json;

namespace EduLearn.ProgressService.Services;

public class ProgressService : IProgressService
{
    private readonly IProgressRepository _repo;
    private readonly ICertificateRepository _certRepo;
    private readonly ILogger<ProgressService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProgressService(
        IProgressRepository repo,
        ICertificateRepository certRepo,
        ILogger<ProgressService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _repo = repo;
        _certRepo = certRepo;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<LessonProgressDto> TrackLesson(TrackLessonDto dto)
    {
        var existing = await _repo.FindByStudentAndLesson(dto.StudentId, dto.LessonId);

        if (existing == null)
        {
            var progress = new LessonProgress
            {
                StudentId = dto.StudentId,
                LessonId = dto.LessonId,
                CourseId = dto.CourseId,
                IsCompleted = false,
                StartedAt = DateTime.UtcNow,
                WatchPercent = dto.WatchPercent
            };
            var created = await _repo.Create(progress);
            return MapToDto(created);
        }

        if (dto.WatchPercent > existing.WatchPercent)
            existing.WatchPercent = dto.WatchPercent;

        var updated = await _repo.Update(existing);
        return MapToDto(updated);
    }

    public async Task<LessonProgressDto> CompleteLesson(CompleteLessonDto dto)
    {
        var existing = await _repo.FindByStudentAndLesson(dto.StudentId, dto.LessonId);

        if (existing == null)
        {
            var progress = new LessonProgress
            {
                StudentId = dto.StudentId,
                LessonId = dto.LessonId,
                CourseId = dto.CourseId,
                IsCompleted = true,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                WatchPercent = 100
            };
            var created = await _repo.Create(progress);
            
            // ✅ Update enrollment progress after completion
            await UpdateEnrollmentProgress(dto.StudentId, dto.CourseId);
            
            _logger.LogInformation("Student {StudentId} completed lesson {LessonId}", dto.StudentId, dto.LessonId);
            return MapToDto(created);
        }

        existing.IsCompleted = true;
        existing.CompletedAt = DateTime.UtcNow;
        existing.WatchPercent = 100;

        var updated = await _repo.Update(existing);
        
        // ✅ Update enrollment progress after completion
        await UpdateEnrollmentProgress(dto.StudentId, dto.CourseId);
        
        _logger.LogInformation("Student {StudentId} completed lesson {LessonId}", dto.StudentId, dto.LessonId);
        return MapToDto(updated);
    }

    public async Task<IList<LessonProgressDto>> GetLessonProgress(int studentId, int courseId)
    {
        var records = await _repo.FindByCourseAndStudent(studentId, courseId);
        return records.Select(MapToDto).ToList();
    }

    public async Task<CourseProgressDto> GetCourseProgress(int studentId, int courseId, int totalLessons)
    {
        var completed = await _repo.CountCompletedLessons(studentId, courseId);
        var percent = totalLessons > 0
            ? (int)Math.Round((double)completed / totalLessons * 100)
            : 0;

        return new CourseProgressDto
        {
            StudentId = studentId,
            CourseId = courseId,
            TotalLessons = totalLessons,
            CompletedLessons = completed,
            ProgressPercent = percent,
            IsCourseComplete = percent >= 100
        };
    }

    // ✅ NEW: Update enrollment progress in Enrollment Service
    private async Task UpdateEnrollmentProgress(int studentId, int courseId)
    {
        try
        {
            // Get total lessons for this course
            int totalLessons = await GetTotalLessons(courseId);
            
            // Get completed lessons count
            int completedLessons = await _repo.CountCompletedLessons(studentId, courseId);
            
            // Calculate progress percentage
            int progressPercent = totalLessons > 0 
                ? (int)Math.Round((double)completedLessons / totalLessons * 100) 
                : 0;
            
            // Get enrollment ID
            int enrollmentId = await GetEnrollmentId(studentId, courseId);
            
            if (enrollmentId > 0)
            {
                await CallEnrollmentApi(enrollmentId, progressPercent);
                
                // ✅ Auto-issue certificate when 100% complete
                if (progressPercent == 100)
                {
                    await IssueCertificateAuto(studentId, courseId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update enrollment progress for Student {StudentId}, Course {CourseId}", studentId, courseId);
        }
    }

    // ✅ Get total lessons from Lesson Service
    private async Task<int> GetTotalLessons(int courseId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:5003/api/lessons/count/{courseId}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("totalLessons").GetInt32();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total lessons for course {CourseId}", courseId);
        }
        return 0;
    }

    // ✅ Get enrollment ID from Enrollment Service
    private async Task<int> GetEnrollmentId(int studentId, int courseId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("http://localhost:5004/api/enrollments/my-courses");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.GetProperty("courseId").GetInt32() == courseId)
                        {
                            return item.GetProperty("enrollmentId").GetInt32();
                        }
                    }
                }
                else if (doc.RootElement.TryGetProperty("$values", out var values))
                {
                    foreach (var item in values.EnumerateArray())
                    {
                        if (item.GetProperty("courseId").GetInt32() == courseId)
                        {
                            return item.GetProperty("enrollmentId").GetInt32();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enrollment ID");
        }
        return 0;
    }

    // ✅ Call Enrollment API to update progress
    private async Task CallEnrollmentApi(int enrollmentId, int progressPercent)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { progressPercent }),
                Encoding.UTF8,
                "application/json");
            
            var response = await client.PutAsync(
                $"http://localhost:5004/api/enrollments/{enrollmentId}/progress",
                content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated enrollment {EnrollmentId} progress to {ProgressPercent}%", enrollmentId, progressPercent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call enrollment API");
        }
    }

    // ✅ Auto-issue certificate when course is 100% complete
    private async Task IssueCertificateAuto(int studentId, int courseId)
    {
        try
        {
            // Check if certificate already exists
            var existingCert = await _certRepo.FindByStudentAndCourse(studentId, courseId);
            if (existingCert != null)
            {
                _logger.LogInformation("Certificate already exists for Student {StudentId}, Course {CourseId}", studentId, courseId);
                return;
            }

            // Get student name
            string studentName = $"Student {studentId}";
            string courseName = $"Course {courseId}";
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Get student name from Auth Service
                var studentResponse = await client.GetAsync($"http://localhost:5001/api/auth/users/{studentId}/profile");
                if (studentResponse.IsSuccessStatusCode)
                {
                    var json = await studentResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    studentName = doc.RootElement.GetProperty("fullName").GetString() ?? studentName;
                }
                
                // Get course name from Course Service
                var courseResponse = await client.GetAsync($"http://localhost:5002/api/courses/{courseId}");
                if (courseResponse.IsSuccessStatusCode)
                {
                    var json = await courseResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    courseName = doc.RootElement.GetProperty("title").GetString() ?? courseName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch names for certificate");
            }

            // Create certificate
            var certificate = new Certificate
            {
                StudentId = studentId,
                CourseId = courseId,
                StudentName = studentName,
                CourseName = courseName,
                IssuedAt = DateTime.UtcNow,
                CertificateCode = $"CERT-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}"
            };

            await _certRepo.Create(certificate);
            _logger.LogInformation("✅ Auto-issued certificate {Code} for Student {StudentId}, Course {CourseId}", 
                certificate.CertificateCode, studentId, courseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-issue certificate");
        }
    }

    private static LessonProgressDto MapToDto(LessonProgress lp) => new()
    {
        LessonProgressId = lp.LessonProgressId,
        StudentId = lp.StudentId,
        LessonId = lp.LessonId,
        CourseId = lp.CourseId,
        IsCompleted = lp.IsCompleted,
        StartedAt = lp.StartedAt,
        CompletedAt = lp.CompletedAt,
        WatchPercent = lp.WatchPercent
    };
}