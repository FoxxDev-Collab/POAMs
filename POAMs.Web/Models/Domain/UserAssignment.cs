namespace POAMs.Web.Models.Domain;

public class UserAssignment
{
    public int Id { get; set; }

    public int POAMId { get; set; }
    public POAM POAM { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public AssignmentType AssignmentType { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public int? AssignedById { get; set; }
    public User? AssignedBy { get; set; }
}
