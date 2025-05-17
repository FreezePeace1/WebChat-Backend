using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SIPBackend.DAL.Context;
using SIPBackend.Domain.Entities;

namespace SIPBackend;

public static class Startup
{
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        //Подключаем Identity
        services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<SIPBackendContext>()
            .AddDefaultTokenProviders();

        //Настраиваем Identity
        services.Configure<IdentityOptions>(opt =>
        {
            opt.Password.RequireDigit = true;
            opt.Password.RequiredLength = 8;
            opt.Password.RequireLowercase = true;
            opt.Password.RequireUppercase = false;
            opt.Password.RequireNonAlphanumeric = false;

            //Не реализовано
            opt.Lockout.AllowedForNewUsers = true;
            opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            opt.Lockout.MaxFailedAccessAttempts = 5;

            //Нужен нормальный email (существующий)
            opt.User.RequireUniqueEmail = true;
        });

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo()
            {
                Version = "v1",
                Title = "Diary.Api",
                Description = "Version 1.0",
                //Adding uri
                /*TermsOfService = new Uri()*/
            });

            opt.SwaggerDoc("v2", new OpenApiInfo()
            {
                Version = "v2",
                Title = "Diary.Api",
                Description = "Version 2.0",
                //Adding uri
                /*TermsOfService = new Uri()*/
            });

            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                In = ParameterLocation.Header,
                Description = "Enter valid token",
                Name = "Authorize",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            opt.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme()
                    {
                        Reference = new OpenApiReference()
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
        });

        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, WebApplicationBuilder builder)
    {
        //Подключаем JWT
        services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                /*opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;*/
            })
            .AddCookie(opt =>
            {
                opt.LoginPath = "/Auth/Login";
                opt.LogoutPath = "/Auth/Logout";
            })
            .AddJwtBearer("Bearer", opt =>
            {
                opt.SaveToken = true;
                opt.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"])),

                    /*RoleClaimType = UserRoles.ADMINISTRATOR*/
                };
                
                // Чтение токена из куки
                opt.Events = new JwtBearerEvents {
                    OnMessageReceived = context => {
                        context.Token = context.Request.Cookies["accessToken"];
                        return Task.CompletedTask;
                    }
                };
            });
        
        services.AddAuthorization(options =>
        {
            var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme,
                CookieAuthenticationDefaults.AuthenticationScheme,
                "Bearer");

            defaultAuthorizationPolicyBuilder =
                defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
            options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();

            options.AddPolicy("Default", new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        });

        return services;
    }
}