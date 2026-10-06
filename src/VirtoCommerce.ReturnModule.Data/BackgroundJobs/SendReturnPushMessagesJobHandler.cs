using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.ReturnModule.Data.Handlers;

namespace VirtoCommerce.ReturnModule.Data.BackgroundJobs;

// Creates the buyer's push message for a return status change, off the save path. Registered only when the
// optional VirtoCommerce.PushMessages is installed, like the event handler it calls.
public class SendReturnPushMessagesJobHandler(SendPushMessagesReturnStatusChangedEventHandler eventHandler)
    : IBackgroundJobHandler<ReturnNotificationJobArgument>
{
    public virtual Task Execute(ReturnNotificationJobArgument payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return eventHandler.SendPushMessagesAsync([payload]);
    }
}
