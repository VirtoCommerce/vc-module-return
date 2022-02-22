using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Events
{
    public class ReturnChangedEvent : GenericChangedEntryEvent<Return>
    {
        public ReturnChangedEvent(IEnumerable<GenericChangedEntry<Return>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
