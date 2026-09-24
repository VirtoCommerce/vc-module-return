using FluentValidation;
using VirtoCommerce.ReturnModule.Core.Models;
using static VirtoCommerce.Platform.Data.Infrastructure.DbContextBase;

namespace VirtoCommerce.ReturnModule.Data.Validation;

// The buyer's strings reach EF unchecked otherwise, and an over-long one surfaces as a
// DbUpdateException - a 500 where the caller deserves a typed failure it can act on.
public class ReturnRequestValidator : AbstractValidator<ReturnRequestValidationContext>
{
    public ReturnRequestValidator()
    {
        RuleFor(x => x.CustomerReference).MaximumLength(Length128);
        RuleFor(x => x.CustomerComment).MaximumLength(Length2048);
        RuleFor(x => x.LanguageCode).MaximumLength(LanguageCodeLength);

        RuleForEach(x => x.Items).SetValidator(x => new ReturnRequestItemValidator(x.Reasons, x.ReasonsRequiringComment, x.RequireReason));
    }
}
