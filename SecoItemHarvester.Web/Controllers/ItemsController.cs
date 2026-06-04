using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCaching;
using SecoItemHarvester.Web.Dtos;
using SecoItemHarvester.Web.Repositories;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Web.Controllers;

[ApiController]
[Route("api/items")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ItemsController : ControllerBase
{
    private readonly IItemLookupRepository _repository;
    private readonly IItemInputParser _inputParser;
    private readonly ICsvExportService _csvExportService;
    private readonly IProcessingTrigger _processingTrigger;
    private readonly IValidator<UploadItemsRequest> _uploadValidator;

    public ItemsController(
        IItemLookupRepository repository,
        IItemInputParser inputParser,
        ICsvExportService csvExportService,
        IProcessingTrigger processingTrigger,
        IValidator<UploadItemsRequest> uploadValidator)
    {
        _repository = repository;
        _inputParser = inputParser;
        _csvExportService = csvExportService;
        _processingTrigger = processingTrigger;
        _uploadValidator = uploadValidator;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromBody] UploadItemsRequest request, CancellationToken cancellationToken)
    {
        var validation = await _uploadValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var normalized = request.ItemNumbers
            .Select(ItemNumberNormalizer.Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        await _repository.ClearAllAsync(cancellationToken);
        var added = await _repository.AddPendingItemsAsync(normalized, cancellationToken);
        return Ok(new { added, totalSubmitted = normalized.Count, cleared = true });
    }

    [HttpPost("upload/csv")]
    public async Task<IActionResult> UploadCsv(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("CSV file is required.");
        }

        await using var stream = file.OpenReadStream();
        var numbers = await _inputParser.ParseCsvAsync(stream, cancellationToken);
        await _repository.ClearAllAsync(cancellationToken);
        var added = await _repository.AddPendingItemsAsync(numbers, cancellationToken);
        return Ok(new { added, totalSubmitted = numbers.Count, cleared = true });
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearAll(CancellationToken cancellationToken)
    {
        var removed = await _repository.ClearAllAsync(cancellationToken);
        return Ok(new { removed, message = "All results cleared." });
    }

    [HttpPost("process")]
    public IActionResult StartProcessing()
    {
        _processingTrigger.RequestProcessing();
        return Accepted(new { message = "Processing queued." });
    }

    [HttpPost("retry-failed")]
    public async Task<IActionResult> RetryFailed(CancellationToken cancellationToken)
    {
        await _repository.ResetFailedToPendingAsync(cancellationToken);
        _processingTrigger.RequestProcessing();
        return Accepted(new { message = "Failed items reset to pending." });
    }

    [HttpGet("status")]
    public async Task<ProcessingStatusDto> GetStatus(CancellationToken cancellationToken) =>
        await _repository.GetStatusAsync(cancellationToken);

    [HttpGet("results")]
    public async Task<IActionResult> GetResults([FromQuery] int skip = 0, [FromQuery] int take = 500, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 2000);
        var results = await _repository.GetResultsAsync(skip, take, cancellationToken);
        return Ok(results);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var items = await _repository.GetCompletedForExportAsync(cancellationToken);
        var bytes = await _csvExportService.ExportAsync(items, cancellationToken);
        return File(bytes, "text/csv", $"seco-items-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
