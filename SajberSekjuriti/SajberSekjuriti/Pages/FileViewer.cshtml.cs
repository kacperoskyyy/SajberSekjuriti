using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xceed.Words.NET;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Logging;

namespace SajberSekjuriti.Pages
{
    [Authorize]
    public class FileViewerModel : PageModel
    {
        private readonly UserService _userService;
        private readonly VigenereCipherService _vigenereService;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<FileViewerModel> _logger;

        private const string VIGENERE_MASTER_KEY = "SAJBERSEKJURITI";
        private const int MaxPreviewChars = 8192;           // max znaków do podgl¹du
        private const long MaxFileSizeBytes = 10L * 1024 * 1024; // 10 MB

        public FileViewerModel(UserService userService,
                               VigenereCipherService vigenereService,
                               AuditLogService auditLogService,
                               ILogger<FileViewerModel> logger)
        {
            _userService = userService;
            _vigenereService = vigenereService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public string? ErrorMessage { get; set; }
        public string? LicenseErrorMessage { get; set; }
        public string? FileContent { get; set; }
        public string? FileName { get; set; }
        public bool IsUnlocked { get; set; }

        [BindProperty]
        public string? LicenseKey { get; set; }

        private async Task<User> GetCurrentUserAsync()
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("[{TraceId}] Brak uwierzytelnionego u¿ytkownika podczas próby pobrania danych.", HttpContext.TraceIdentifier);
                throw new InvalidOperationException("Brak uwierzytelnionego u¿ytkownika.");
            }
            _logger.LogDebug("[{TraceId}] Pobieranie u¿ytkownika: {Username}", HttpContext.TraceIdentifier, username);
            return await _userService.GetByUsernameAsync(username);
        }

        public async Task OnGetAsync()
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = HttpContext.TraceIdentifier });
            _logger.LogInformation("OnGetAsync start");
            var sw = Stopwatch.StartNew();
            var user = await GetCurrentUserAsync();
            IsUnlocked = user.IsFileViewerUnlocked;
            sw.Stop();
            _logger.LogInformation("OnGetAsync koniec (IsUnlocked={IsUnlocked}, {Elapsed} ms)", IsUnlocked, sw.ElapsedMilliseconds);
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["Path"] = HttpContext.Request.Path.ToString()
            });

            _logger.LogInformation("--- OnPostAsync START ---");
            var sw = Stopwatch.StartNew();

            var user = await GetCurrentUserAsync();
            IsUnlocked = user.IsFileViewerUnlocked;
            _logger.LogInformation("U¿ytkownik: {Username}, Odblokowany: {IsUnlocked}", user.Username, IsUnlocked);

            if (file == null)
            {
                ErrorMessage = "Nie wybrano pliku.";
                _logger.LogWarning("Nie wybrano pliku (file == null)");
                return Page();
            }

            if (file.Length == 0)
            {
                ErrorMessage = "Plik jest pusty.";
                _logger.LogWarning("Pusty plik: {FileName}", file.FileName);
                return Page();
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            FileName = file.FileName;
            _logger.LogInformation("Przetwarzanie pliku: {FileName}, Rozszerzenie: {Extension}, Rozmiar: {Length} B", FileName, extension, file.Length);

            if (file.Length > MaxFileSizeBytes)
            {
                ErrorMessage = $"B£¥D: Plik jest zbyt du¿y. Maksymalny rozmiar to {MaxFileSizeBytes / (1024 * 1024)} MB.";
                _logger.LogWarning("Plik za du¿y: {FileName} ({Length} B)", FileName, file.Length);
                try { await _auditLogService.LogAsync(user.Username, "Otwieranie pliku", $"Nieudana próba (Plik za du¿y): {file.FileName}."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (za du¿y plik)"); }
                return Page();
            }

            try
            {
                _logger.LogInformation("Rozpoczynanie odczytu. Tryb odblokowany: {IsUnlocked}", IsUnlocked);
                switch (extension)
                {
                    case ".txt":
                    case ".log":
                    case ".csv":
                        _logger.LogInformation("Œcie¿ka tekstowa (.txt/.log/.csv)");
                        FileContent = await ReadTextFileAsync(file, MaxPreviewChars);
                        break;

                    //case ".docx" when IsUnlocked:
                    //    _logger.LogInformation("Œcie¿ka .docx (odblokowana)");
                    //    FileContent = ReadDocxFile(file);
                    //    break;

                    //case ".pdf" when IsUnlocked:
                    //    _logger.LogInformation("Œcie¿ka .pdf (odblokowana)");
                    //    FileContent = ReadPdfFile(file);
                    //    break;

                    default:
                        _logger.LogWarning("Nieobs³ugiwane rozszerzenie lub tryb DEMO: {Extension}", extension);
                        if (!IsUnlocked && extension != ".txt")
                        {
                            ErrorMessage = "DEMOWARE: Mo¿na otwieraæ tylko pliki w formacie .TXT.";
                            try { await _auditLogService.LogAsync(user.Username, "Otwieranie pliku", $"Nieudana próba (DEMO): Plik {file.FileName} zablokowany."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (DEMO)"); }
                            return Page();
                        }
                        if (IsUnlocked)
                        {
                            ErrorMessage = $"Plik '{file.FileName}' zosta³ poprawnie przes³any, ale typ pliku '{extension}' nie jest obs³ugiwany do podgl¹du.";
                            FileContent = "[Plik binarny lub nieobs³ugiwany]";
                            try { await _auditLogService.LogAsync(user.Username, "Otwieranie pliku", $"Pomyœlnie otwarto (bez podgl¹du): {file.FileName}."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (nieobs³ugiwany)"); }
                        }
                        return Page();
                }

                if (FileContent != null)
                {
                    if (FileContent.Length > MaxPreviewChars)
                    {
                        _logger.LogInformation("Podgl¹d obciêty do {MaxPreviewChars} znaków (oryginalnie {Length})", MaxPreviewChars, FileContent.Length);
                        FileContent = FileContent.Substring(0, MaxPreviewChars) + "\n\n[... Plik zosta³ obciêty (wyœwietlono pierwsze 8KB) ...]";
                    }
                    else
                    {
                        _logger.LogDebug("D³ugoœæ wczytanego podgl¹du: {Length}", FileContent.Length);
                    }
                }
                else
                {
                    _logger.LogDebug("FileContent == null po odczycie");
                }

                _logger.LogInformation("Pomyœlnie przetworzono plik.");
                try { await _auditLogService.LogAsync(user.Username, "Otwieranie pliku", $"Pomyœlnie otwarto podgl¹d pliku: {file.FileName}."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (sukces)"); }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KRYTYCZNY B£¥D podczas przetwarzania pliku {FileName} (ext={Extension}, size={Size})", FileName, extension, file.Length);
                ErrorMessage = $"B³¹d podczas przetwarzania pliku: {ex.Message}";
                try { await _auditLogService.LogAsync(user.Username, "B³¹d otwierania", $"B³¹d pliku {file.FileName}: {ex.Message}."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (wyj¹tek)"); }
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("--- OnPostAsync KONIEC --- ({Elapsed} ms)", sw.ElapsedMilliseconds);
            }

            return Page();
        }

        private async Task<string> ReadTextFileAsync(IFormFile file, int charLimit)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = HttpContext.TraceIdentifier });
            _logger.LogInformation("ReadTextFileAsync: Otwieranie pliku: {FileName}, limit={Limit}", file.FileName, charLimit);
            char[] buffer = new char[charLimit];
            int charsRead;

            try
            {
                _logger.LogInformation("Próba odczytu jako UTF-8 (BOM detection w³¹czony)");
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                    _logger.LogInformation("UTF-8: odczytano {CharsRead} znaków", charsRead);
                    return new string(buffer, 0, charsRead);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Odczyt jako UTF-8 nie powiód³ siê. Próba jako ISO-8859-1 (Latin1)");
                try
                {
                    _logger.LogInformation("Próba odczytu jako ISO-8859-1");
                    var fallbackEncoding = Encoding.GetEncoding("ISO-8859-1");
                    using (var stream = file.OpenReadStream())
                    using (var reader = new StreamReader(stream, fallbackEncoding, detectEncodingFromByteOrderMarks: false))
                    {
                        charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                        _logger.LogInformation("ISO-8859-1: odczytano {CharsRead} znaków", charsRead);
                        return new string(buffer, 0, charsRead);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Odczyt jako ISO-8859-1 równie¿ nie powiód³ siê");
                    throw new Exception("Nie mo¿na by³o odczytaæ pliku ani jako UTF-8, ani jako kodowanie zapasowe.", fallbackEx);
                }
            }
        }

        private string ReadDocxFile(IFormFile file)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = HttpContext.TraceIdentifier });
            try
            {
                _logger.LogInformation("Odczytywanie pliku .docx: {FileName}", file.FileName);
                using (var stream = file.OpenReadStream())
                using (var doc = DocX.Load(stream))
                {
                    var text = doc.Text ?? string.Empty;
                    _logger.LogDebug(".docx: d³ugoœæ tekstu={Length}", text.Length);
                    return text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "B³¹d odczytu .docx");
                return "[B£¥D: Nie mo¿na odczytaæ pliku .docx. Mo¿e byæ uszkodzony lub w starym formacie .doc]";
            }
        }

        private string ReadPdfFile(IFormFile file)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = HttpContext.TraceIdentifier });
            try
            {
                _logger.LogInformation("Odczytywanie pliku .pdf: {FileName}", file.FileName);
                var sb = new StringBuilder();
                using (var stream = file.OpenReadStream())
                using (var pdfReader = new PdfReader(stream))
                using (var pdfDoc = new PdfDocument(pdfReader))
                {
                    var pages = pdfDoc.GetNumberOfPages();
                    _logger.LogInformation(".pdf: liczba stron={Pages}", pages);
                    for (int i = 1; i <= pages; i++)
                    {
                        var page = pdfDoc.GetPage(i);
                        sb.Append(PdfTextExtractor.GetTextFromPage(page));
                    }
                }
                var text = sb.ToString();
                _logger.LogDebug(".pdf: d³ugoœæ tekstu={Length}", text.Length);
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "B³¹d odczytu .pdf");
                return "[B£¥D: Nie mo¿na odczytaæ pliku .pdf. Plik mo¿e byæ obrazem lub byæ uszkodzony.]";
            }
        }

        public async Task<IActionResult> OnPostUnlockAsync()
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = HttpContext.TraceIdentifier });
            _logger.LogInformation("--- OnPostUnlockAsync START ---");
            var sw = Stopwatch.StartNew();

            var user = await GetCurrentUserAsync();
            IsUnlocked = user.IsFileViewerUnlocked;
            _logger.LogDebug("Status przed odblokowaniem: IsUnlocked={IsUnlocked}", IsUnlocked);

            if (string.IsNullOrEmpty(LicenseKey))
            {
                LicenseErrorMessage = "Klucz nie mo¿e byæ pusty.";
                _logger.LogWarning("Próba odblokowania pustym kluczem");
                return Page();
            }

            _logger.LogInformation("Próba odszyfrowania klucza dla u¿ytkownika: {Username}", user.Username);
            string decryptedKey = _vigenereService.Decrypt(LicenseKey, VIGENERE_MASTER_KEY);
            _logger.LogDebug("D³ugoœæ odszyfrowanego klucza: {Len}", decryptedKey?.Length);

            if (decryptedKey == user.Username.ToUpper())
            {
                _logger.LogInformation("Klucz poprawny. Odblokowywanie funkcji.");
                user.IsFileViewerUnlocked = true;
                await _userService.UpdateAsync(user);
                try { await _auditLogService.LogAsync(user.Username, "Odblokowano licencjê", "U¿ytkownik odblokowa³ funkcjê FileViewer."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (odblokowanie)"); }
            }
            else
            {
                _logger.LogWarning("Niepoprawny klucz licencyjny dla u¿ytkownika: {Username}", user.Username);
                LicenseErrorMessage = "Niepoprawny klucz licencyjny.";
                try { await _auditLogService.LogAsync(user.Username, "B³êdny klucz", "Nieudana próba odblokowania licencji."); } catch (Exception logEx) { _logger.LogDebug(logEx, "B³¹d logowania audytu (b³êdny klucz)"); }
            }

            sw.Stop();
            _logger.LogInformation("--- OnPostUnlockAsync KONIEC --- ({Elapsed} ms)", sw.ElapsedMilliseconds);
            return RedirectToPage();
        }
    }
}

