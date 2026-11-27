using System.Collections.Generic;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Menu
{
    /// <summary>
    /// Persistence abstraction for UC02 Manage Menu and Brands (DIP / NFR02).
    /// Implemented by EF6 in SaigonBite.Infrastructure and faked in MSTest unit tests.
    /// </summary>
    public interface IMenuRepository
    {
        IList<Hub> GetActiveHubs();

        IList<Brand> GetBrands(bool includeDeleted);
        Brand GetBrandById(int id);
        Brand AddBrand(Brand brand, IEnumerable<int> hubIds);
        void UpdateBrand(Brand brand, IEnumerable<int> hubIds);
        void SoftDeleteBrand(int id);

        IList<MenuItem> GetMenuItems(MenuFilter filter);
        MenuItem GetMenuItemById(int id);
        IList<MenuItem> GetCustomerMenuByHub(int hubId);
        bool IsItemOrderableAtHub(int hubId, int menuItemId);

        MenuItem AddMenuItem(MenuItem item);
        void UpdateMenuItem(MenuItem item);
        void SoftDeleteMenuItem(int id);
    }
}
