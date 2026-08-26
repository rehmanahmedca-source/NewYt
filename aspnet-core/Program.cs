/**
 * YT Downloader X Pro -- ASP.NET Core entry point.
 *
 * Run with:
 *     dotnet run
 *
 * Then open http://127.0.0.1:5000 in a browser.
 */
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YTDownloaderXPro.Services;
using YTDownloaderXPro.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.IsEssential = true;
});
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<DownloaderService>();
builder.Services.AddSingleton<QueueManagerService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<HistoryService>();

var app = builder.Build();

// Initialize
var db = app.Services.GetRequiredService<DatabaseService>();
db.Initialize();

var settings = app.Services.GetRequiredService<SettingsService>();
settings.EnsureDefaults();

var queue = app.Services.GetRequiredService<QueueManagerService>();
queue.RecoverIncompleteTasks();
queue.Start();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "api",
    pattern: "api/{action}/{id?}",
    defaults: new { controller = "Api" });

app.MapControllerRoute(
    name: "default",
    pattern: "{action=Index}/{id?}",
    defaults: new { controller = "Home" });

app.Run();
