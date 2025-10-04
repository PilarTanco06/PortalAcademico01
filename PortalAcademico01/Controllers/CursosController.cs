using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico01.Data;
using PortalAcademico01.Models;

namespace PortalAcademico01.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cursos/Catalogo
        public async Task<IActionResult> Catalogo(string? nombre, int? creditosMin, int? creditosMax, string? horarioInicio, string? horarioFin)
        {
            // Si no hay ningún filtro, retornar lista vacía
            if (string.IsNullOrWhiteSpace(nombre) &&
                !creditosMin.HasValue &&
                !creditosMax.HasValue &&
                string.IsNullOrWhiteSpace(horarioInicio) &&
                string.IsNullOrWhiteSpace(horarioFin))
            {
                ViewBag.Nombre = nombre;
                ViewBag.CreditosMin = creditosMin;
                ViewBag.CreditosMax = creditosMax;
                ViewBag.HorarioInicio = horarioInicio;
                ViewBag.HorarioFin = horarioFin;
                ViewBag.MostrarMensajeInicial = true;
                return View(new List<Curso>());
            }

            var cursosQuery = _context.Cursos.Where(c => c.Activo);

            // Filtro por nombre
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                cursosQuery = cursosQuery.Where(c => c.Nombre.Contains(nombre) || c.Codigo.Contains(nombre));
            }

            // Filtro por créditos mínimos
            if (creditosMin.HasValue && creditosMin.Value > 0)
            {
                cursosQuery = cursosQuery.Where(c => c.Creditos >= creditosMin.Value);
            }

            // Filtro por créditos máximos
            if (creditosMax.HasValue && creditosMax.Value > 0)
            {
                cursosQuery = cursosQuery.Where(c => c.Creditos <= creditosMax.Value);
            }

            // Obtener los cursos en memoria para filtrar horarios
            var cursos = await cursosQuery.ToListAsync();

            // Filtro por horario inicio (en memoria)
            if (!string.IsNullOrWhiteSpace(horarioInicio) && TimeSpan.TryParse(horarioInicio, out var horarioInicioTime))
            {
                cursos = cursos.Where(c => c.HorarioInicio >= horarioInicioTime).ToList();
            }

            // Filtro por horario fin (en memoria)
            if (!string.IsNullOrWhiteSpace(horarioFin) && TimeSpan.TryParse(horarioFin, out var horarioFinTime))
            {
                cursos = cursos.Where(c => c.HorarioFin <= horarioFinTime).ToList();
            }

            // Pasar filtros a la vista para mantenerlos
            ViewBag.Nombre = nombre;
            ViewBag.CreditosMin = creditosMin;
            ViewBag.CreditosMax = creditosMax;
            ViewBag.HorarioInicio = horarioInicio;
            ViewBag.HorarioFin = horarioFin;
            ViewBag.MostrarMensajeInicial = false;

            return View(cursos);
        }

        // GET: Cursos/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos
                .Include(c => c.Matriculas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (curso == null)
            {
                return NotFound();
            }

            // Calcular cupos disponibles
            var matriculasActivas = curso.Matriculas.Count(m => m.Estado != EstadoMatricula.Cancelada);
            ViewBag.CuposDisponibles = curso.CupoMaximo - matriculasActivas;

            return View(curso);
        }

        // Método para validar los filtros
private bool ValidarFiltros(int? creditosMin, int? creditosMax, string? horarioInicio, string? horarioFin, out string mensajeError)
{
    mensajeError = string.Empty;

    // Validar créditos negativos
    if (creditosMin.HasValue && creditosMin.Value < 0)
    {
        mensajeError = "Los créditos mínimos no pueden ser negativos.";
        return false;
    }

    if (creditosMax.HasValue && creditosMax.Value < 0)
    {
        mensajeError = "Los créditos máximos no pueden ser negativos.";
        return false;
    }

    // Validar que HorarioFin no sea anterior a HorarioInicio
    if (!string.IsNullOrWhiteSpace(horarioInicio) && !string.IsNullOrWhiteSpace(horarioFin))
    {
        if (TimeSpan.TryParse(horarioInicio, out var inicio) && TimeSpan.TryParse(horarioFin, out var fin))
        {
            if (fin < inicio)
            {
                mensajeError = "El horario de fin no puede ser anterior al horario de inicio.";
                return false;
            }
        }
    }

    return true;
}
    }
    
}