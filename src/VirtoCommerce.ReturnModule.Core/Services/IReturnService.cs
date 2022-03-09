using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ReturnModule.Core.Models;

namespace VirtoCommerce.ReturnModule.Core.Services
{
    public interface IReturnService : ICrudService<Return>
    {
        Task SaveChangesAsync(Return[] returns);
    }
}
