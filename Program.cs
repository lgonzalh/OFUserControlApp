using Microsoft.EntityFrameworkCore;
using OFUserControlApp.Application.Interfaces;
using OFUserControlApp.Application.Services;
using OFUserControlApp.Domain.Interfaces;
using OFUserControlApp.Infrastructure.Data;
using OFUserControlApp.Infrastructure.Repositories;
using OFUserControlApp.Infrastructure.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllersWithViews();

// Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR para notificaciones en tiempo real
builder.Services.AddSignalR();

// Repositorios (Infraestructura)
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Servicios de aplicación
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IExcelProcessorService, ExcelProcessorService>();
builder.Services.AddSingleton<IProgresoService, ProgresoService>();

// Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuración de EPPlus
ExcelPackage.License.SetNonCommercialPersonal("OFUserControlApp");

var app = builder.Build();

// Pipeline de middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Configuración de rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Hub de SignalR
app.MapHub<ProgresoHub>("/progressHub");

// Verificar conexión a la base de datos (sin crear la BD)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Solo verificar que podemos conectarnos, sin crear la BD
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("Conexión a la base de datos establecida correctamente");
        }
        else
        {
            logger.LogWarning("No se pudo conectar a la base de datos. Verifique la cadena de conexión y que la base de datos exista");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al verificar la conexión a la base de datos: {Message}", ex.Message);
        
        // No detener la aplicación, solo registrar el error
        // La aplicación puede funcionar sin verificación inicial de BD
    }
}

app.Run();
