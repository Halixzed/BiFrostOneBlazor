using Microsoft.EntityFrameworkCore;
using PortfolioApp.Components;
using PortfolioApp.Data;
using PortfolioApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var dbPath = Path.Combine(dataDirectory, "portfolio.db");
builder.Services.AddDbContextFactory<PortfolioDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<DeviceStore>();
builder.Services.AddSingleton<UnitStore>();
builder.Services.AddSingleton<WatermarkStore>();
builder.Services.AddSingleton<ModelFileStore>();
builder.Services.AddSingleton<EnvironmentMapStore>();
builder.Services.AddSingleton<PdfFileStore>();

var app = builder.Build();

// Migrate the database once at startup, before any singleton store can query it - relying on
// whichever store's constructor happens to run first (as UnitStore used to do) is a race that
// only surfaces on a brand-new/empty database (e.g. a freshly attached Railway volume).
using (var scope = app.Services.CreateScope())
{
    using var context = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PortfolioDbContext>>().CreateDbContext();
    context.Database.Migrate();
}

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

// Serves a device's background watermark image from App_Data/uploads (outside wwwroot - see the
// WatermarkStore constructor comment for why). app.MapStaticAssets() below only serves files
// known from the build-time asset manifest, so a plain wwwroot path wouldn't work for a file
// that's uploaded at runtime and can change or be deleted at any time anyway.
app.MapGet("/watermark-image/{deviceId:int}", (int deviceId, WatermarkStore watermarkStore) =>
{
    var watermark = watermarkStore.Get(deviceId);
    var fullPath = watermark is null ? null : watermarkStore.GetFullPath(watermark);
    if (watermark is null || fullPath is null || !File.Exists(fullPath))
    {
        return Results.NotFound();
    }

    var contentType = watermark.StoredFileName.EndsWith(".svg") ? "image/svg+xml" : "image/png";
    return Results.File(fullPath, contentType);
});

// Serves a device's uploaded HDRI/environment-map image the same way as watermark-image above.
app.MapGet("/environment-map-image/{deviceId:int}", (int deviceId, EnvironmentMapStore environmentMapStore) =>
{
    var environmentMap = environmentMapStore.Get(deviceId);
    var fullPath = environmentMap is null ? null : environmentMapStore.GetFullPath(environmentMap);
    if (environmentMap is null || fullPath is null || !File.Exists(fullPath))
    {
        return Results.NotFound();
    }

    var contentType = environmentMap.StoredFileName.EndsWith(".png") ? "image/png" : "image/jpeg";
    return Results.File(fullPath, contentType);
});

// The BlazorThreeJS Viewer component injects its own <script src="_content/BlazorThreeJS/dist/app-lib.js">
// client-side regardless of what's referenced in App.razor. That bundle unconditionally renders a
// room/grid around the scene with no way to disable it via the public API, so intercept the request
// and serve our locally patched copy (wwwroot/lib/blazor-three-js/app-lib.js) instead. Patches applied,
// none of which are exposed by BlazorThreeJS's public API (there's no OrbitControls settings
// class in it at all - decompiled to confirm):
//   - addRoom() neutered (no room/grid backdrop)
//   - OrbitControls.enablePan forced false (panning locked)
//   - OrbitControls.enableDamping/dampingFactor set so rotate/zoom ease out instead of snapping
//   - Viewer.render() now calls controls.update() every frame, required for damping to animate at all
//   - WebGLRenderer created with alpha:true and an explicit setClearAlpha(0) (alpha:true alone
//     only allows transparency, it doesn't zero out the default opaque clear color), and
//     Scene.background is only set when a BackGroundColor is actually provided (was
//     unconditional) - together these make the canvas render transparent when no background is
//     set, needed so the background watermark image can show through behind it
//   - OrbitControls.autoRotate/autoRotateSpeed set for a slow continuous idle orbit around the
//     model (reuses the same controls.update() per-frame call as damping above)
//   - OrbitControls.minDistance/maxDistance hardcoded to a fixed zoom range (previously read from
//     this.options.orbitControls, which nothing in the public API populates - zoom was
//     effectively unbounded)
//   - Shadow mapping enabled end-to-end (previously entirely off, so every PointLight shone
//     straight through the model into cavities/interiors that should be dark): renderer.shadowMap
//     enabled with PCFSoftShadowMap, every light given castShadow + a shadow.bias of -0.01
//     (prevents shadow-acne self-shadowing artifacts), and every loaded GLTF mesh given
//     castShadow/receiveShadow so the model actually occludes and shadows itself.
//     All 4 rig lights cast shadows (an intensity>=1 gate that limited this to just the key
//     light was tried first for performance, but left the other 3 lights shining straight through
//     the model with no occlusion at all, which was clearly visible as light leaking into
//     interior cavities) - shadow map size dropped to 256x256 (from 512) per light to help
//     offset the 4x increase in shadow-casting lights; see BuildScene() in Home.razor
//   - BuildPointLight constructs a THREE.SpotLight instead of a THREE.PointLight (there's no
//     SpotLight/RectAreaLight class in BlazorThreeJS's public API at all, only PointLight/
//     AmbientLight, even though the underlying three.js bundle has both), aimed at its default
//     (0,0,0) target - which is where models are always loaded, so no extra target-positioning
//     code is needed. Chosen over RectAreaLight specifically because RectAreaLight cannot cast
//     shadows in three.js at all (a hard engine limitation), which would have undone the shadow
//     work above. Bonus: SpotLightShadow uses a single perspective camera, not PointLightShadow's
//     6-face cubemap, so this is also cheaper per shadow-casting light than before.
//   - scene.environment set to a small procedural gradient CanvasTexture by default (not
//     scene.background, which stays unset for the watermark) so PBR materials have something to
//     reflect. This is cosmetic only - it does NOT enable real glass rendering, see the next
//     bullet. A global window.portfolioSetEnvironmentMap(url) function is also added: Home.razor
//     calls it (via JS interop) whenever the admin uploads/removes a custom HDRI-style
//     environment image, loading it with TextureLoader and swapping it in as scene.environment
//     (or reverting to the procedural gradient when url is falsy). There's no RGBELoader/
//     EXRLoader/PMREMGenerator in this bundle at all, so this only supports plain equirectangular
//     JPG/PNG images with no HDR dynamic range and no roughness-based reflection prefiltering -
//     reflections are sharper than a real HDRI pipeline would give, but it's still real
//     image-based lighting, not just a flat gradient.
//   - KHR_materials_transmission ("glass") materials on loaded GLTFs are converted to plain alpha
//     transparency (transparent:true, opacity derived from transmissionFactor, transmission
//     zeroed out) instead of left as real transmission. Confirmed by grepping the whole bundle:
//     there is no transmissionRenderTarget/renderTransmissionPass anywhere in it, meaning this
//     vendored three.js build's WebGLRenderer has had real transmission rendering stripped out
//     entirely - setting material.transmission has no visual effect at all in this build, glass
//     materials silently render as flat opaque PBR surfaces. This alpha-transparency conversion is
//     a simulated look (translucent, no refraction), not a fix for the underlying gap.
//
// This middleware alone is only enough in Development: BlazorThreeJS's Viewer component loads the
// bundle via a Blazor Server JS-interop call, which the client runtime fetches with a Subresource
// Integrity check computed from THIS project's own build-time static web assets manifest - and
// that manifest is generated from the ORIGINAL (unpatched) package file, since the build has no
// idea this middleware exists. In Production that mismatch makes the browser silently block the
// script (breaking the whole viewer with no server-side error). PortfolioApp.csproj's
// PatchBlazorThreeJsIntegrity MSBuild target fixes this by overwriting the published app-lib.js
// and its manifest entry with this same patched content after every `dotnet publish`, so Production
// serves consistent bytes end-to-end - see that target's comment for details.
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

// Serves uploaded unit .glb/.gltf model files from App_Data/uploads/models (same reasoning as
// the watermark-image middleware above: outside wwwroot, uploaded at runtime).
app.MapGet("/model-file/{fileName}", (string fileName, ModelFileStore modelFileStore) =>
{
    var fullPath = modelFileStore.GetFullPath(fileName);
    if (fullPath is null)
    {
        return Results.NotFound();
    }

    var contentType = fileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
        ? "model/gltf-binary"
        : "model/gltf+json";
    return Results.File(fullPath, contentType);
});

// Serves uploaded unit "Learn More" PDFs from App_Data/uploads/pdfs (same reasoning as the
// watermark-image middleware above: outside wwwroot, uploaded at runtime).
app.MapGet("/pdf-file/{fileName}", (string fileName, PdfFileStore pdfFileStore) =>
{
    var fullPath = pdfFileStore.GetFullPath(fileName);
    if (fullPath is null)
    {
        return Results.NotFound();
    }

    return Results.File(fullPath, "application/pdf");
});

// TEMPORARY, read-only - lets the Node rewrite's migration script enumerate every unit/device and
// the URLs to download their files from, since this app has no other JSON API at all. Doesn't
// mutate anything; safe to remove once the migration to the new stack is complete.
app.MapGet("/export", (UnitStore unitStore, DeviceStore deviceStore, WatermarkStore watermarkStore, EnvironmentMapStore environmentMapStore) =>
{
    var units = unitStore.GetAll().Select(u => new
    {
        u.Id,
        u.Name,
        Rotation = new { u.Rotation.X, u.Rotation.Y, u.Rotation.Z },
        u.Zones,
        u.WidthPerZone,
        u.Height,
        u.AveragePowerUsage,
        u.WarrantyYears,
        u.ModelFileName,
        ModelUrl = u.ModelUrl,
        u.PdfFileName,
        PdfUrl = u.PdfUrl,
    });

    var devices = deviceStore.GetAll().Select(d =>
    {
        var watermark = watermarkStore.Get(d.Id);
        var environmentMap = environmentMapStore.Get(d.Id);
        return new
        {
            d.Id,
            d.Key,
            d.DisplayName,
            d.BackgroundColor,
            d.ActiveUnitId,
            Watermark = watermark is null ? null : new { watermark.OriginalFileName, watermark.Grayscale, Url = watermarkStore.Url(watermark) },
            EnvironmentMap = environmentMap is null ? null : new { environmentMap.OriginalFileName, Url = environmentMapStore.Url(environmentMap) },
        };
    });

    return Results.Json(new { units, devices });
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
