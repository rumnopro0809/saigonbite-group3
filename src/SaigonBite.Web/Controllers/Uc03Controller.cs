using System.Web.Mvc;
using SaigonBite.Application;
using SaigonBite.Infrastructure;
using SaigonBite.Web.Security;

namespace SaigonBite.Web.Controllers
{
    /// <summary>
    /// Lớp cha của các controller MVC trong UC03. Giữ một DbContext cho mỗi request và đóng nó khi xong.
    /// Mọi trang đều chỉ dành cho Admin.
    /// </summary>
    [AdminOnly]
    public abstract class Uc03Controller : Controller
    {
        protected readonly AppDbContext Db = new AppDbContext();

        /// <summary>Đưa lỗi của service vào ModelState để view hiển thị.</summary>
        protected void AddErrors<T>(ServiceResult<T> result)
        {
            foreach (var error in result.Errors)
            {
                // Tên trường của service khác tên trong form ở một vài chỗ.
                string key = error.Field == "BrandIds" ? "SelectedBrandIds"
                    : error.Field == "RowVersion" ? string.Empty
                    : (error.Field ?? string.Empty);
                ModelState.AddModelError(key, error.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }
}
