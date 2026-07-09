using PortfolioApp.Components;
using PortfolioApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<UnitStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// The BlazorThreeJS Viewer component injects its own <script src="_content/BlazorThreeJS/dist/app-lib.js">
// client-side regardless of what's referenced in App.razor. That bundle unconditionally renders a
// room/grid around the scene with no way to disable it via the public API, so intercept the request
// and serve our locally patched copy (wwwroot/lib/blazor-three-js/app-lib.js, addRoom() neutered) instead.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/_content/BlazorThreeJS/dist/app-lib.js")
    {
        context.Response.ContentType = "text/javascript";
        await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "lib", "blazor-three-js", "app-lib.js"));
        return;
    }

    await next();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
