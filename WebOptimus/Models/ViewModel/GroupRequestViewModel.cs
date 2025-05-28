namespace WebOptimus.Models.ViewModel
{
    public class GroupRequestViewModel
    {
        public int RequestId { get; set; } // ID of the join request
        public string GroupName { get; set; } // Name of the group
        public string Status { get; set; } // Status of the request (e.g., Pending, Approved, Declined)
        public DateTime RequestDate { get; set; }
    }
    public class GroupOverviewViewModel
    {
        public List<GroupViewModel> MyGroups { get; set; }
        public List<PendingRequestViewModel> PendingRequests { get; set; }
    }
    public class MyGroupViewModel
    {
        public List<GroupViewModel> MyGroups { get; set; }
        public List<PendingRequestViewModel> PendingRequests { get; set; }
    }
    public class GroupViewModel
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public int TotalMembers { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class PendingRequestViewModel
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
    }
    public class JoinRequestViewModel
    {
        public int RequestId { get; set; } // ID of the GroupMember join request
        public string GroupName { get; set; } // Name of the group being requested
        public DateTime RequestDate { get; set; } // Date the request was made
        public string Status { get; set; } // Status of the request (Pending, Approved, Declined)
    }

}
