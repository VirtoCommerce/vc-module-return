using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Notifications
{
    public abstract class ReturnEmailNotificationBase : EmailNotification
    {
        protected ReturnEmailNotificationBase(string type)
            : base(type)
        {
        }

        public virtual string ReturnId { get; set; }

        public virtual Return Return { get; set; }

        public virtual Member Customer { get; set; }
    }
}
