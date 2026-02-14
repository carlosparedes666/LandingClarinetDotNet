using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LandingClarinetDotNet.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [BindProperty]
    public ContactFormModel ContactForm { get; set; } = new();

    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Por favor completa correctamente todos los campos.";
            return Page();
        }

        try
        {
            await SendContactEmailAsync();
            StatusMessage = "Gracias. Tu solicitud fue enviada correctamente a ventas@clarinet.com.mx.";
            ModelState.Clear();
            ContactForm = new ContactFormModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible enviar la solicitud de contacto.");
            StatusMessage = "No fue posible enviar la solicitud en este momento. Intenta nuevamente más tarde.";
        }

        return Page();
    }

    private async Task SendContactEmailAsync()
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpPort = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var smtpUser = _configuration["Smtp:User"];
        var smtpPassword = _configuration["Smtp:Password"];
        var smtpFrom = _configuration["Smtp:From"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpFrom))
        {
            throw new InvalidOperationException("Falta configuración SMTP. Define Smtp:Host y Smtp:From en appsettings o variables de entorno.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(smtpFrom),
            Subject = "Nueva solicitud desde landing ClariNet",
            Body = BuildEmailBody(),
            IsBodyHtml = false
        };

        message.To.Add("ventas@clarinet.com.mx");
        message.ReplyToList.Add(new MailAddress(ContactForm.Email));

        using var smtp = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(smtpUser))
        {
            smtp.Credentials = new NetworkCredential(smtpUser, smtpPassword);
        }

        await smtp.SendMailAsync(message);
    }

    private string BuildEmailBody()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Nueva solicitud de contacto desde la landing de ClariNet");
        builder.AppendLine();
        builder.AppendLine($"Nombre: {ContactForm.Name}");
        builder.AppendLine($"Empresa: {ContactForm.Company}");
        builder.AppendLine($"Correo: {ContactForm.Email}");
        builder.AppendLine($"Fecha UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

        return builder.ToString();
    }
}

public class ContactFormModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La empresa es obligatoria.")]
    [StringLength(120)]
    public string Company { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;
}
