using System.Collections.Generic;

namespace SaigonBite.Application
{
    /// <summary>Kết quả chung của một thao tác service. Controller dựa vào đây để chọn mã HTTP hoặc trang hiển thị.</summary>
    public enum ResultStatus
    {
        /// <summary>Thành công.</summary>
        Ok,
        /// <summary>Đã tạo mới thành công.</summary>
        Created,
        /// <summary>Không tìm thấy dữ liệu.</summary>
        NotFound,
        /// <summary>Dữ liệu gửi lên không hợp lệ.</summary>
        Invalid,
        /// <summary>Có người khác vừa sửa dữ liệu này (rowversion không khớp).</summary>
        Conflict
    }

    /// <summary>Một lỗi kiểm tra dữ liệu, gắn với tên trường (có thể để trống nếu là lỗi chung).</summary>
    public sealed class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }

        public ValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }
    }

    /// <summary>Kết quả thao tác kèm dữ liệu trả về (nếu có) và danh sách lỗi (nếu có).</summary>
    public sealed class ServiceResult<T>
    {
        public ResultStatus Status { get; private set; }
        public T Value { get; private set; }
        public IList<ValidationError> Errors { get; private set; }

        private ServiceResult() { Errors = new List<ValidationError>(); }

        public bool IsSuccess { get { return Status == ResultStatus.Ok || Status == ResultStatus.Created; } }

        public static ServiceResult<T> Ok(T value)
        {
            return new ServiceResult<T> { Status = ResultStatus.Ok, Value = value };
        }

        public static ServiceResult<T> Created(T value)
        {
            return new ServiceResult<T> { Status = ResultStatus.Created, Value = value };
        }

        public static ServiceResult<T> NotFound(string message)
        {
            var r = new ServiceResult<T> { Status = ResultStatus.NotFound };
            r.Errors.Add(new ValidationError(null, message));
            return r;
        }

        public static ServiceResult<T> Invalid(IList<ValidationError> errors)
        {
            return new ServiceResult<T> { Status = ResultStatus.Invalid, Errors = errors };
        }

        public static ServiceResult<T> Conflict(string message)
        {
            var r = new ServiceResult<T> { Status = ResultStatus.Conflict };
            r.Errors.Add(new ValidationError(null, message));
            return r;
        }
    }
}
