using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models;
using BioGamaEcuador.Models.Sales;
using System.Linq;

namespace BioGamaEcuador.Controllers
{
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index(
            string? busqueda,
            string? modalidad,
            string? estado,
            int page = 1,
            int pageSize = 12)
        {
            var query = _context.Courses
                .Include(c => c.Species)
                .Where(c => c.IsActive)
                .AsQueryable();

            query = BioGamaEcuador.Services.CourseSearch.Filter(query, busqueda);

            if (!string.IsNullOrWhiteSpace(modalidad))
            {
                query = query.Where(c => c.Modality == modalidad);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var now = DateTime.UtcNow;
                var today = now.Date;
                query = estado switch
                {
                    "abierto" => query.Where(c => (c.TotalSeats - c.ReservedSeats - c.ConfirmedSeats) > 0 && c.StartDate > now),
                    "agotado" => query.Where(c => (c.TotalSeats - c.ReservedSeats - c.ConfirmedSeats) <= 0 && c.StartDate > now),
                    "finalizado" => query.Where(c => c.EndDate < today),
                    "en-curso" => query.Where(c => c.StartDate <= now && c.EndDate >= today),
                    _ => query
                };
            }

            query = query.OrderBy(c => c.StartDate);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

            var courses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Busqueda = busqueda;
            ViewBag.ModalidadFiltro = modalidad;
            ViewBag.EstadoFiltro = estado;
            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPages;
            ViewBag.ProximosCursos = await _context.Courses.CountAsync(c => c.IsActive && c.StartDate > DateTime.UtcNow);
            ViewBag.TotalCupos = await _context.Courses
                .Where(c => c.IsActive && c.StartDate > DateTime.UtcNow)
                .SumAsync(c => c.TotalSeats - c.ReservedSeats - c.ConfirmedSeats);

            return View(courses);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Species)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }
    }
}
