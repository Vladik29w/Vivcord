using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using Vivcord.Server.DbContext;
using Vivcord.Server.Extensions;
using Vivcord.Server.Hubs;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Infastructure.SignalR;
using Vivcord.Server.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IFriendService, FriendService>();

var signalRBuilder = builder.Services.AddSignalR();
var azureSignalRConnectionString = builder.Configuration.GetConnectionString("AzureSignalR");
if (!string.IsNullOrWhiteSpace(azureSignalRConnectionString))
{
    signalRBuilder.AddAzureSignalR(options =>
    {
        options.ConnectionString = azureSignalRConnectionString;
    });
}

builder.Services.AddSingleton<IUserIdProvider, LowercaseUserIdProvider>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
// CORS
var corsOrigins = builder.Configuration["CorsOrigins"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularOrigin",
        policy =>
        {
            var origins = new List<string> { "https://localhost:62667", "https://127.0.0.1:62667", "http://localhost:4200" };
            if (!string.IsNullOrEmpty(corsOrigins))
            {
                origins.AddRange(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()));
            }
            policy.WithOrigins(origins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
//Database
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//Identity and roles
builder.Services.AddVivcordIdentity();
//JWT
var jwtSetting = builder.Configuration.GetSection("JwtSetting");
var key = Encoding.UTF8.GetBytes(jwtSetting["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSetting["VivcordServer"],
        ValidAudience = jwtSetting["VivcordClient"],
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapScalarApiReference(options =>
{
    options
        .AddPreferredSecuritySchemes("https")
        .WithTitle("VivcordServer")
        .WithTheme(ScalarTheme.DeepSpace)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAngularOrigin");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<PrivateHub>("/hubs/private");

app.MapFallbackToFile("/index.html");

app.Run();
