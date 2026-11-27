using System.Collections.Generic;
using System.Web.Mvc;
using SaigonBite.Application.Hubs;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>
    /// Lớp cha của các trang quản trị UC03 (bếp, cờ mưa, R2, gán shipper). Dùng DbContext, người dùng hiện tại
    /// và ngôn ngữ của BaseController chung; mọi trang chỉ dành cho Admin (NFR04).
    /// </summary>
    [AdminOnly]
    public abstract class Uc03Controller : BaseController
    {
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

        /// <summary>Trả kết quả của service dưới dạng JSON cho các nút trên trang (uc03.js), kèm mã HTTP phù hợp.</summary>
        protected ActionResult JsonResultOf<T>(ServiceResult<T> result, System.Func<T, object> body)
        {
            switch (result.Status)
            {
                case ResultStatus.Ok:
                case ResultStatus.Created:
                    return Json(body(result.Value));
                case ResultStatus.NotFound:
                    Response.StatusCode = 404;
                    break;
                case ResultStatus.Conflict:
                    Response.StatusCode = 409;
                    break;
                default:
                    Response.StatusCode = 400;
                    break;
            }
            Response.TrySkipIisCustomErrors = true;
            var errors = new List<object>();
            foreach (var e in result.Errors) errors.Add(new { field = e.Field, message = HubText.Localize(e.Message, English) });
            return Json(new { errors = errors });
        }
    }
}
