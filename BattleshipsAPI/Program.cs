using BattleshipsAPI.Attributes;
using BattleshipsAPI.Auth;
using BattleshipsAPI.Hubs;
using BattleshipsAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace BattleshipsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<ActiveGames>();
            builder.Services.AddDbContext<BattleshipsDBContext>(
                options => options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));
            builder.Services.AddControllers().AddJsonOptions(o =>
                o.JsonSerializerOptions.PropertyNamingPolicy = null
            );
            builder.Services.AddSwaggerGen(o =>
            {
                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "Token",
                    In = ParameterLocation.Header,
                    Description = "Токенчик"
                });
                o.AddSecurityRequirement(document => new()
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<BattleshipsDBContext>().Database.Migrate();
            }
            app.Use(async (context, next) =>
            {
                if (context.GetEndpoint()?.Metadata.GetMetadata<TokenRequired>() == null)
                {
                    await next(context);
                    return;
                }
                string token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                string? playerLogin = TokenStorage.GetUserLogin(token);
                if (playerLogin is null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Плохой токен");
                    return;
                }
                context.Items["PlayerLogin"] = playerLogin;
                await next(context);
            });

            //

            app.MapHub<NotificationHub>("/notifications");
            app.MapControllers();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.Run();
        }
    }
}
