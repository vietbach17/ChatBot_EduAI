using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Convert tài liệu office sang PDF bằng LibreOffice headless (soffice --convert-to pdf).
    /// Tự dò đường dẫn soffice; nếu không tìm thấy thì <see cref="IsAvailable"/> = false và
    /// <see cref="ConvertToPdfAsync"/> trả null (caller sẽ rơi về chế độ xem text).
    /// </summary>
    public class PdfConversionService : IPdfConversionService
    {
        private static readonly string[] ConvertibleExtensions =
            { "docx", "doc", "pptx", "ppt", "odt", "odp", "rtf", "xlsx", "xls" };

        private readonly string? _sofficePath;

        public PdfConversionService()
        {
            _sofficePath = LocateSoffice();
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_sofficePath);

        public bool CanConvert(string fileExtension)
        {
            if (string.IsNullOrWhiteSpace(fileExtension)) return false;
            var ext = fileExtension.TrimStart('.').ToLowerInvariant();
            return ConvertibleExtensions.Contains(ext);
        }

        public async Task<string?> ConvertToPdfAsync(string sourceAbsolutePath, string outputDir)
        {
            if (!IsAvailable || string.IsNullOrEmpty(sourceAbsolutePath) || !File.Exists(sourceAbsolutePath))
                return null;

            try
            {
                Directory.CreateDirectory(outputDir);

                // Profile riêng cho mỗi lần chạy để cho phép convert đồng thời và tránh lỗi
                // "LibreOffice đang chạy". LibreOffice yêu cầu URL dạng file:/// cho -env.
                var profileDir = Path.Combine(Path.GetTempPath(), "lo_profile_" + Guid.NewGuid().ToString("N"));
                var profileUri = new Uri(profileDir).AbsoluteUri;

                var psi = new ProcessStartInfo
                {
                    FileName = _sofficePath!,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                psi.ArgumentList.Add($"-env:UserInstallation={profileUri}");
                psi.ArgumentList.Add("--headless");
                psi.ArgumentList.Add("--norestore");
                psi.ArgumentList.Add("--nolockcheck");
                psi.ArgumentList.Add("--convert-to");
                psi.ArgumentList.Add("pdf");
                psi.ArgumentList.Add("--outdir");
                psi.ArgumentList.Add(outputDir);
                psi.ArgumentList.Add(sourceAbsolutePath);

                using var process = new Process { StartInfo = psi };
                process.Start();

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                // Chờ tối đa 2 phút cho một file
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(2));
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(true); } catch { /* ignore */ }
                    CleanupProfile(profileDir);
                    return null;
                }

                await Task.WhenAll(stdOutTask, stdErrTask);
                CleanupProfile(profileDir);

                if (process.ExitCode != 0)
                    return null;

                // LibreOffice ghi ra "<tên file nguồn không đuôi>.pdf" trong outputDir.
                var expectedPdf = Path.Combine(
                    outputDir,
                    Path.GetFileNameWithoutExtension(sourceAbsolutePath) + ".pdf");

                return File.Exists(expectedPdf) ? expectedPdf : null;
            }
            catch
            {
                return null;
            }
        }

        private static void CleanupProfile(string profileDir)
        {
            try
            {
                if (Directory.Exists(profileDir))
                    Directory.Delete(profileDir, recursive: true);
            }
            catch { /* ignore */ }
        }

        /// <summary>Tìm đường dẫn soffice: biến môi trường → PATH → thư mục cài đặt phổ biến.</summary>
        private static string? LocateSoffice()
        {
            // 1. Biến môi trường tuỳ chỉnh
            var fromEnv = Environment.GetEnvironmentVariable("SOFFICE_PATH");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return fromEnv;

            var isWindows = OperatingSystem.IsWindows();
            var exeNames = isWindows ? new[] { "soffice.exe", "soffice.com" } : new[] { "soffice" };

            // 2. PATH
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var exe in exeNames)
                {
                    try
                    {
                        var candidate = Path.Combine(dir.Trim(), exe);
                        if (File.Exists(candidate)) return candidate;
                    }
                    catch { /* invalid path segment */ }
                }
            }

            // 3. Thư mục cài đặt phổ biến
            var commonPaths = new List<string>();
            if (isWindows)
            {
                var pf = Environment.GetEnvironmentVariable("ProgramFiles");
                var pfx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                foreach (var root in new[] { pf, pfx86 })
                {
                    if (!string.IsNullOrEmpty(root))
                        commonPaths.Add(Path.Combine(root, "LibreOffice", "program", "soffice.exe"));
                }
            }
            else
            {
                commonPaths.AddRange(new[]
                {
                    "/usr/bin/soffice",
                    "/usr/local/bin/soffice",
                    "/usr/lib/libreoffice/program/soffice",
                    "/opt/libreoffice/program/soffice",
                    "/Applications/LibreOffice.app/Contents/MacOS/soffice"
                });
            }

            return commonPaths.FirstOrDefault(File.Exists);
        }
    }
}
