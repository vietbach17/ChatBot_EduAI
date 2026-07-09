namespace BussinessLayer.DTOs
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MonthlyQuestionLimit { get; set; } // -1 = không giới hạn
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string Features { get; set; } = "[]";
        public int DurationDays { get; set; } = 30;

        public List<string> FeatureList 
        {
            get 
            {
                try 
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Features) ?? new List<string>();
                }
                catch 
                {
                    return new List<string>();
                }
            }
        }
    }
}
