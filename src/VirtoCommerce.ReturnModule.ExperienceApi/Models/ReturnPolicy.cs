using System.Collections.Generic;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Models;

public class ReturnPolicy
{
    public bool IsEnabled { get; set; }

    public int WindowDays { get; set; }

    public IList<string> AllowedOrderStatuses { get; set; } = [];
}
