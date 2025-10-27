using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajberSekjuriti.Services;
using SajberSekjuriti.Model;


namespace SajberSekjuriti.Pages;

public class AuditLogModel : PageModel
{
    private readonly AuditLogService _auditLogService;

    private const int PageSize = 100;

    public IEnumerable<AuditLog> Logs { get; set; } = new List<AuditLog>();

    [BindProperty(SupportsGet = true)]
    public string? SearchUsername { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? SearchAction { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public SelectList ActionTypes { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public AuditLogModel(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task OnGetAsync()
    {
        var allLogs = await _auditLogService.GetAllLogsAsync();

        var distinctActions = allLogs
            .OrderBy(log => log.Action)
            .Select(log => log.Action)
            .Distinct()
            .ToList();
        ActionTypes = new SelectList(distinctActions);

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


        var sortedLogs = filteredLogs.OrderByDescending(log => log.Timestamp);

        TotalItems = sortedLogs.Count();
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        Logs = sortedLogs
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}