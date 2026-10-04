using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.ReturnModule.Data.Handlers;

namespace VirtoCommerce.ReturnModule.Data.BackgroundJobs;

// Sends the buyer's email for a return status change, off the save path.
public class SendReturnNotificationsJobHandler(SendNotificationsReturnStatusChangedEventHandler eventHandler)
    : IBackgroundJobHandler<ReturnNotificationJobArgument>
{
    public virtual Task Execute(ReturnNotificationJobArgument payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return eventHandler.SendNotificationsAsync([payload]);
    }
}
