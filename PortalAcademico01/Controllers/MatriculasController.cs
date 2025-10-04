using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico01.Data;
using PortalAcademico01.Models;
using System.Security.Claims;

namespace PortalAcademico01.Controllers
{
    [Authorize]
    public class MatriculasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MatriculasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Matriculas/Inscribirse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscribirse(int cursoId)
        {
            // 1. Validar que el usuario esté autenticado
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                TempData["Error"] = "Debe iniciar sesión para inscribirse en un curso.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }

            // 2. Obtener el curso con sus matrículas
            var curso = await _context.Cursos
                .Include(c => c.Matriculas)
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null || !curso.Activo)
            {
                TempData["Error"] = "El curso no existe o no está disponible.";
                return RedirectToAction("Catalogo", "Cursos");
            }

            // 3. Validar que no esté ya matriculado en este curso
            var yaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.CursoId == cursoId && 
                              m.UsuarioId == usuarioId && 
                              m.Estado != EstadoMatricula.Cancelada);

            if (yaMatriculado)
            {
                TempData["Error"] = "Ya estás inscrito en este curso.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }

            // 4. Validar que no se supere el cupo máximo
            var matriculasActivas = curso.Matriculas
                .Count(m => m.Estado != EstadoMatricula.Cancelada);

            if (matriculasActivas >= curso.CupoMaximo)
            {
                TempData["Error"] = "No hay cupos disponibles para este curso.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }

            // 5. Validar que no haya solapamiento de horarios
            var cursosMatriculados = await _context.Matriculas
                .Where(m => m.UsuarioId == usuarioId && m.Estado != EstadoMatricula.Cancelada)
                .Include(m => m.Curso)
                .Select(m => m.Curso)
                .ToListAsync();

            foreach (var cursoExistente in cursosMatriculados)
            {
                if (cursoExistente != null && HorariosSeSuperponen(curso, cursoExistente))
                {
                    TempData["Error"] = $"El horario de este curso se solapa con '{cursoExistente.Nombre}' ({cursoExistente.HorarioInicio:hh\\:mm} - {cursoExistente.HorarioFin:hh\\:mm}).";
                    return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
                }
            }

            // 6. Crear la matrícula en estado Pendiente
            var matricula = new Matricula
            {
                CursoId = cursoId,
                UsuarioId = usuarioId,
                FechaRegistro = DateTime.Now,
                Estado = EstadoMatricula.Pendiente
            };

            try
            {
                _context.Matriculas.Add(matricula);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"¡Te has inscrito exitosamente en '{curso.Nombre}'! Tu matrícula está en estado Pendiente.";
                return RedirectToAction("MisCursos");
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Ocurrió un error al procesar tu inscripción. Por favor, intenta nuevamente.";
                return RedirectToAction("Detalle", "Cursos", new { id = cursoId });
            }
        }

        // GET: Matriculas/MisCursos
        public async Task<IActionResult> MisCursos()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var matriculas = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            return View(matriculas);
        }

        // Método auxiliar para verificar solapamiento de horarios
        private bool HorariosSeSuperponen(Curso curso1, Curso curso2)
        {
            // Dos cursos se solapan si:
            // - El inicio de uno está entre el inicio y fin del otro, O
            // - El fin de uno está entre el inicio y fin del otro, O
            // - Uno contiene completamente al otro
            
            return (curso1.HorarioInicio < curso2.HorarioFin && curso1.HorarioFin > curso2.HorarioInicio);
        }
    }
}