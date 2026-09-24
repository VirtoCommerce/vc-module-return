using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.ReturnModule.Core.Services;

namespace VirtoCommerce.ReturnModule.Data.Services;

public class ReturnBuyerResolver : IReturnBuyerResolver
{
    private readonly IMemberService _memberService;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public ReturnBuyerResolver(IMemberService memberService, Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        _memberService = memberService;
        _userManagerFactory = userManagerFactory;
    }

    public virtual async Task<ReturnBuyer> GetBuyerAsync(string customerId)
    {
        if (string.IsNullOrEmpty(customerId))
        {
            return null;
        }

        // A storefront return carries the order's CustomerId, which is the buyer's user id, so the
        // login is looked up first; a member id is the fallback for returns created elsewhere.
        ApplicationUser user;

        using (var userManager = _userManagerFactory())
        {
            user = await userManager.FindByIdAsync(customerId);
        }

        var member = await _memberService.GetByIdAsync(user?.MemberId ?? customerId, ResponseGroup);
        var email = member?.Emails?.FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? user?.Email;

        if (member == null && string.IsNullOrEmpty(email))
        {
            return null;
        }

        var result = AbstractTypeFactory<ReturnBuyer>.TryCreateInstance();

        result.Member = member;
        result.Email = email;

        return result;
    }

    /// <summary>
    /// The templates read the name, which every group carries, and the handlers read the emails.
    /// </summary>
    protected virtual string ResponseGroup => MemberResponseGroup.WithEmails.ToString();
}
