using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;
using WebAPI.Common;
using WebAPI.DataLayer.Context;
using WebAPI.Models.Identity;
using WebAPI.Services;

namespace WebAPI.Extentions;

public static class ConfigureServicesExtensions
{
    public static void AddCustomAntiforgery(this IServiceCollection services)
    {
        services.AddAntiforgery(x => x.HeaderName = "X-XSRF-TOKEN");
        services.AddMvc(options => { options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); });
    }

    public static void AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
                         {
                             options.AddPolicy("CorsPolicy",
                                               builder => builder
                                                          .WithOrigins(
                                                                       "http://localhost:4200") //Note:  The URL must be specified without a trailing slash (/).
                                                          .AllowAnyMethod()
                                                          .AllowAnyHeader()
                                                          .SetIsOriginAllowed(host => true)
                                                          .AllowCredentials());
                         });
    }

    public static void AddCustomJwtBearer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization(options =>
                                  {
                                      options.AddPolicy(CustomRoles.Admin,
                                                        policy => policy.RequireRole(CustomRoles.Admin));
                                      options.AddPolicy(CustomRoles.Delivery,
                                                        policy => policy.RequireRole(CustomRoles.Delivery));
                                      options.AddPolicy(CustomRoles.B2B,
                                                        policy => policy.RequireRole(CustomRoles.B2B));
                                  });

        services
            .AddAuthentication(options =>
                               {
                                   options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                                   options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                                   options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                               })
            .AddJwtBearer(cfg =>
                          {
                              cfg.RequireHttpsMetadata = false;
                              cfg.SaveToken = true;
                              var bearerTokenOption =
                                  configuration.GetSection("BearerTokens").Get<BearerTokensOptions>();
                              if (bearerTokenOption is null)
                              {
                                  throw new InvalidOperationException("bearerTokenOption is null");
                              }

                              cfg.TokenValidationParameters = new TokenValidationParameters
                              {
                                  // site that makes the token
                                  ValidIssuer = bearerTokenOption.Issuer,
                                  ValidateIssuer = true,
                                  // site that consumes the token
                                  ValidAudience = bearerTokenOption.Audience,
                                  ValidateAudience = true,
                                  IssuerSigningKey =
                                                                      new SymmetricSecurityKey(
                                                                       Encoding.UTF8
                                                                               .GetBytes(bearerTokenOption.Key)),
                                  // verify signature to avoid tampering
                                  ValidateIssuerSigningKey = true,
                                  ValidateLifetime = true, // validate the expiration
                                                           // tolerance for the expiration date
                                  ClockSkew = TimeSpan.Zero,
                              };
                              cfg.Events = new JwtBearerEvents
                              {
                                  OnAuthenticationFailed = context =>
                                                           {
                                                               var logger = context.HttpContext
                                                                   .RequestServices
                                                                   .GetRequiredService<ILoggerFactory>()
                                                                   .CreateLogger(nameof(JwtBearerEvents));
                                                               logger
                                                                   .LogError($"Authentication failed {context.Exception}");
                                                               return Task.CompletedTask;
                                                           },
                                  OnTokenValidated = context =>
                                                     {
                                                         var tokenValidatorService =
                                                             context.HttpContext.RequestServices
                                                                 .GetRequiredService<
                                                                     ITokenValidatorService>();
                                                         return tokenValidatorService
                                                             .ValidateAsync(context);
                                                     },
                                  OnMessageReceived = _ => Task.CompletedTask,
                                  OnChallenge = context =>
                                                {
                                                    var logger = context.HttpContext.RequestServices
                                                        .GetRequiredService<ILoggerFactory>()
                                                        .CreateLogger(nameof(JwtBearerEvents));
                                                    logger
                                                        .LogError($"OnChallenge error {context.Error}, {context.ErrorDescription}");
                                                    return Task.CompletedTask;
                                                },
                              };
                          });
    }

    public static void AddCustomDbContext(this IServiceCollection services, IConfiguration configuration,
                                          Assembly startupAssembly)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var projectDir = ServerPath.GetProjectPath(startupAssembly);
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("defaultConnection is null");
        }

        var connectionString = defaultConnection.Replace("|DataDirectory|",
                                                         Path.Combine(projectDir, "wwwroot", "app_data"),
                                                         StringComparison.OrdinalIgnoreCase);
        services.AddDbContext<ApplicationDbContext>(options =>
                                                    {
                                                        options.UseSqlite(connectionString,
                                                                             serverDbContextOptionsBuilder =>
                                                                             {
                                                                                 var minutes =
                                                                                     (int)TimeSpan.FromMinutes(3)
                                                                                         .TotalSeconds;
                                                                                 serverDbContextOptionsBuilder
                                                                                     .CommandTimeout(minutes);
                                                                             });
                                                    });
    }

    public static void AddCustomServices(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<IDeviceDetectionService, DeviceDetectionService>();
        services.AddScoped<IAntiForgeryCookieService, AntiForgeryCookieService>();
        services.AddScoped<IUnitOfWork, ApplicationDbContext>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IRolesService, RolesService>();
        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddScoped<IDbInitializerService, DbInitializerService>();
        services.AddScoped<ITokenStoreService, TokenStoreService>();
        services.AddScoped<ITokenValidatorService, TokenValidatorService>();
        services.AddScoped<ITokenFactoryService, TokenFactoryService>();
    }

    public static void AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.AddOptions<BearerTokensOptions>()
                .Bind(configuration.GetSection("BearerTokens"))
                .Validate(
                          bearerTokens =>
                          {
                              return bearerTokens.AccessTokenExpirationMinutes <
                                     bearerTokens.RefreshTokenExpirationMinutes;
                          },
                          "RefreshTokenExpirationMinutes is less than AccessTokenExpirationMinutes. Obtaining new tokens using the refresh token should happen only if the access token has expired.");
        services.AddOptions<ApiSettings>().Bind(configuration.GetSection("ApiSettings"));
        services.AddOptions<UserSeed>().Bind(configuration.GetSection("UserSeed"));
    }

    public static void UseCustomSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(setupAction =>
                         {
                             setupAction.SwaggerEndpoint(
                                                         "/swagger/LibraryOpenAPISpecification/swagger.json",
                                                         "Library API");
                             //setupAction.RoutePrefix = ""; --> To be able to access it from this URL: https://localhost:5001/swagger/index.html

                             setupAction.DefaultModelExpandDepth(2);
                             setupAction.DefaultModelRendering(ModelRendering.Model);
                             setupAction.DocExpansion(DocExpansion.None);
                             setupAction.EnableDeepLinking();
                             setupAction.DisplayOperationId();
                         });
    }

    public static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(setupAction =>
                               {
                                   setupAction.SwaggerDoc(
                                                          "LibraryOpenAPISpecification",
                                                          new OpenApiInfo
                                                          {
                                                              Title = "Library API",
                                                              Version = "1",
                                                              Description =
                                                                  "Through this API you can access the site's capabilities.",
                                                              Contact = new OpenApiContact
                                                              {
                                                                  Email = "toomaris@live.com",
                                                                  Name = "Sajjad",
                                                              }
                                                          });

                                   var xmlFiles = Directory
                                                  .GetFiles(AppContext.BaseDirectory, "*.xml",
                                                            SearchOption.TopDirectoryOnly)
                                                  .ToList();
                                   xmlFiles.ForEach(xmlFile => setupAction.IncludeXmlComments(xmlFile));

                                   setupAction.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                                   {
                                       Name = "Authorization",
                                       Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
                                       In = ParameterLocation.Header,
                                       Type = SecuritySchemeType.ApiKey,
                                       Scheme = "Bearer"
                                   });

                                   setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
                                                                      {
                                                                          {
                                                                              new OpenApiSecurityScheme
                                                                              {
                                                                                  Reference = new OpenApiReference
                                                                                      {
                                                                                          Type = ReferenceType
                                                                                              .SecurityScheme,
                                                                                          Id = "Bearer",
                                                                                      },
                                                                              },
                                                                              new List<string>()
                                                                          },
                                                                      });
                               });
    }
}