using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreDisplayTimingToQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Quizzes""
ADD COLUMN IF NOT EXISTS ""ScoreDisplayTiming"" character varying(20) NOT NULL DEFAULT '';");

            migrationBuilder.Sql(@"
INSERT INTO ""QuestionBanks"" (""Id"", ""Content"", ""CorrectAnswer"", ""CreatedAt"", ""Difficulty"", ""IsAIGenerated"", ""IsDeleted"", ""LecturerId"", ""OptionsJson"", ""QuestionType"", ""SubjectId"", ""Tags"")
VALUES
(4, 'Nếu trình duyệt của client không hỗ trợ WebSockets, SignalR trong ASP.NET Core sẽ tự động fallback (lùi về) sử dụng các cơ chế nào?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""Chỉ Long Polling"",""Server-Sent Events và Long Polling"",""gRPC và TCP"",""Không có cơ chế fallback""]', 'MultipleChoice', 1, 'SignalR,WebSockets'),
(5, 'Sự khác biệt cốt lõi giữa giao thức TCP và UDP là gì?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Easy', FALSE, FALSE, 2, '[""TCP nhanh hơn UDP gấp nhiều lần"",""TCP đảm bảo truyền tin cậy (không mất gói tin), UDP thì không"",""UDP mặc định mã hóa dữ liệu, TCP thì không"",""Cả hai đều không yêu cầu thiết lập kết nối (connectionless)""]', 'MultipleChoice', 1, 'TCP,UDP,Protocols'),
(6, 'Mã trạng thái HTTP (Status Code) nào báo hiệu rằng người dùng chưa đăng nhập hoặc cung cấp Token không hợp lệ?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Easy', FALSE, FALSE, 2, '[""400 Bad Request"",""403 Forbidden"",""401 Unauthorized"",""404 Not Found""]', 'MultipleChoice', 1, 'HTTP,StatusCode,Security'),
(7, 'CORS (Cross-Origin Resource Sharing) trong ASP.NET Core API được sử dụng để giải quyết bài toán gì?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""Bảo vệ CSDL khỏi SQL Injection"",""Cho phép hoặc chặn các Request gọi từ các Domain/Port khác"",""Mã hóa mật khẩu người dùng trước khi lưu"",""Tối ưu hóa tốc độ tải trang (Caching)""]', 'MultipleChoice', 1, 'CORS,Security,API'),
(8, 'Trong kiến trúc gRPC của .NET, định dạng nào được sử dụng để tuần tự hóa (serialize) dữ liệu truyền đi thay cho JSON?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""XML"",""BSON"",""Protocol Buffers (Protobuf)"",""MessagePack""]', 'MultipleChoice', 1, 'gRPC,Serialization'),
(9, 'Phương thức HTTP nào được thiết kế chuẩn để cập nhật MỘT PHẦN dữ liệu của tài nguyên thay vì cập nhật toàn bộ?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Easy', FALSE, FALSE, 2, '[""PUT"",""POST"",""PATCH"",""UPDATE""]', 'MultipleChoice', 1, 'HTTP,REST'),
(10, 'Để ánh xạ (map) một SignalR Hub vào Endpoint Routing trong file Program.cs của .NET 8, ta dùng phương thức nào?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""app.MapControllers()"",""app.AddSignalR()"",""app.MapHub<THub>()"",""app.UseWebSockets()""]', 'MultipleChoice', 1, 'SignalR,Routing'),
(11, 'YARP (Yet Another Reverse Proxy) của Microsoft thường được dùng trong trường hợp nào?', 'A', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Easy', FALSE, FALSE, 2, '[""Làm API Gateway điều hướng request cho các Microservices"",""Thay thế Entity Framework Core"",""Quản lý background jobs như Hangfire"",""Tạo giao diện người dùng thời gian thực""]', 'MultipleChoice', 1, 'YARP,Microservices,Proxy'),
(12, 'Bản chất của kết nối WebSocket là gì?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""Giao tiếp 1 chiều từ Client lên Server"",""Kết nối 2 chiều (Bi-directional) toàn thời gian qua 1 TCP Socket"",""Request-Response liên tục (Polling)"",""Chỉ mở kết nối khi Server có dữ liệu mới""]', 'MultipleChoice', 1, 'WebSockets,RealTime'),
(13, 'Khi cấu hình Middleware trong ASP.NET Core, thứ tự khai báo (chẳng hạn UseAuthentication trước UseAuthorization) có quan trọng không?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Easy', FALSE, FALSE, 2, '[""Không quan trọng, .NET sẽ tự động sắp xếp lại"",""Chỉ quan trọng khi cấu hình CORS"",""Có, thứ tự cực kỳ quan trọng vì Request đi qua theo thứ tự khai báo"",""Không, tất cả middleware chạy song song cùng lúc""]', 'MultipleChoice', 1, 'Middleware,ASP.NETCore'),
(14, 'Namespace nào trong .NET cung cấp các lớp (classes) làm việc trực tiếp ở tầng thấp với TCP/UDP như TcpListener, UdpClient?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Easy', FALSE, FALSE, 2, '[""System.Net.Http"",""Microsoft.AspNetCore.Http"",""System.Net.Sockets"",""System.IO.Ports""]', 'MultipleChoice', 1, 'Sockets,Networking'),
(15, 'Trong SignalR, làm thế nào để Hub Server gửi một tin nhắn trực tiếp đến MỘT người dùng cụ thể đã được xác thực (đăng nhập)?', 'D', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Hard', FALSE, FALSE, 2, '[""Clients.All.SendAsync()"",""Clients.Others.SendAsync()"",""Clients.Group(userId).SendAsync()"",""Clients.User(userId).SendAsync()""]', 'MultipleChoice', 1, 'SignalR,Targeting'),
(16, 'Công nghệ SSE (được SignalR dùng làm fallback) là viết tắt của từ gì?', 'A', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""Server-Sent Events"",""Socket Secure Engine"",""SignalR Streaming Extension"",""Synchronous Server Endpoint""]', 'MultipleChoice', 1, 'SSE,RealTime'),
(17, 'Trong ASP.NET Core, JWT (JSON Web Token) được ứng dụng chủ yếu vào mục đích gì trong môi trường mạng phân tán?', 'C', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""Lưu trữ Session trực tiếp trên RAM của Server"",""Mã hóa dữ liệu trong Database"",""Xác thực không trạng thái (Stateless Authentication)"",""Chống lại tấn công DDoS""]', 'MultipleChoice', 1, 'JWT,Security,Stateless'),
(18, 'Thuộc tính ''Idempotent'' (Lũy đẳng) trong thiết kế REST API có nghĩa là gì?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Hard', FALSE, FALSE, 2, '[""API phản hồi cực kỳ nhanh"",""Gọi API 1 lần hay nhiều lần liên tiếp đều sinh ra cùng một trạng thái hệ thống"",""API chỉ cho phép giao tiếp mã hóa SSL"",""API bắt buộc phải dùng JSON""]', 'MultipleChoice', 1, 'REST,API,Architecture'),
(19, 'Việc thiết lập các Limits trên Kestrel Web Server (như MaxRequestBodySize, MaxConcurrentConnections) mang lại lợi ích chính gì?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Medium', FALSE, FALSE, 2, '[""Tránh lãng phí bộ nhớ lưu trữ CSDL"",""Bảo vệ Server khỏi quá tải hoặc các cuộc tấn công gửi dữ liệu lớn"",""Tự động Scale-out server lên Kubernetes"",""Tăng tốc độ mã hóa HTTPS""]', 'MultipleChoice', 1, 'Kestrel,Security,Performance'),
(20, 'Trong .NET, để triển khai Rate Limiting (Giới hạn số lượng request từ 1 Client) một cách native từ .NET 7/8, ta dùng tính năng nào?', 'B', TIMESTAMPTZ '2026-07-08T00:00:00Z', 'Hard', FALSE, FALSE, 2, '[""Thư viện bên thứ ba như AspNetCoreRateLimit"",""Rate Limiting Middleware tích hợp sẵn trong Microsoft.AspNetCore.RateLimiting"",""Dùng Hangfire để đếm request"",""Dùng SignalR""]', 'MultipleChoice', 1, 'RateLimiting,Middleware,Security')
ON CONFLICT (""Id"") DO NOTHING;

SELECT setval(
    pg_get_serial_sequence('""QuestionBanks""', 'Id'),
    COALESCE((SELECT MAX(""Id"") FROM ""QuestionBanks""), 0) + 1,
    false
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "QuestionBanks",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DropColumn(
                name: "ScoreDisplayTiming",
                table: "Quizzes");
        }
    }
}
