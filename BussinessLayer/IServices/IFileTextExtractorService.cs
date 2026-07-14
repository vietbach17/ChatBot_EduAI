namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Trích xuất văn bản từ file (PDF/DOCX/PPTX).
    /// </summary>
    public interface IFileTextExtractorService
    {
        string ExtractText(string filePath);
    }
}
