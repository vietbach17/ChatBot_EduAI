using BussinessLayer.Services;
using DataAccessLayer;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using DataAccessLayer.IRepositories;

Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// DbContext setup
var connectionString = System.Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
