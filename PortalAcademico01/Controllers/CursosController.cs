using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PortalAcademico01.Data;
using PortalAcademico01.Models;
using System.Text.Json;

namespace PortalAcademico01.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private const string CACHE_KEY_CURSOS = "cursos_activos";
        private const int CACHE_DURATION_SECONDS = 60;

        public CursosController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Cursos/Catalogo
        public async Task<IActionResult> Catalogo(string? nombre, int? creditosMin, int? creditosMax, string? horarioInicio, string? horarioFin)
        {
            // Validar filtros
            if (!ValidarFiltros(creditosMin, creditosMax, horarioInicio, horarioFin, out string mensajeError))
            {
                ViewBag.Error = mensajeError;
                ViewBag.Nombre = nombre;
                ViewBag.CreditosMin = creditosMin;
                ViewBag.CreditosMax = creditosMax;
                ViewBag.HorarioInicio = horarioInicio;
                ViewBag.HorarioFin = horarioFin;
                return View(new List<Curso>());
            }

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

            // Intentar obtener del caché si no hay filtros específicos
            List<Curso> cursos;
            bool usandoCache = false;

            if (string.IsNullOrWhiteSpace(nombre) &&
                !creditosMin.HasValue &&
                !creditosMax.HasValue &&
                string.IsNullOrWhiteSpace(horarioInicio) &&
                string.IsNullOrWhiteSpace(horarioFin))
            {
                var cursosCache = await _cache.GetStringAsync(CACHE_KEY_CURSOS);
                
                if (!string.IsNullOrEmpty(cursosCache))
                {
                    cursos = JsonSerializer.Deserialize<List<Curso>>(cursosCache) ?? new List<Curso>();
                    usandoCache = true;
                    ViewBag.UsandoCache = true;
                }
                else
                {
                    cursos = await ObtenerYCachearCursos();
                    ViewBag.UsandoCache = false;
                }
            }
            else
            {
                // Si hay filtros, consultar directamente la base de datos
                var cursosQuery = _context.Cursos.Where(c => c.Activo);

                // Aplicar filtros
                if (!string.IsNullOrWhiteSpace(nombre))
                {
                    cursosQuery = cursosQuery.Where(c => c.Nombre.Contains(nombre) || c.Codigo.Contains(nombre));
                }

                if (creditosMin.HasValue && creditosMin.Value > 0)
                {
                    cursosQuery = cursosQuery.Where(c => c.Creditos >= creditosMin.Value);
                }

                if (creditosMax.HasValue && creditosMax.Value > 0)
                {
                    cursosQuery = cursosQuery.Where(c => c.Creditos <= creditosMax.Value);
                }

                cursos = await cursosQuery.ToListAsync();

                // Filtros de horario en memoria
                if (!string.IsNullOrWhiteSpace(horarioInicio) && TimeSpan.TryParse(horarioInicio, out var horarioInicioTime))
                {
                    cursos = cursos.Where(c => c.HorarioInicio >= horarioInicioTime).ToList();
                }

                if (!string.IsNullOrWhiteSpace(horarioFin) && TimeSpan.TryParse(horarioFin, out var horarioFinTime))
                {
                    cursos = cursos.Where(c => c.HorarioFin <= horarioFinTime).ToList();
                }
            }

            // Pasar filtros a la vista
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

            // Guardar en sesión el último curso visitado
            HttpContext.Session.SetInt32("UltimoCursoId", curso.Id);
            HttpContext.Session.SetString("UltimoCursoNombre", curso.Nombre);

            // Calcular cupos disponibles
            var matriculasActivas = curso.Matriculas.Count(m => m.Estado != EstadoMatricula.Cancelada);
            ViewBag.CuposDisponibles = curso.CupoMaximo - matriculasActivas;

            return View(curso);
        }

        // Método auxiliar para obtener y cachear cursos
        private async Task<List<Curso>> ObtenerYCachearCursos()
        {
            var cursos = await _context.Cursos
                .Where(c => c.Activo)
                .ToListAsync();

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_DURATION_SECONDS)
            };

            var cursosJson = JsonSerializer.Serialize(cursos);
            await _cache.SetStringAsync(CACHE_KEY_CURSOS, cursosJson, options);

            return cursos;
        }

        // Método para invalidar caché (llamar cuando se cree/edite un curso)
        public async Task InvalidarCache()
        {
            await _cache.RemoveAsync(CACHE_KEY_CURSOS);
        }

        // Método para validar los filtros
        private bool ValidarFiltros(int? creditosMin, int? creditosMax, string? horarioInicio, string? horarioFin, out string mensajeError)
        {
            mensajeError = string.Empty;

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