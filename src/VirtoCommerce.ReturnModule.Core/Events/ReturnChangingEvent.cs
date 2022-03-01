using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Events
{
    public class ReturnChangingEvent : GenericChangedEntryEvent<Return>
    {
        public ReturnChangingEvent(IEnumerable<GenericChangedEntry<Return>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
