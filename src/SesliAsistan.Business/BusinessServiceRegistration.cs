using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.Business.Options;
using SesliAsistan.Business.Services;
using SesliAsistan.Business.TextExtraction;

namespace SesliAsistan.Business;

public static class BusinessServiceRegistration
{
    public static IServiceCollection AddBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<TtsOptions>(configuration.GetSection(TtsOptions.SectionName));

        services.AddValidatorsFromAssembly(typeof(BusinessServiceRegistration).Assembly);

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IFileValidator, FileValidator>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.AddSingleton<IEmailSender, LoggerEmailSender>();
        services.AddSingleton<ITextRankSummarizer, SesliAsistan.Business.Speech.TextRankSummarizer>();
        services.AddSingleton<SesliAsistan.Business.Speech.IKeyTermExtractor, SesliAsistan.Business.Speech.KeyTermExtractor>();
        services.AddSingleton<SesliAsistan.Business.Speech.IDefinitionMatcher, SesliAsistan.Business.Speech.DefinitionMatcher>();
        services.AddSingleton<SesliAsistan.Business.Speech.IQuizValidator, SesliAsistan.Business.Speech.QuizValidator>();

        services.AddHttpClient<ITtsProvider, SesliAsistan.Business.Speech.AzureSpeechTtsProvider>();
        services.AddHttpClient<SesliAsistan.Business.Speech.IQuizGenerator, SesliAsistan.Business.Speech.GroqQuizGenerator>();

        services.AddSingleton<ITextExtractor, PdfTextExtractor>();
        services.AddSingleton<ITextExtractor, DocxTextExtractor>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
        services.AddScoped<ITtsService, TtsService>();
        services.AddScoped<IListeningProgressService, ListeningProgressService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<IFlashcardService, FlashcardService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IFriendshipService, FriendshipService>();
        services.AddScoped<IShareAccessHandler, ShareAccessHandler>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IStudyGroupService, StudyGroupService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IModerationService, ModerationService>();
        return services;
    }
}
