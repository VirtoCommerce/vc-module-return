using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ReturnModule.ExperienceApi.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Queries;

public abstract class ReturnSearchQueryBuilderBase<TQuery, TResult, TItem, TItemGraphType> : SearchQueryBuilder<TQuery, TResult, TItem, TItemGraphType>
    where TQuery : IQuery<TResult>, IExtendableQuery, IHasArguments, ISearchQuery
    where TResult : GenericSearchResult<TItem>
    where TItemGraphType : IGraphType
{
    protected ReturnSearchQueryBuilderBase(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, TQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<IReturnAccessService>().CheckUserStateAsync(context);
    }
}
