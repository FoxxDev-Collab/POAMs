using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

/// <summary>
/// Control Correlation Identifier - maps specific requirements to NIST controls
/// </summary>
public class CCI
{
    public int Id { get; set; }

    public int NISTControlId { get; set; }
    public NISTControl NISTControl { get; set; } = null!;

    /// <summary>
    /// CCI number (e.g., "CCI-000002")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string CCINumber { get; set; } = string.Empty;

    /// <summary>
    /// CCI definition/requirement text
    /// </summary>
    [Required]
    public string Definition { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
