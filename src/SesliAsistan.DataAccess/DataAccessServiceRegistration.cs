using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SesliAsistan.DataAccess.Context;
using SesliAsistan.DataAccess.Repositories;

namespace SesliAsistan.DataAccess;

public static class DataAccessServiceRegistration
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();
        services.AddScoped<IAudioAssetRepository, AudioAssetRepository>();
        services.AddScoped<IListeningProgressRepository, ListeningProgressRepository>();
        services.AddScoped<ISummaryRepository, SummaryRepository>();
        services.AddScoped<IFlashcardDeckRepository, FlashcardDeckRepository>();
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IStudyGroupRepository, StudyGroupRepository>();
        services.AddScoped<ISharedContentRepository, SharedContentRepository>();
        services.AddScoped<IContentReportRepository, ContentReportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAdminActionRepository, AdminActionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
