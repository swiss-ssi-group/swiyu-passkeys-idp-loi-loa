using Idp.Swiyu.Passkeys.Sts.SwiyuServices;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Net.Codecrete.QrCodeGenerator;
using System.Text.Json;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Swiyu;

[Authorize]
public class RegisterModel : PageModel
{
    private readonly VerificationService _verificationService;
    private readonly string? _swiyuOid4vpUrl;

    [BindProperty]
    public string? VerificationId { get; set; }

    [BindProperty]
    public string? QrCodeUrl { get; set; } = string.Empty;

    [BindProperty]
    public byte[] QrCodePng { get; set; } = [];

    public RegisterModel(VerificationService verificationService,
        IConfiguration configuration)
    {
        _verificationService = verificationService;
        _swiyuOid4vpUrl = configuration["SwiyuOid4vpUrl"];
        QrCodeUrl = QrCodeUrl.Replace("{OID4VP_URL}", _swiyuOid4vpUrl);
    }

    public void OnGet()
    {
        // default HTTP GET is required
    }

    public async Task OnPostAsync()
    {
        var presentation = await _verificationService
            .CreateBetaIdVerificationPresentationAsync();

        var verificationResponse = JsonSerializer.Deserialize<CreateVerificationPresentationModel>(presentation);
        // verification_url
        QrCodeUrl = verificationResponse!.verification_url;

        var qrCode = QrCode.EncodeText(verificationResponse!.verification_url, QrCode.Ecc.Quartile);
        QrCodePng = qrCode.ToPng(20, 4, MagickColors.Black, MagickColors.White);

        VerificationId = verificationResponse.id;
    }
}
