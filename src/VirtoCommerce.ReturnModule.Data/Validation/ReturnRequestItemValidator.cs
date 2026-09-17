using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using VirtoCommerce.ReturnModule.Core.Models;
using static VirtoCommerce.Platform.Data.Infrastructure.DbContextBase;

namespace VirtoCommerce.ReturnModule.Data.Validation;

public class ReturnRequestItemValidator : AbstractValidator<CreateReturnItemRequest>
{
    public ReturnRequestItemValidator(IList<string> reasons, IList<string> reasonsRequiringComment, bool requireReason)
    {
        RuleFor(x => x.ReasonCode)
            .MaximumLength(Length64)
            .Must(x => string.IsNullOrEmpty(x) || reasons.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage(x => $"Reason '{x.ReasonCode}' is not one of the return reasons on offer.");

        // Clearing the reason used to defeat the mandatory-comment setting, because both rules below
        // stand down on an empty code. Demanded once the buyer can no longer go back and fill it in.
        RuleFor(x => x.ReasonCode)
            .NotEmpty()
            .When(_ => requireReason)
            .WithMessage("A return reason is required.");

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
