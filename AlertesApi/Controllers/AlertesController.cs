using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlertesApi.Data;
using AlertesApi.Models;
using Microsoft.AspNetCore.SignalR;
using AlertesApi.Hubs;
using Newtonsoft.Json; // Obligatoire pour JsonConvert

namespace AlertesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertesController : ControllerBase
    {
        private readonly AlertesContext _context;
        private readonly IHubContext<AlertesHub> _hubContext;

        public AlertesController(AlertesContext context, IHubContext<AlertesHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/Alertes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Alerte>>> GetAlertes()
        {
            return await _context.Alertes.ToListAsync();
        }

        // GET: api/Alertes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Alerte>> GetAlerte(int id)
        {
            var alerte = await _context.Alertes.FindAsync(id);

            if (alerte == null)
            {
                return NotFound();
            }

            return alerte;
        }

        // POST: api/Alertes
        [HttpPost]
        public async Task<ActionResult<Alerte>> PostAlerte(Alerte alerte)
        {
            alerte.DateCreation = DateTime.UtcNow;
            alerte.EstLue = false;
            alerte.EstArchivee = false;

            _context.Alertes.Add(alerte);
            await _context.SaveChangesAsync();

            // === ENVOI EN TEMPS RÉEL À TOUS LES CLIENTS WPF ===
            var alertForClient = new
            {
                Titre = alerte.Titre,
                Message = alerte.Message,
                Niveau = alerte.Niveau // "Info", "Warning" ou "Critical"
            };

            await _hubContext.Clients.All.SendAsync("ReceiveAlert", JsonConvert.SerializeObject(alertForClient));

            return CreatedAtAction(nameof(GetAlerte), new { id = alerte.Id }, alerte);
        }

        // PUT: api/Alertes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlerte(int id, Alerte alerte)
        {
            if (id != alerte.Id)
            {
                return BadRequest();
            }

            _context.Entry(alerte).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlerteExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            // Envoi mise à jour en temps réel (optionnel)
            var alertForClient = new
            {
                Titre = alerte.Titre,
                Message = alerte.Message,
                Niveau = alerte.Niveau
            };

            await _hubContext.Clients.All.SendAsync("ReceiveAlert", JsonConvert.SerializeObject(alertForClient));

            return NoContent();
        }

        // DELETE: api/Alertes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlerte(int id)
        {
            var alerte = await _context.Alertes.FindAsync(id);
            if (alerte == null)
            {
                return NotFound();
            }

            _context.Alertes.Remove(alerte);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AlerteExists(int id)
        {
            return _context.Alertes.Any(e => e.Id == id);
        }
    }
}