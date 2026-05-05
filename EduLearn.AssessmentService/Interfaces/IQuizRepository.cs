using EduLearn.AssessmentService.DTOs;
using EduLearn.AssessmentService.Entities;

namespace EduLearn.AssessmentService.Interfaces;

// Repository interface — all DB operations for Quiz and QuizAttempt
public interface IQuizRepository
{
    // Quiz operations
    Task<Quiz?> FindByQuizId(int quizId);
    Task<IList<Quiz>> FindByCourseId(int courseId);
    Task<Quiz?> FindByLessonId(int lessonId);      // lesson-level quiz
    Task<Quiz> Create(Quiz quiz);
    Task<Quiz> Update(Quiz quiz);
    Task Delete(int quizId);

    // Attempt operations
    Task<QuizAttempt?> FindAttemptById(int attemptId);
    Task<IList<QuizAttempt>> FindAttemptsByStudentAndQuiz(int studentId, int quizId);
    Task<int> CountAttempts(int studentId, int quizId);  // checks against MaxAttempts
    Task<QuizAttempt?> FindBestAttempt(int studentId, int quizId); // highest score
    Task<QuizAttempt> CreateAttempt(QuizAttempt attempt);
    Task<QuizAttempt> UpdateAttempt(QuizAttempt attempt);
}

// Service interface — all business logic for Quiz and Attempt management
public interface IQuizService
{
    // Quiz CRUD — Instructor only
    Task<QuizResponseDto> CreateQuiz(CreateQuizDto dto);
    Task<QuizResponseDto?> GetQuizById(int quizId);
    Task<IList<QuizResponseDto>> GetQuizzesByCourse(int courseId);
    Task<QuizResponseDto?> GetQuizByLesson(int lessonId);
    Task<QuizResponseDto> UpdateQuiz(int quizId, CreateQuizDto dto);
    Task DeleteQuiz(int quizId);
    Task PublishQuiz(int quizId);

    // Get quiz questions for student — correct answers hidden
    Task<IList<StudentQuestionDto>> GetQuestionsForStudent(int quizId);

    // Attempt lifecycle
    // StartAttempt: checks CountAttempts < MaxAttempts before creating
    Task<AttemptResponseDto> StartAttempt(int quizId, int studentId);

    // SubmitAttempt: computes score, sets IsPassed, persists attempt
    Task<AttemptResponseDto> SubmitAttempt(int attemptId, SubmitAttemptDto dto);

    // Get all attempts by student for a quiz
    Task<IList<AttemptResponseDto>> GetAttemptsByStudent(int studentId, int quizId);

    // GetBestAttempt: returns attempt with highest score
    Task<AttemptResponseDto?> GetBestAttempt(int studentId, int quizId);

    // Count attempts — used to check if student can retake
    Task<int> GetAttemptCount(int studentId, int quizId);
}