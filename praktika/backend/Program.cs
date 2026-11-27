using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var dataDir   = Path.Combine(builder.Environment.ContentRootPath, "data");
var uploadDir = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(uploadDir);

builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite($"Data Source={Path.Combine(dataDir, "gallery.db")}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadDir),
    RequestPath  = "/uploads"
});

app.MapGet("/api/images", (AppDb db) =>
    db.Images.OrderByDescending(x => x.Id)
        .Select(x => new { x.Id, url = $"/uploads/{x.Filename}", name = x.Original })
        .ToList());

app.MapPost("/api/images", async (IFormFile file, AppDb db) =>
{
    if (file is null || file.Length == 0) return Results.BadRequest("Файл пуст");

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    if (!allowed.Contains(ext)) return Results.BadRequest("Формат не разрешён");

    var filename = $"{Guid.NewGuid():N}{ext}";
    await using (var fs = File.Create(Path.Combine(uploadDir, filename)))
        await file.CopyToAsync(fs);

    db.Images.Add(new Image { Filename = filename, Original = file.FileName });
    await db.SaveChangesAsync();
    return Results.Ok();
}).DisableAntiforgery();

app.Run();

class Image
{
    public int Id { get; set; }
    public string Filename { get; set; } = "";
    public string Original { get; set; } = "";
}

class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> o) : base(o) { }
    public DbSet<Image> Images => Set<Image>();
}