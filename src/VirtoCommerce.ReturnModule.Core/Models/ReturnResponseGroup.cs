using System;

namespace VirtoCommerce.ReturnModule.Core.Models
{
    [Flags]
    public enum ReturnResponseGroup
    {
        None = 0,

        WithOrders = 1,

        WithLineItems = 2
    }
}
