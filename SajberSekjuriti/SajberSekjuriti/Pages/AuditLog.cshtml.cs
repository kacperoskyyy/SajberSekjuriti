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
        var distinctActions = await _auditLogService.GetDistinctActionsAsync();
        ActionTypes = new SelectList(distinctActions.OrderBy(a => a));

        if (CurrentPage < 1) CurrentPage = 1;

        var paginatedResult = await _auditLogService.GetPaginatedFilteredLogsAsync(
            SearchUsername,
            SearchAction,
            StartDate,
            EndDate,
            CurrentPage,
            PageSize
        );

        Logs = paginatedResult.Logs;
        TotalItems = (int)paginatedResult.TotalItems;
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
            paginatedResult = await _auditLogService.GetPaginatedFilteredLogsAsync(
                SearchUsername,
                SearchAction,
                StartDate,
                EndDate,
                CurrentPage,
                PageSize
            );
            Logs = paginatedResult.Logs;
        }
    }
}