namespace FETDS.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalDonors { get; set; }
        public int TotalReceivers { get; set; }
        public int TotalProductsListed { get; set; }
        public int TotalDonationsCompleted { get; set; } // Status == "Reserved" or a future "Donated" status
        public int TotalPendingRequests { get; set; }
        public int TotalExpiredProducts { get; set; }
        public List<(string Category, int Count)> ProductsByCategory { get; set; } = new();
    }
}