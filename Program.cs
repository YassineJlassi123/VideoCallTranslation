using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoCallTranslation.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
DotNetEnv.Env.Load();
var CohereSecret = Environment.GetEnvironmentVariable("CohereSecret");
builder.Services.AddSingleton(new CohereService(CohereSecret));
builder.Services.AddRazorPages();  // Add Razor Pages support
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") ?? throw new InvalidOperationException("Connection string not configured.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=createroom}/{id?}");
app.MapHub<VideoCallHub>("/VideoCallHub");

app.Run();