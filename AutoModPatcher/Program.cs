using CustomizationPatch.Components;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var port = app.Services.GetRequiredService<IConfiguration>().GetValue<string>("Kestrel:EndPoints:Http:Url")?.Split(":").Last();
        Console.WriteLine("The Customiaztion Patch application is running");
        Console.WriteLine($"If the web browser did not open, please visit: http://localhost:{port}/");
        Process.Start(new ProcessStartInfo($"http://localhost:{port}") { UseShellExecute = true });
    });

}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


