using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize(Roles = "ExcelWorker")]
public class ExcelModel : PageModel
{
    private readonly ILogger<ExcelModel> _logger;

    public ExcelModel(ILogger<ExcelModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    [Required]
    [DataType(DataType.Upload)]
    [Display(Name = "Excel file")]
    public IFormFile? File { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || File == null)
        {
            return Page();
        }

        using var ms = new MemoryStream();
        await File.CopyToAsync(ms);
        byte[] fileBytes = ms.ToArray();
        _logger.LogInformation("Uploaded Excel file {Name} ({Size} bytes)", File.FileName, fileBytes.Length);

        TempData["UploadSuccess"] = $"File '{File.FileName}' uploaded successfully.";
        return RedirectToPage();
    }
}
