using System.Collections.Generic;
using VirtoCommerce.FileExperienceApi.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Models;

public class ReturnAttachmentChanges
{
    public IList<File> Claimed { get; set; } = [];

    public IList<File> Released { get; set; } = [];
}
