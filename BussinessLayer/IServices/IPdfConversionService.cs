using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Dịch vụ convert tài liệu office (DOCX/DOC/PPTX/PPT/…) sang PDF để hiển thị "chuẩn như lúc up"
    /// trong viewer PDF.js. Dựa trên LibreOffice headless (soffice) nếu có trên máy chủ.
    /// </summary>
    public interface IPdfConversionService
    {
        /// <summary>True nếu tìm thấy công cụ convert (LibreOffice/soffice) trên máy.</summary>
        bool IsAvailable { get; }

        /// <summary>Danh sách phần mở rộng (không dấu chấm, chữ thường) có thể convert sang PDF.</summary>
        bool CanConvert(string fileExtension);

        /// <summary>
        /// Convert file nguồn sang PDF, ghi ra thư mục <paramref name="outputDir"/>.
        /// Trả về đường dẫn tuyệt đối file PDF sinh ra, hoặc null nếu không convert được.
        /// </summary>
        Task<string?> ConvertToPdfAsync(string sourceAbsolutePath, string outputDir);
    }
}
