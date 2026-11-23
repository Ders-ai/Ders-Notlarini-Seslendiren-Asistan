using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.DTOs.Learning;

public record VoiceDto(string Id, string Name, string Gender, string Locale);

public record AudioResultDto(
    Guid AudioId,
    string Url,
    string Voice,
    decimal Speed,
    double DurationSeconds,
    bool FromCache);

public record SaveProgressRequest(Guid? SectionId, int PositionSeconds);

public record ListeningProgressDto(
    Guid DocumentId,
    Guid? SectionId,
    int PositionSeconds,
    DateTime UpdatedAt);

public record CreateSummaryRequest(SummaryLength Length = SummaryLength.Medium);

public record SummaryDto(
    Guid Id,
    Guid DocumentId,
    Guid? SectionId,
    SummaryLength Length,
    string Content,
    DateTime CreatedAt);

// ---- FG-6: Bilgi Kartları (Flashcards) ----
public record CreateFlashcardsRequest(string? Title = null);

public record UpdateFlashcardRequest(string Front, string Back);

public record FlashcardDto(Guid Id, string Front, string Back, int OrderIndex);

public record FlashcardDeckDto(
    Guid Id,
    Guid DocumentId,
    Guid? SectionId,
    string Title,
    PrivacyLevel Privacy,
    DateTime CreatedAt,
    IReadOnlyList<FlashcardDto> Cards);

public record FlashcardDeckListItemDto(
    Guid Id,
    Guid DocumentId,
    string Title,
    int CardCount,
    PrivacyLevel Privacy,
    DateTime CreatedAt);

// ---- FG-6: GenAI Test (Quizzes) ----
public record CreateQuizRequest(Guid? SectionId = null, int QuestionCount = 5, string? Title = null);

public record QuizQuestionDto(
    Guid Id,
    string QuestionText,
    IReadOnlyList<string> Options,
    int OrderIndex);

public record QuizDetailDto(
    Guid Id,
    Guid DocumentId,
    Guid? SectionId,
    string Title,
    QuizStatus Status,
    PrivacyLevel Privacy,
    DateTime CreatedAt,
    IReadOnlyList<QuizQuestionDto> Questions);

public record QuizAnswerSubmissionDto(Guid QuestionId, int SelectedOptionIndex);

public record SubmitQuizAttemptRequest(List<QuizAnswerSubmissionDto> Answers);

public record QuestionResultDto(
    Guid QuestionId,
    bool IsCorrect,
    int CorrectOptionIndex,
    string? Explanation);

public record QuizAttemptResultDto(
    Guid AttemptId,
    int CorrectCount,
    int TotalCount,
    int Score,
    IReadOnlyList<QuestionResultDto> Results);

public record QuizAttemptHistoryDto(
    Guid AttemptId,
    Guid QuizId,
    string QuizTitle,
    int CorrectCount,
    int TotalQuestions,
    int Score,
    DateTime CompletedAt);

public record QuizQuestionGeneratedDto(
    string QuestionText,
    List<string> Options,
    int CorrectOptionIndex,
    string? Explanation);

