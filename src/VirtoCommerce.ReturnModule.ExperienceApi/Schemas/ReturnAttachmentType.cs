using VirtoCommerce.ReturnModule.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ReturnModule.ExperienceApi.Schemas;

public class ReturnAttachmentType : ExtendableGraphType<ReturnAttachment>
{
    public ReturnAttachmentType()
    {
        Field(x => x.Name, nullable: false);
        Field(x => x.Url, nullable: false);
        Field(x => x.MimeType, nullable: true);
        Field(x => x.Size, nullable: false);
    }
}
