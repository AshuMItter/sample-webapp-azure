namespace sample_webapp_azure.Models
{
    public class Demographics
    {
        public int PersonId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Occupation { get; set; }
        public decimal AnnualIncome { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } // "Pending", "Processed", "Flagged"
    }
}
