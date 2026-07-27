namespace FETDS.Helpers
{
    public static class StatusBadgeHelper
    {
        public static string CssClass(string status) => status switch
        {
            "Available" => "status-available",
            "Pending" => "status-pending",
            "Reserved" => "status-reserved",
            "Expired" => "status-expired",
            "Approved" => "status-approved",
            "Rejected" => "status-rejected",
            _ => "status-pending"
        };
    }
}