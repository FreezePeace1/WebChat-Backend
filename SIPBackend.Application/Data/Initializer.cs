using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SIPBackend.Domain.Entities;
using SIPBackend.Domain.Models;

namespace SIPBackend.Application.Data;

public class Initializer
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public Initializer(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public void Initialize()
    {
        InitializeAsync().Wait();
    }
    
    private async Task InitializeAsync()
    {
        await GenerateIdentity().ConfigureAwait(false);
    }
    
    private async Task GenerateIdentity()
    {
        if (!await _roleManager.RoleExistsAsync(UserRoles.ADMINISTRATOR))
        {
            await _roleManager.CreateAsync(new IdentityRole()
            {
                Name = UserRoles.ADMINISTRATOR
            });
        }

        if (!await _roleManager.RoleExistsAsync(UserRoles.USER))
        {
            await _roleManager.CreateAsync(new IdentityRole()
            {
                Name = UserRoles.USER
            });
        }

        var adminName = _configuration["AdminInfo:Name"];
        var adminPassword = _configuration["AdminInfo:Password"];
        var adminEmail = _configuration["AdminInfo:Email"];

        if (await _userManager.FindByNameAsync(adminName) is null)
        {
            var admin = new AppUser()
            {
                UserName = adminName,
                Email = adminEmail,
                EmailConfirmed = true,
                PhoneNumber = "89100000000",
                RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                RefreshTokenExpires = DateTime.UtcNow.AddDays(30),
                RefreshTokenCreated = DateTime.UtcNow
            };

            var createAdmin = await _userManager.CreateAsync(admin, adminPassword);

            if (createAdmin.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, UserRoles.ADMINISTRATOR);
            }
            else
            {
                var errors = createAdmin.Errors.Select(x => x.Description);

                throw new InvalidOperationException($"Ошибка при создании Админа " +
                                                    $"{string.Join(",", errors)}");
            }
        }
    }
}