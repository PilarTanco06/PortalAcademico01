using Microsoft.AspNetCore.Identity;
using PortalAcademico01.Models;

namespace PortalAcademico01.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Crear rol Coordinador
            if (!await roleManager.RoleExistsAsync("Coordinador"))
            {
                await roleManager.CreateAsync(new IdentityRole("Coordinador"));
            }

            // Crear usuario coordinador
            var coordinador = await userManager.FindByEmailAsync("coordinador@usmp.pe");
            if (coordinador == null)
            {
                coordinador = new IdentityUser
                {
                    UserName = "coordinador@usmp.pe",
                    Email = "coordinador@usmp.pe",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(coordinador, "Coordinador123!");
                await userManager.AddToRoleAsync(coordinador, "Coordinador");
            }

            // Crear cursos iniciales 
            if (!context.Cursos.Any())
            {
                var cursos = new List<Curso>
                {
                    new Curso
                    {
                        Codigo = "IO101",
                        Nombre = "Investigación Operativa",
                        Creditos = 4,
                        CupoMaximo = 30,
                        HorarioInicio = new TimeSpan(8, 0, 0),
                        HorarioFin = new TimeSpan(10, 0, 0),
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "BD201",
                        Nombre = "Base de Datos",
                        Creditos = 5,
                        CupoMaximo = 25,
                        HorarioInicio = new TimeSpan(10, 0, 0),
                        HorarioFin = new TimeSpan(12, 0, 0),
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "IS301",
                        Nombre = "Ingeniería de Software",
                        Creditos = 4,
                        CupoMaximo = 20,
                        HorarioInicio = new TimeSpan(14, 0, 0),
                        HorarioFin = new TimeSpan(16, 30, 0),
                        Activo = true
                    }
                };

                context.Cursos.AddRange(cursos);
                await context.SaveChangesAsync();
            }
        }
    }
}