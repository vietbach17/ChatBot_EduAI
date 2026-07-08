namespace PresentationLayer.ViewModels.Admin
{
    public class SubjectCreateViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? LecturerId { get; set; }
    }

    public class SubjectUpdateViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? LecturerId { get; set; }
    }
}
