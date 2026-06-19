using System.Text.Json;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Data;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Infrastructure.Services;

public class ComparisonService(ApplicationDbContext dbContext, IAuditService auditService) : IComparisonService
{
    public async Task<PagedResult<ComparisonJobListItemVm>> GetJobsAsync(string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.ComparisonJobs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.JobNumber.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ComparisonJobListItemVm
            {
                Id = x.Id,
                JobNumber = x.JobNumber,
                Title = x.Title,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToListAsync();

        return new PagedResult<ComparisonJobListItemVm> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ComparisonScreenVm?> GetJobScreenAsync(int id)
    {
        var job = await dbContext.ComparisonJobs.AsNoTracking().Include(x => x.Fields).FirstOrDefaultAsync(x => x.Id == id);
        if (job == null) return null;

        return new ComparisonScreenVm
        {
            JobId = job.Id,
            JobTitle = job.Title,
            PdfUrl = job.PdfFilePath,
            Fields = job.Fields.Select(f => new ComparisonFieldVm
            {
                Id = f.Id,
                FieldLabel = f.FieldLabel,
                FieldType = f.FieldType,
                ExpectedValue = f.ExpectedValue,
                ActualValue = f.ActualValue,
                IsRequired = f.IsRequired,
                IsBlocking = f.IsBlocking,
                IsMatch = f.IsMatch,
                MismatchReason = f.MismatchReason
            }).ToList()
        };
    }

    public async Task<int> SubmitAsync(ComparisonSubmitVm model, string userId)
    {
        var job = await dbContext.ComparisonJobs.Include(x => x.Fields).FirstOrDefaultAsync(x => x.Id == model.JobId);
        if (job == null)
        {
            throw new InvalidOperationException("Comparison job was not found.");
        }

        if (model.Fields.Count != job.Fields.Count)
        {
            throw new InvalidOperationException("All comparison fields must be submitted before the job can be completed.");
        }

        var duplicateFieldIds = model.Fields
            .GroupBy(x => x.Id)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateFieldIds.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate comparison field values were submitted: {string.Join(", ", duplicateFieldIds)}.");
        }

        var submittedFieldIds = model.Fields.Select(x => x.Id).ToHashSet();
        var missingFieldIds = job.Fields.Select(x => x.Id).Where(x => !submittedFieldIds.Contains(x)).ToList();
        if (missingFieldIds.Count > 0)
        {
            throw new InvalidOperationException($"Missing comparison field values: {string.Join(", ", missingFieldIds)}.");
        }

        foreach (var fieldInput in model.Fields)
        {
            var field = job.Fields.FirstOrDefault(x => x.Id == fieldInput.Id);
            if (field == null)
            {
                throw new InvalidOperationException($"Comparison field {fieldInput.Id} was not found for job {model.JobId}.");
            }

            if (field.IsRequired && string.IsNullOrWhiteSpace(fieldInput.ActualValue))
            {
                throw new InvalidOperationException($"{field.FieldLabel} is required before submission.");
            }

            field.ActualValue = fieldInput.ActualValue;
            field.IsMatch = Normalize(field.ExpectedValue) == Normalize(fieldInput.ActualValue);
            field.MismatchReason = field.IsMatch ? null : "Value differs after normalization";

            if (field.IsBlocking && !field.IsMatch)
            {
                throw new InvalidOperationException($"{field.FieldLabel} has a blocking mismatch and must be resolved before submission.");
            }
        }

        job.Status = "Submitted";

        var submission = new ComparisonSubmission
        {
            ComparisonJobId = job.Id,
            SubmittedByUserId = userId,
            FinalJson = JsonSerializer.Serialize(model),
            SubmittedAt = DateTime.UtcNow
        };

        dbContext.ComparisonSubmissions.Add(submission);
        await dbContext.SaveChangesAsync();
        await auditService.LogAsync("ComparisonSubmitted", $"ComparisonJob:{job.Id}", $"Job submitted with {job.Fields.Count} fields", userId);
        return submission.Id;
    }

    private static string Normalize(string? input) => (input ?? string.Empty).Trim().ToLowerInvariant();
}
