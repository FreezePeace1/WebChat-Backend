using Serilog;
using SIPBackend;
using SIPBackend.Application.Data;
using SIPBackend.Application.DependencyInjection;
using SIPBackend.Application.Hubs;
using SIPBackend.DAL.DI;
using WebStoreMVC.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwagger();
builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentity();

builder.Services.AddServices();

builder.Services.AddJwt(builder);

builder.Services.AddSignalR();
builder.Services.AddStackExchangeRedisCache(opts =>
{
    var connection = builder.Configuration.GetConnectionString("Redis");
    opts.Configuration = connection;
});

builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RefreshTokenMiddleware>();

app.UseCors(opts => opts
    .WithOrigins("http://localhost:5173","https://localhost:5173"
        ,"https://d702bfaf84f2973ffe8992b17ead0db2.serveo.net")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chat");
app.MapHub<CallHub>("/callHub");

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<Initializer>();
    initializer.Initialize();
}

app.Run();

