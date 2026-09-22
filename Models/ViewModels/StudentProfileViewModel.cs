using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CampusConnect.Models.ViewModels;

public class StudentProfileViewModel
{
    public Guid ProfileId { get; set; }
    
    [Required, MaxLength(50), Display(Name = "Roll Number")]
    public string RollNumber { get; set; } = string.Empty;
    
    [Required, Display(Name = "Department")]
    public int DepartmentId { get; set; }
    
    [Required, Display(Name = "Batch Year")]
    public int BatchYear { get; set; }
    
    public string? Bio { get; set; }
    
    [MaxLength(500), Display(Name = "GitHub URL"), Url]
    public string? GitHubUrl { get; set; }
    
    [MaxLength(500), Display(Name = "LinkedIn URL"), Url]
    public string? LinkedInUrl { get; set; }
    
    public string? ExistingResumeUrl { get; set; }
    
    [Display(Name = "Upload Resume (PDF/DOCX)")]
    public IFormFile? ResumeFile { get; set; }
}
