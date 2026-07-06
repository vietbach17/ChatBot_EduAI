using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BussinessLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromName;

        public EmailService()
        {
            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            _smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty;
            _smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? string.Empty;
            _fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "EduManager";
        }

        public async Task SendAccountCreatedEmailAsync(string toEmail, string username, string password, string role)
        {
            var roleLabel = role switch
            {
                "Admin"    => "Quản trị viên",
                "Lecturer" => "Giảng viên",
                _          => "Sinh viên"
            };

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _smtpUser));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "[EduManager] Tài khoản của bạn đã được tạo";

            message.Body = new TextPart("html")
            {
                Text = $"""
                <!DOCTYPE html>
                <html lang="vi">
                <head><meta charset="UTF-8"></head>
                <body style="font-family: 'Segoe UI', Arial, sans-serif; background:#f8f8f8; margin:0; padding:0;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8f8f8; padding: 30px 0;">
                    <tr><td align="center">
                      <table width="500" cellpadding="0" cellspacing="0"
                             style="background:#fff; border:3px solid #000; box-shadow: 6px 6px 0px #000;">
                        <!-- Header -->
                        <tr>
                          <td style="background:#eb2f64; padding:25px 30px; border-bottom:3px solid #000;">
                            <h1 style="margin:0; color:#fff; font-size:22px; font-weight:900; letter-spacing:1px;">
                              🎓 EduManager
                            </h1>
                          </td>
                        </tr>
                        <!-- Body -->
                        <tr>
                          <td style="padding:30px;">
                            <h2 style="margin:0 0 10px; color:#000; font-size:20px; font-weight:900;">
                              Tài khoản của bạn đã được tạo! 🎉
                            </h2>
                            <p style="margin:0 0 25px; color:#555; font-size:14px; font-weight:600;">
                              Quản trị viên vừa tạo tài khoản cho bạn trên hệ thống EduManager.
                              Dưới đây là thông tin đăng nhập:
                            </p>

                            <!-- Thông tin tài khoản -->
                            <table width="100%" cellpadding="0" cellspacing="0"
                                   style="border:2px solid #000; margin-bottom:25px;">
                              <tr style="border-bottom:2px solid #000;">
                                <td style="padding:12px 15px; background:#f4cf45; font-weight:900;
                                           font-size:13px; border-right:2px solid #000; width:40%;">
                                  👤 Tên đăng nhập
                                </td>
                                <td style="padding:12px 15px; font-weight:800; font-size:14px; color:#000;">
                                  {username}
                                </td>
                              </tr>
                              <tr style="border-bottom:2px solid #000;">
                                <td style="padding:12px 15px; background:#f4cf45; font-weight:900;
                                           font-size:13px; border-right:2px solid #000;">
                                  🔑 Mật khẩu
                                </td>
                                <td style="padding:12px 15px; font-weight:800; font-size:14px;
                                           font-family:monospace; color:#eb2f64; letter-spacing:1px;">
                                  {password}
                                </td>
                              </tr>
                              <tr>
                                <td style="padding:12px 15px; background:#f4cf45; font-weight:900;
                                           font-size:13px; border-right:2px solid #000;">
                                  🏷️ Vai trò
                                </td>
                                <td style="padding:12px 15px; font-weight:800; font-size:14px; color:#000;">
                                  {roleLabel}
                                </td>
                              </tr>
                            </table>

                            <p style="margin:0 0 20px; color:#e53935; font-size:13px; font-weight:700;
                                      background:#fff3cd; border:2px solid #f4cf45; padding:10px;">
                              ⚠️ Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu để bảo mật tài khoản.
                            </p>

                            <p style="margin:0; color:#555; font-size:13px; font-weight:600;">
                              Nếu bạn không yêu cầu tài khoản này, vui lòng liên hệ quản trị viên.
                            </p>
                          </td>
                        </tr>
                        <!-- Footer -->
                        <tr>
                          <td style="padding:15px 30px; border-top:3px solid #000;
                                     background:#f8f8f8; text-align:center;">
                            <p style="margin:0; font-size:12px; color:#888; font-weight:600;">
                              © EduManager — Email này được gửi tự động, vui lòng không trả lời.
                            </p>
                          </td>
                        </tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        public async Task SendBroadcastEmailAsync(System.Collections.Generic.IEnumerable<string> toEmails, string subject, string content)
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);

            foreach (var email in toEmails)
            {
                if (string.IsNullOrWhiteSpace(email)) continue;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _smtpUser));
                message.To.Add(MailboxAddress.Parse(email));
                message.Subject = $"[EduManager] {subject}";

                message.Body = new TextPart("html")
                {
                    Text = $"""
                    <!DOCTYPE html>
                    <html lang="vi">
                    <head><meta charset="UTF-8"></head>
                    <body style="font-family: 'Segoe UI', Arial, sans-serif; background:#f8f8f8; margin:0; padding:0;">
                      <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8f8f8; padding: 30px 0;">
                        <tr><td align="center">
                          <table width="500" cellpadding="0" cellspacing="0"
                                 style="background:#fff; border:3px solid #000; box-shadow: 6px 6px 0px #000;">
                            <!-- Header -->
                            <tr>
                              <td style="background:#2b6cb0; padding:25px 30px; border-bottom:3px solid #000;">
                                <h1 style="margin:0; color:#fff; font-size:22px; font-weight:900; letter-spacing:1px;">
                                  📢 THÔNG BÁO TỪ HỆ THỐNG
                                </h1>
                              </td>
                            </tr>
                            <!-- Body -->
                            <tr>
                              <td style="padding:30px; color:#333; font-size:15px; line-height:1.6;">
                                {content.Replace("\n", "<br/>")}
                              </td>
                            </tr>
                            <!-- Footer -->
                            <tr>
                              <td style="padding:15px 30px; border-top:3px solid #000;
                                         background:#f8f8f8; text-align:center;">
                                <p style="margin:0; font-size:12px; color:#888; font-weight:600;">
                                  © EduManager — Hệ thống quản lý học tập.
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td></tr>
                      </table>
                    </body>
                    </html>
                    """
                };

                try
                {
                    await client.SendAsync(message);
                }
                catch (Exception)
                {
                    // Log error if needed, but continue with next email
                }
            }

            await client.DisconnectAsync(true);
        }
    }
}
