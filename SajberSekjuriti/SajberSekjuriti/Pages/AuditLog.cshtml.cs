using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajberSekjuriti.Services;
using SajberSekjuriti.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SajberSekjuriti.Pages
{
    public class AuditLogModel : PageModel
    {
        private readonly AuditLogService _auditLogService;

        // --- Ustawiamy rozmiar strony ---
        private const int PageSize = 100;

        public IEnumerable<AuditLog> Logs { get; set; } = new List<AuditLog>();

        // --- W�a�ciwo�ci filtrowania (z poprzedniej wersji) ---
        [BindProperty(SupportsGet = true)]
        public string? SearchUsername { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? SearchAction { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public SelectList ActionTypes { get; set; }

        // --- NOWE W�a�ciwo�ci dla paginacji ---
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1; // Domy�lnie strona 1
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public AuditLogModel(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public async Task OnGetAsync()
        {
            // --- UWAGA DOT. WYDAJNO�CI (Nadal aktualna!) ---
            // Ten kod nadal pobiera WSZYSTKIE logi z bazy, a potem filtruje.
            // Przy du�ej ilo�ci danych, filtry i paginacja powinny
            // by� realizowane po stronie bazy danych, a nie w pami�ci.
            var allLogs = await _auditLogService.GetAllLogsAsync();

            var distinctActions = allLogs
                .OrderBy(log => log.Action)
                .Select(log => log.Action)
                .Distinct()
                .ToList();
            ActionTypes = new SelectList(distinctActions);

            // --- Logika filtrowania (bez zmian) ---
            IEnumerable<AuditLog> filteredLogs = allLogs;

            if (!string.IsNullOrEmpty(SearchUsername))
            {
                filteredLogs = filteredLogs.Where(log =>
                    log.Username.Contains(SearchUsername, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(SearchAction))
            {
                filteredLogs = filteredLogs.Where(log => log.Action == SearchAction);
            }
            if (StartDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.Timestamp >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.Timestamp < EndDate.Value.AddDays(1));
            }

            // --- NOWA Logika Paginacji ---

            // 1. Sortujemy PRZED paginacj�
            var sortedLogs = filteredLogs.OrderByDescending(log => log.Timestamp);

            // 2. Obliczamy statystyki
            TotalItems = sortedLogs.Count();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            // 3. Zabezpieczenie przed b��dnym numerem strony w URL
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // 4. Pobieramy tylko logi dla bie��cej strony
            Logs = sortedLogs
                .Skip((CurrentPage - 1) * PageSize) // Pomi� logi z poprzednich stron
                .Take(PageSize) // We� 100 log�w
                .ToList();
        }
    }
}