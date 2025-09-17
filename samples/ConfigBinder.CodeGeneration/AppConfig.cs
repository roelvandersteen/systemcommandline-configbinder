using System.ComponentModel.DataAnnotations;

namespace ConfigBinder.CodeGeneration;

public sealed class AppConfig
{
    [Required]
    [Display(Description = "The service endpoint.")]
    public string Endpoint { get; set; } = string.Empty;

    [Display(Description = "Enable diagnostics.")]
    public bool Diagnostics { get; set; } = true; // yields --no-diagnostics

    [Range(1, 10)]
    [Display(Description = "Retry attempts.")]
    public int Retries { get; set; } = 3;
}