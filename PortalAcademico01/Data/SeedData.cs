using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico01.Data;
using PortalAcademico01.Models;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Crear rol Coordinador si no existe
        if (!await roleManager.RoleExistsAsync("Coordinador"))
        {
            await roleManager.CreateAsync(new IdentityRole("Coordinador"));
        }

        // Crear usuario coordinador si no existe
        var coordinadorEmail = "coordinador@usmp.pe";
        var coordinador = await userManager.FindByEmailAsync(coordinadorEmail);

        if (coordinador == null)
        {
            coordinador = new IdentityUser
            {
                UserName = coordinadorEmail,
                Email = coordinadorEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(coordinador, "Coordinador123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(coordinador, "Coordinador");
            }
        }

        // Seed de cursos si no existen
        if (!context.Cursos.Any())
        {
            context.Cursos.AddRange(
                new Curso
                {
                    Codigo = "BD101",
                    Nombre = "Base de Datos",
                    Creditos = 4,
                    CupoMaximo = 30,
                    HorarioInicio = new TimeSpan(8, 0, 0),
                    HorarioFin = new TimeSpan(10, 0, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "IO201",
                    Nombre = "Investigación Operativa I",
                    Creditos = 5,
                    CupoMaximo = 25,
                    HorarioInicio = new TimeSpan(10, 0, 0),
                    HorarioFin = new TimeSpan(12, 0, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "PROG101",
                    Nombre = "Programación I",
                    Creditos = 4,
                    CupoMaximo = 35,
                    HorarioInicio = new TimeSpan(14, 0, 0),
                    HorarioFin = new TimeSpan(16, 0, 0),
                    Activo = true
                }
            );

            await context.SaveChangesAsync();
        }
    }
}