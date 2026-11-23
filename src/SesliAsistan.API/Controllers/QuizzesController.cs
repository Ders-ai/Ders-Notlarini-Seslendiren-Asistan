using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-6: GenAI tabanlı çoktan seçmeli test üretme, çözme ve geçmiş sorgulama.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizzes;

    public QuizzesController(IQuizService quizzes) => _quizzes = quizzes;

    /// <summary>Test üretme isteği (202 Accepted).</summary>
    [HttpPost("api/v1/documents/{id:guid}/quizzes")]
    [ProducesResponseType<QuizDetailDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateQuiz(
        Guid id,
        [FromBody] CreateQuizRequest request,
        CancellationToken ct)
    {
        var quiz = await _quizzes.CreateQuizAsync(User.GetUserId(), id, request, ct);
        return AcceptedAtAction(nameof(GetQuiz), new { quizId = quiz.Id }, quiz);
    }

    /// <summary>Test durumu ve soruları (doğru cevap hariç).</summary>
    [HttpGet("api/v1/quizzes/{quizId:guid}")]
    [ProducesResponseType<QuizDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizDetailDto>> GetQuiz(Guid quizId, CancellationToken ct) =>
        Ok(await _quizzes.GetQuizAsync(User.GetUserId(), quizId, ct));

    /// <summary>Cevapları gönderme, puan ve açıklamalar.</summary>
    [HttpPost("api/v1/quizzes/{quizId:guid}/attempts")]
    [ProducesResponseType<QuizAttemptResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizAttemptResultDto>> Submit(
        Guid quizId,
        [FromBody] SubmitQuizAttemptRequest request,
        CancellationToken ct) =>
        Ok(await _quizzes.SubmitAttemptAsync(User.GetUserId(), quizId, request, ct));

    /// <summary>Başarı geçmişi.</summary>
    [HttpGet("api/v1/quizzes/{quizId:guid}/attempts")]
    [ProducesResponseType<IReadOnlyList<QuizAttemptHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuizAttemptHistoryDto>>> GetHistory(Guid quizId, CancellationToken ct) =>
        Ok(await _quizzes.GetAttemptsHistoryAsync(User.GetUserId(), quizId, ct));
}
