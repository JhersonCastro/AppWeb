using AppWeb.Components;
using AppWeb.Services;
using DbContext;
using Microsoft.EntityFrameworkCore;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<DatabaseContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("FreeConnection"))
);
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<CookiesService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PostService>();

builder.Services.AddScoped<UserState>();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();