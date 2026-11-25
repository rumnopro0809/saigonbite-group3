using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SaigonBite.Application;
using SaigonBite.Infrastructure;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers.Api
{
    /// <summary>Nội dung lỗi mà API trả về, cùng một dạng cho mọi lỗi (400, 404, 409).</summary>
    public sealed class ApiError
    {
        public string Message { get; set; }
        public IList<ValidationError> Errors { get; set; }
    }

    /// <summary>
    /// Lớp cha của các API controller UC03. Giữ một DbContext cho mỗi request
    /// và đổi kết quả của service thành mã HTTP theo chuẩn REST.
    /// </summary>
    [AdminOnlyApi]
    public abstract class Uc03ApiController : ApiController
    {
        protected readonly AppDbContext Db = new AppDbContext();

        /// <summary>Người gọi đang dùng tiếng Anh (cookie sb_lang = en). Thông báo lỗi được dịch theo đó (NFR03).</summary>
        private bool English
        {
            get
            {
                var header = Request.Headers.GetCookies("sb_lang").FirstOrDefault();
                var cookie = header == null ? null : header.Cookies.FirstOrDefault(c => c.Name == "sb_lang");
                return cookie != null && cookie.Value == "en";
            }
        }

        /// <summary>
        /// Đổi ServiceResult thành phản hồi HTTP:
        /// Ok → 200, NotFound → 404, Invalid → 400, Conflict → 409 (rowversion không khớp).
        /// </summary>
        protected IHttpActionResult FromResult<T>(ServiceResult<T> result)
        {
            switch (result.Status)
            {
                case ResultStatus.Ok:
                    return Ok(result.Value);
                case ResultStatus.NotFound:
                    return Content(HttpStatusCode.NotFound, ToError("Không tìm thấy dữ liệu.", result));
                case ResultStatus.Conflict:
                    return Content(HttpStatusCode.Conflict, ToError(result.Errors[0].Message, result));
                default:
                    return Content(HttpStatusCode.BadRequest, ToError("Dữ liệu không hợp lệ.", result));
            }
        }

        protected IHttpActionResult BadRequestMessage(string message)
        {
            return Content(HttpStatusCode.BadRequest,
                new ApiError { Message = HubText.Localize(message, English), Errors = new List<ValidationError>() });
        }

        private ApiError ToError<T>(string message, ServiceResult<T> result)
        {
            bool en = English;
            return new ApiError
            {
                Message = HubText.Localize(message, en),
                Errors = result.Errors.Select(e => new ValidationError(e.Field, HubText.Localize(e.Message, en))).ToList()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }
}
