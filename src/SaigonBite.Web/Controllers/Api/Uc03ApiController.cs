using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using SaigonBite.Application;
using SaigonBite.Infrastructure;
using SaigonBite.Web.Security;

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
            return Content(HttpStatusCode.BadRequest, new ApiError { Message = message, Errors = new List<ValidationError>() });
        }

        private static ApiError ToError<T>(string message, ServiceResult<T> result)
        {
            return new ApiError { Message = message, Errors = result.Errors.ToList() };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }
}
