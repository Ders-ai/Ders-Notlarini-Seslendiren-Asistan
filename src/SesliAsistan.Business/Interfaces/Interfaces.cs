using SesliAsistan.Business.DTOs.Auth;
using SesliAsistan.Business.DTOs.Documents;
using SesliAsistan.Business.DTOs.Learning;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Speech;
using SesliAsistan.Business.TextExtraction;
using SesliAsistan.Entities.Entities;
using SesliAsistan.Entities.Enums;

namespace SesliAsistan.Business.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

public interface IEmailSender
{
    Task SendPasswordResetCodeAsync(string toEmail, string code, CancellationToken ct = default);
}

public interface IUserService
{
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    /// <summary>KVKK: kullanıcının hesabını silme hakkı.</summary>
    Task DeleteAccountAsync(Guid userId, CancellationToken ct = default);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
    (string PlainToken, string TokenHash) CreateRefreshToken();
    string HashToken(string token);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IDocumentService
{
    Task<DocumentDetailDto> UploadAsync(UploadDocumentCommand command, CancellationToken ct = default);
    Task<PagedResult<DocumentListItemDto>> GetListAsync(Guid ownerId, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<DocumentDetailDto> GetDetailAsync(Guid ownerId, Guid documentId, CancellationToken ct = default);
    Task<SectionContentDto> GetSectionAsync(Guid ownerId, Guid documentId, Guid sectionId, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, Guid documentId, CancellationToken ct = default);
    Task ReprocessAsync(Guid ownerId, Guid documentId, CancellationToken ct = default);
}

/// <summary>Belgeyi arka planda işleyen (metin çıkarma + konu ağacı) bileşen.</summary>
public interface IDocumentProcessor
{
    Task ProcessAsync(Guid documentId, CancellationToken ct = default);
}

/// <summary>Yükleme isteğini bekletmeden işleme kuyruğu (NFR: asenkron işleme).</summary>
public interface IDocumentProcessingQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken ct = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

public interface IFileValidator
{
    /// <summary>Uzantı, boyut ve dosya imzasını (magic bytes) doğrular; geçerliyse dosya tipini döner.</summary>
    DocumentFileType Validate(string fileName, long length, Stream content);
}

public interface ITextExtractor
{
    DocumentFileType SupportedType { get; }
    Task<ExtractionResult> ExtractAsync(Stream content, CancellationToken ct = default);
}

public interface ITtsProvider
{
    Task<byte[]> SynthesizeAsync(string ssml, string voice, decimal speed, CancellationToken ct = default);
}

public interface ITtsService
{
    Task<AudioResultDto> SynthesizeSectionAudioAsync(Guid userId, Guid documentId, Guid sectionId, string? voice, decimal? speed, CancellationToken ct = default);
    Task<AudioResultDto> SynthesizeSummaryAudioAsync(Guid userId, Guid summaryId, string? voice, decimal? speed, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType, long FileLength)> GetAudioStreamAsync(Guid audioId, CancellationToken ct = default);
    IReadOnlyList<VoiceDto> GetAvailableVoices();
}

public interface IListeningProgressService
{
    Task SaveProgressAsync(Guid userId, Guid documentId, SaveProgressRequest request, CancellationToken ct = default);
    Task<ListeningProgressDto?> GetProgressAsync(Guid userId, Guid documentId, CancellationToken ct = default);
}

public interface ITextRankSummarizer
{
    string Summarize(string text, SummaryLength length);
}

public interface ISummaryService
{
    Task<SummaryDto> CreateSummaryAsync(Guid userId, Guid documentId, Guid sectionId, SummaryLength length, CancellationToken ct = default);
    Task<SummaryDto> GetSummaryAsync(Guid userId, Guid summaryId, CancellationToken ct = default);
}

// ---- FG-6: Bilgi Kartı ve Test Servisleri ----

public interface IFlashcardService
{
    Task<FlashcardDeckDto> CreateFromSectionAsync(Guid userId, Guid documentId, Guid sectionId, CreateFlashcardsRequest? request, CancellationToken ct = default);
    Task<IReadOnlyList<FlashcardDeckListItemDto>> GetUserDecksAsync(Guid userId, CancellationToken ct = default);
    Task<FlashcardDeckDto> GetDeckAsync(Guid userId, Guid deckId, CancellationToken ct = default);
    Task<FlashcardDto> UpdateCardAsync(Guid userId, Guid cardId, UpdateFlashcardRequest request, CancellationToken ct = default);
    Task DeleteDeckAsync(Guid userId, Guid deckId, CancellationToken ct = default);
    Task<AudioResultDto> SynthesizeCardAudioAsync(Guid userId, Guid cardId, string side, string? voice, decimal? speed, CancellationToken ct = default);
}

public interface IQuizService
{
    Task<QuizDetailDto> CreateQuizAsync(Guid userId, Guid documentId, CreateQuizRequest request, CancellationToken ct = default);
    Task<QuizDetailDto> GetQuizAsync(Guid userId, Guid quizId, CancellationToken ct = default);
    Task<QuizAttemptResultDto> SubmitAttemptAsync(Guid userId, Guid quizId, SubmitQuizAttemptRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuizAttemptHistoryDto>> GetAttemptsHistoryAsync(Guid userId, Guid quizId, CancellationToken ct = default);
}

// ---- FG-7: İşbirlikçi Ağ Servisleri ----

public interface IFriendshipService
{
    Task<IReadOnlyList<UserSearchDto>> SearchUsersAsync(Guid currentUserId, string query, CancellationToken ct = default);
    Task<FriendDto> SendRequestAsync(Guid currentUserId, Guid addresseeId, CancellationToken ct = default);
    Task<FriendDto> RespondRequestAsync(Guid currentUserId, Guid friendshipId, bool accept, CancellationToken ct = default);
    Task<FriendListDto> GetFriendsAsync(Guid currentUserId, CancellationToken ct = default);
    Task RemoveOrBlockFriendAsync(Guid currentUserId, Guid targetUserId, bool block = false, CancellationToken ct = default);
}

public interface IShareAccessHandler
{
    Task<bool> CanAccessAsync(Guid userId, SharedContent share, CancellationToken ct = default);
}

public interface IShareService
{
    Task<FeedItemDto> CreateShareAsync(Guid userId, ShareContentRequest request, CancellationToken ct = default);
    Task<FeedItemDto> UpdateShareAsync(Guid userId, Guid shareId, UpdateShareRequest request, CancellationToken ct = default);
    Task<CopyShareResultDto> CopyShareAsync(Guid userId, Guid shareId, CancellationToken ct = default);
}

public interface IFeedService
{
    Task<CursorPagedResult<FeedItemDto>> GetFeedAsync(Guid userId, string? cursor, int pageSize = 20, CancellationToken ct = default);
}

public interface IStudyGroupService
{
    Task<StudyGroupDetailDto> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken ct = default);
    Task<StudyGroupDetailDto> JoinGroupAsync(Guid userId, JoinGroupRequest request, CancellationToken ct = default);
    Task<StudyGroupDetailDto> GetGroupAsync(Guid userId, Guid groupId, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid memberId, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid userId, CancellationToken ct = default);
    Task SendNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default);
}

// ---- FG-8: Yönetim Paneli ve Moderasyon ----

public interface IModerationService
{
    Task<ContentReportDto> ReportContentAsync(Guid userId, CreateReportRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ContentReportDto>> GetReportsAsync(Guid adminId, CancellationToken ct = default);
    Task ReviewReportAsync(Guid adminId, Guid reportId, ReviewReportRequest request, CancellationToken ct = default);
    Task UpdateUserStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken ct = default);
    Task<AdminStatsDto> GetStatsAsync(Guid adminId, CancellationToken ct = default);
}

