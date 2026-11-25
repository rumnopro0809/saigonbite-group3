using System.Collections.Generic;
using System.Web.Mvc;
using SaigonBite.Application;
using SaigonBite.Infrastructure;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

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

        /// <summary>Trang quản trị đang ở tiếng Anh (cookie sb_lang = en, NFR03).</summary>
        protected bool English { get { return HubText.IsEnglish(Request); } }

        /// <summary>Đưa lỗi của service vào ModelState để view hiển thị.</summary>
        protected void AddErrors<T>(ServiceResult<T> result)
        {
            foreach (var error in result.Errors)
            {
                // Tên trường của service khác tên trong form ở một vài chỗ.
                string key = error.Field == "BrandIds" ? "SelectedBrandIds"
                    : error.Field == "RowVersion" ? string.Empty
                    : (error.Field ?? string.Empty);
                ModelState.AddModelError(key, HubText.Localize(error.Message, English));
            }
        }

        /// <summary>Đổi các lỗi DataAnnotations (tiếng Việt) sang ngôn ngữ đang chọn trước khi vẽ lại form.</summary>
        protected void LocalizeModelState()
        {
            if (!English) return;
            foreach (var entry in ModelState.Values)
            {
                var original = new List<ModelError>(entry.Errors);
                entry.Errors.Clear();
                foreach (var error in original)
                    entry.Errors.Add(string.IsNullOrEmpty(error.ErrorMessage)
                        ? error : new ModelError(HubText.Localize(error.ErrorMessage, true)));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }
}
