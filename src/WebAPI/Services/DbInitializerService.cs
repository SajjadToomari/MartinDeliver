using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI.DataLayer.Context;
using WebAPI.DomainClasses;
using WebAPI.Models.Identity;

namespace WebAPI.Services;

public class DbInitializerService : IDbInitializerService
{
    private readonly IOptions<UserSeed> _userSeedOption;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISecurityService _securityService;

    public DbInitializerService(
        IServiceScopeFactory scopeFactory,
        ISecurityService securityService,
        IOptions<UserSeed> adminUserSeedOption)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
        _userSeedOption = adminUserSeedOption ?? throw new ArgumentNullException(nameof(adminUserSeedOption));
    }

    public void Initialize()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        using var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        context.Database.Migrate();
    }

    public void SeedData()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        using var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Add default roles
        var adminRole = new Role { Name = CustomRoles.Admin };
        var deliveryRole = new Role { Name = CustomRoles.Delivery };
        var b2bRole = new Role { Name = CustomRoles.B2B };
        if (!context.Roles.Any())
        {
            context.Add(adminRole);
            context.Add(deliveryRole);
            context.Add(b2bRole);
            context.SaveChanges();
        }

        // Add user
        if (!context.Users.Any())
        {
            var deliveryUser = new User
            {
                Username = _userSeedOption.Value.UsernameDelivery,
                DisplayName = _userSeedOption.Value.UsernameDelivery,
                IsActive = true,
                LastLoggedIn = null,
                Password = _securityService.GetSha256Hash(_userSeedOption.Value.PasswordDelivery),
                SerialNumber = Guid.NewGuid().ToString("N"),
            };
            context.Add(deliveryUser);
            context.SaveChanges();

            context.Add(new UserRole { Role = deliveryRole, User = deliveryUser });
            context.SaveChanges();

            var b2bUser = new User
            {
                Username = _userSeedOption.Value.UsernameB2B,
                DisplayName = _userSeedOption.Value.UsernameB2B,
                IsActive = true,
                LastLoggedIn = null,
                Password = _securityService.GetSha256Hash(_userSeedOption.Value.PasswordB2B),
                SerialNumber = Guid.NewGuid().ToString("N"),
            };
            context.Add(b2bUser);
            context.SaveChanges();

            context.Add(new UserRole { Role = b2bRole, User = b2bUser });
            context.SaveChanges();
        }
    }
}