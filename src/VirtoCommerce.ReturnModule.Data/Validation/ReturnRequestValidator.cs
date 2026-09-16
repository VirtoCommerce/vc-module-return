using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using VirtoCommerce.Platform.Core.Common;
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

        RuleForEach(x => x.Items).SetValidator(x => new ReturnRequestItemValidator(x.Reasons, x.ReasonsRequiringComment));
    }
}

public class ReturnRequestItemValidator : AbstractValidator<CreateReturnItemRequest>
{
    public ReturnRequestItemValidator(IList<string> reasons, IList<string> reasonsRequiringComment)
    {
        RuleFor(x => x.ReasonCode)
            .MaximumLength(Length64)
            .Must(x => string.IsNullOrEmpty(x) || reasons.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage(x => $"Reason '{x.ReasonCode}' is not one this store offers.");

        // Two rules, not one chain: When applies to every preceding validator in a chain, so the
        // length check would only run for the reasons that also demand a comment.
        RuleFor(x => x.ReasonComment).MaximumLength(Length1024);

        RuleFor(x => x.ReasonComment)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.ReasonCode) &&
                       reasonsRequiringComment.Contains(x.ReasonCode, StringComparer.OrdinalIgnoreCase))
            .WithMessage("This reason needs a comment.");

        RuleFor(x => x.SerialNumber).MaximumLength(Length128);
    }
}
