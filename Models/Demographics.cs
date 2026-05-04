namespace sample_webapp_azure.Models
{
    public class Demographics
    {
        public int PersonId { get; set; }

        public string Name { get; set; }

        public string? Address { get; set; }


        public DateOnly DateOfBirth { get; set; }


        public string AndSoOn { get; set; }
    }
}
