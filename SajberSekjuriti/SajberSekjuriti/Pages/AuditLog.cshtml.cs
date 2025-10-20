using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;

namespace SajberSekjuriti.Pages
{
    [Authorize(Roles = "Admin")]
    public class AuditLogModel : PageModel
    {
        private readonly AuditLogService _auditLogService;

        public AuditLogModel(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public List<AuditLog> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            Logs = await _auditLogService.GetAllLogsAsync();
        }
    }
}