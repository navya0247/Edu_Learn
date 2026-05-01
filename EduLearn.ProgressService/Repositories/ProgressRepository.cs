using EduLearn.ProgressService.Data;
using EduLearn.ProgressService.Entities;
using EduLearn.ProgressService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ProgressService.Repositories;

// EF Core implementation for LessonProgress
public class ProgressRepository : IProgressRepository
{
    private readonly ProgressDbContext _db;

    public ProgressRepository(ProgressDbContext db) => _db = db;

    public async Task<LessonProgress?> FindByStudentAndLesson(int studentId, int lessonId)
        => await _db.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.StudentId == studentId && lp.LessonId == lessonId);

    public async Task<IList<LessonProgress>> FindByCourseAndStudent(int studentId, int courseId)
        => await _db.LessonProgresses
            .Where(lp => lp.StudentId == studentId && lp.CourseId == courseId)
            .ToListAsync();

    public async Task<int> CountCompletedLessons(int studentId, int courseId)
        => await _db.LessonProgresses
            .CountAsync(lp => lp.StudentId == studentId
                           && lp.CourseId == courseId
                           && lp.IsCompleted);

    public async Task<LessonProgress> Create(LessonProgress progress)
    {
        _db.LessonProgresses.Add(progress);
        await _db.SaveChangesAsync();
        return progress;
    }

    public async Task<LessonProgress> Update(LessonProgress progress)
    {
        _db.LessonProgresses.Update(progress);
        await _db.SaveChangesAsync();
        return progress;
    }
}

// EF Core implementation for Certificate
public class CertificateRepository : ICertificateRepository
{
    private readonly ProgressDbContext _db;

    public CertificateRepository(ProgressDbContext db) => _db = db;

    // ✅ ADDED - Find by ID for download
    public async Task<Certificate?> FindById(int certificateId)
        => await _db.Certificates
            .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

    public async Task<Certificate?> FindByStudentAndCourse(int studentId, int courseId)
        => await _db.Certificates
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId);

    public async Task<Certificate?> FindByCertificateCode(string code)
        => await _db.Certificates
            .FirstOrDefaultAsync(c => c.CertificateCode == code);

    public async Task<IList<Certificate>> FindByStudentId(int studentId)
        => await _db.Certificates
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

    public async Task<Certificate> Create(Certificate certificate)
    {
        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync();
        return certificate;
    }

    public async Task<Certificate> Update(Certificate certificate)
    {
        _db.Certificates.Update(certificate);
        await _db.SaveChangesAsync();
        return certificate;
    }
}