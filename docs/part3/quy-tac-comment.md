# Quy tắc comment (Part 3)

Trong tài liệu của nhóm chưa có quy tắc comment. File này là quy tắc **đơn giản** do UC03 đề xuất, cả nhóm có thể dùng chung. Nếu giảng viên hoặc nhóm có quy tắc khác thì sửa file này.

1. **Viết tiếng Việt có dấu**, câu ngắn, dùng từ dễ hiểu. Tên biến, tên hàm, tên lớp vẫn viết tiếng Anh.
2. **Mỗi lớp và mỗi hàm public** có một đoạn `/// <summary>` dài 1–2 câu, nói *nó làm gì* (không giải thích từng dòng).
3. **Comment trong hàm** chỉ để nói *vì sao* làm vậy, hoặc nhắc một quy tắc nghiệp vụ. Không lặp lại điều mà code đã nói rõ.
4. **Ghi mã yêu cầu** khi có liên quan, ví dụ `// BR8: tải từ 90% trở lên thì từ chối đơn mới.`
5. **Phần làm tạm** phải ghi rõ: `// TẠM (UC01): ...` và nói khi nào sẽ thay thế.
6. **Không để code bị tắt bằng comment** (xóa đi, đã có Git giữ lịch sử). Không viết `TODO` mà không ghi UC hoặc người phụ trách.
7. **Sửa code thì sửa comment.** Comment sai còn tệ hơn không có comment.
