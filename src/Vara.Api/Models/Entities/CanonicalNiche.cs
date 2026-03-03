using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class CanonicalNiche
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Alternate phrasings used for fuzzy matching, stored as a PostgreSQL text[] array.</summary>
    public string[] Aliases { get; set; } = [];

    public bool IsActive { get; set; } = true;
}
