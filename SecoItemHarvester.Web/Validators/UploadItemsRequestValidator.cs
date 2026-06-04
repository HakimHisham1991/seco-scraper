using FluentValidation;
using SecoItemHarvester.Web.Dtos;
using SecoItemHarvester.Web.Services;

namespace SecoItemHarvester.Web.Validators;

public class UploadItemsRequestValidator : AbstractValidator<UploadItemsRequest>
{
    public UploadItemsRequestValidator()
    {
        RuleFor(x => x.ItemNumbers)
            .NotEmpty()
            .WithMessage("At least one item number is required.");

        RuleForEach(x => x.ItemNumbers)
            .Must(n => !string.IsNullOrWhiteSpace(ItemNumberNormalizer.Normalize(n)))
            .WithMessage("Item numbers must contain digits.");
    }
}
