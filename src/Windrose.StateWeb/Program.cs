using MudBlazor.Services;
using Seq.Extensions.Logging;
using Windrose.StateWeb.Api;
using Windrose.StateWeb.Components;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.Parsing;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.State;

var builder = WebApplication.CreateBuilder(args);
var seqOptions = builder.Configuration.GetSection("Seq");

builder.Services.Configure<WindroseStateOptions>(builder.Configuration.GetSection("WindroseState"));
builder.Services.AddMudServices();
builder.Services.AddSingleton<IWindroseLogParser, WindroseLogParser>();
builder.Services.AddSingleton<IWindroseStateStore, WindroseStateStore>();
builder.Services.AddHostedService<WindroseLogTailer>();
builder.Services.AddHostedService<SaveMetadataReader>();
builder.Services.AddSingleton<IWindroseHubConnectionFactory, DefaultWindroseHubConnectionFactory>();
builder.Services.AddSingleton<IWindroseLivePushPublisher, SignalRWindroseLivePushPublisher>();
builder.Services.AddHostedService<WindroseLivePushService>();

builder.Logging.AddConsole();
if (!string.IsNullOrWhiteSpace(seqOptions["ServerUrl"]) &&
    !string.IsNullOrWhiteSpace(seqOptions["ApiKey"]))
{
    builder.Logging.AddSeq(seqOptions);
    SelfLog.Enable(Console.Error);
}

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
