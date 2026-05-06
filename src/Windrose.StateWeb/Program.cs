using MudBlazor.Services;
using Windrose.StateWeb.Api;
using Windrose.StateWeb.Components;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.Parsing;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.State;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WindroseStateOptions>(builder.Configuration.GetSection("WindroseState"));
builder.Services.AddMudServices();
builder.Services.AddSingleton<IWindroseLogParser, WindroseLogParser>();
builder.Services.AddSingleton<IWindroseStateStore, WindroseStateStore>();
builder.Services.AddHostedService<WindroseLogTailer>();
builder.Services.AddHostedService<SaveMetadataReader>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapWindroseStateEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
