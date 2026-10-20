using JoFit.Domain.AcademicDegrees;
using JoFit.Domain.Attachments;
using JoFit.Domain.Cities;
using JoFit.Domain.Countries;
using JoFit.Domain.Articles;
using JoFit.Domain.Brands;
using JoFit.Domain.Careers;
using JoFit.Domain.Community;
using JoFit.Domain.Support;
using JoFit.Domain.Menus;
using JoFit.Domain.Enrolments;
using JoFit.Domain.Quotations;
using JoFit.Domain.Reviews;
using JoFit.Domain.Notifications;
using JoFit.Domain.Orders;
using JoFit.Domain.Coaches;
using JoFit.Domain.Locations;
using JoFit.Domain.Rental;
using JoFit.Domain.Products;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Sessions;
using JoFit.Domain.Media;
using JoFit.Domain.Authors;
using JoFit.Domain.JobCategories;
using JoFit.Domain.ArticleCategories;
using JoFit.Domain.CourseCategories;
using JoFit.Domain.CoachSpecialisations;
using JoFit.Domain.RentalDepots;
using JoFit.Domain.RentalCategories;
using JoFit.Domain.Sports;
using JoFit.Domain.ProductCategories;
using JoFit.Domain.Courses;
using JoFit.Domain.Common;
using JoFit.Domain.Identity;
using JoFit.Domain.JobStatuses;
using JoFit.Domain.JobTypes;
using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.RefreshTokens;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.UniversityMajors;
using JoFit.Domain.UserCertificates;
using JoFit.Domain.UserEducations;
using JoFit.Domain.UserExperiences;
using JoFit.Domain.UserJobTypes;
using JoFit.Domain.UserSkills;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace JoFit.Infrastructure.Data;

public class JoFitDbContext(DbContextOptions<JoFitDbContext> options, IMediator mediator)
    : IdentityDbContext<User, Role, long, IdentityUserClaim<long>, IdentityUserRole<long>, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<UserJobType> UserJobTypes => Set<UserJobType>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<JobStatus> JobStatuses => Set<JobStatus>();
    public DbSet<AcademicDegree> AcademicDegrees => Set<AcademicDegree>();
    public DbSet<UniversityMajor> UniversityMajors => Set<UniversityMajor>();
    public DbSet<JobType> JobTypes => Set<JobType>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobSportLink> JobSportLinks => Set<JobSportLink>();
    public DbSet<CvFile> CvFiles => Set<CvFile>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<CommunityTopic> CommunityTopics => Set<CommunityTopic>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityComment> CommunityComments => Set<CommunityComment>();
    public DbSet<SupportConversation> SupportConversations => Set<SupportConversation>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<JoFit.Domain.Settings.ThemeSetting> ThemeSettings => Set<JoFit.Domain.Settings.ThemeSetting>();
    public DbSet<MenuPage> MenuPages => Set<MenuPage>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleCategoryLink> ArticleCategoryLinks => Set<ArticleCategoryLink>();
    public DbSet<ArticleSportLink> ArticleSportLinks => Set<ArticleSportLink>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<QuotationRequest> QuotationRequests => Set<QuotationRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Coach> Coaches => Set<Coach>();
    public DbSet<CoachPackage> CoachPackages => Set<CoachPackage>();
    public DbSet<CoachCertification> CoachCertifications => Set<CoachCertification>();
    public DbSet<CoachSportLink> CoachSportLinks => Set<CoachSportLink>();
    public DbSet<CoachSpecialisationLink> CoachSpecialisationLinks => Set<CoachSpecialisationLink>();
    public DbSet<CoachBooking> CoachBookings => Set<CoachBooking>();
    public DbSet<RentalItem> RentalItems => Set<RentalItem>();
    public DbSet<RentalRate> RentalRates => Set<RentalRate>();
    public DbSet<RentalItemCategoryLink> RentalItemCategoryLinks => Set<RentalItemCategoryLink>();
    public DbSet<RentalBooking> RentalBookings => Set<RentalBooking>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<ProductCategoryLink> ProductCategoryLinks => Set<ProductCategoryLink>();
    public DbSet<ProductSportLink> ProductSportLinks => Set<ProductSportLink>();
    public DbSet<CourseCategoryLink> CourseCategoryLinks => Set<CourseCategoryLink>();
    public DbSet<CourseInstructorLink> CourseInstructorLinks => Set<CourseInstructorLink>();
    public DbSet<CourseSportLink> CourseSportLinks => Set<CourseSportLink>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<RecordMedia> RecordMedia => Set<RecordMedia>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AccountSession> AccountSessions => Set<AccountSession>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<JobCategory> JobCategories => Set<JobCategory>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<CoachSpecialisation> CoachSpecialisations => Set<CoachSpecialisation>();
    public DbSet<RentalDepot> RentalDepots => Set<RentalDepot>();
    public DbSet<RentalCategory> RentalCategories => Set<RentalCategory>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<UserEducation> UserEducations => Set<UserEducation>();
    public DbSet<UserCertificate> UserCertificates => Set<UserCertificate>();
    public DbSet<UserExperience> UserExperiences => Set<UserExperience>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(JoFitDbContext).Assembly);
        ApplySoftDeleteQueryFilter(builder);
    }

    /// <summary>
    /// Applies a shared "!IsDeleted" global query filter to every entity that inherits
    /// AuditableEntity, so soft-deleted rows are excluded automatically from every LINQ
    /// query against this context. Use IgnoreQueryFilters() to opt out where deleted
    /// rows are needed (e.g. restore flows, audit views).
    /// </summary>
    private static void ApplySoftDeleteQueryFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    public Task<int> SaveChangesWithoutEventsAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker.Entries()
            .Where(e => e.Entity is Entity baseEntity && baseEntity.DomainEvents.Count != 0)
            .Select(e => (Entity)e.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, cancellationToken);

        foreach (var entity in domainEntities)
            entity.ClearDomainEvents();
    }
}
