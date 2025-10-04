using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico01.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar el puerto desde la variable de entorno PORT de Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=/data/portalacademico.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configurar Redis para Sesiones y Cache
var redisConnection = builder.Configuration.GetConnectionString("Redis") 
    ?? builder.Configuration["Redis:ConnectionString"];

// Validar si Redis está disponible
if (!string.IsNullOrEmpty(redisConnection))
{
    // Session con Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "PortalAcademico_";
    });
    
    Console.WriteLine("✓ Redis configurado correctamente");
}
else
{
    // Fallback a cache en memoria si Redis no está configurado
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine("⚠ Redis no configurado, usando cache en memoria");
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed data y migraciones
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Aplicar migraciones pendientes automáticamente
        Console.WriteLine("Aplicando migraciones...");
        await context.Database.MigrateAsync();
        Console.WriteLine("✓ Migraciones aplicadas");
        
        // Inicializar datos semilla
        Console.WriteLine("Inicializando datos semilla...");
        await SeedData.Initialize(services);
        Console.WriteLine("✓ Datos semilla inicializados");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la inicialización de la base de datos");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Solo usar HTTPS redirection en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Habilitar sesiones
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

Console.WriteLine($"Aplicación iniciada en modo: {app.Environment.EnvironmentName}");
Console.WriteLine($"Escuchando en puerto: {port}");

app.Run();