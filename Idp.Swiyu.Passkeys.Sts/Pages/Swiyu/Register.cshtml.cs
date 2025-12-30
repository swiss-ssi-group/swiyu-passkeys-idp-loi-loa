using Idp.Swiyu.Passkeys.Sts.Data;
using Idp.Swiyu.Passkeys.Sts.Data.Migrations;
using Idp.Swiyu.Passkeys.Sts.Models;
using Idp.Swiyu.Passkeys.Sts.SwiyuServices;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Net.Codecrete.QrCodeGenerator;
using System.Text.Json;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Swiyu;

[Authorize]
public class RegisterModel : PageModel
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IHttpClientFactory _clientFactory;
    private readonly VerificationService _verificationService;
    private readonly string? _swiyuOid4vpUrl;
    private readonly UserManager<ApplicationUser> _userManager;

    [BindProperty]
    public string? VerificationId { get; set; }

    [BindProperty]
    public string? QrCodeUrl { get; set; } = string.Empty;

    [BindProperty]
    public byte[] QrCodePng { get; set; } = [];

    public RegisterModel(VerificationService verificationService,
        IConfiguration configuration,
        IHttpClientFactory clientFactory,
        ApplicationDbContext applicationDbContext,
        UserManager<ApplicationUser> userManager)
    {
        _applicationDbContext = applicationDbContext;
        _clientFactory = clientFactory;
        _verificationService = verificationService;
        _swiyuOid4vpUrl = configuration["SwiyuOid4vpUrl"];
        QrCodeUrl = QrCodeUrl.Replace("{OID4VP_URL}", _swiyuOid4vpUrl);
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
        var swiyuVerifiedIdentity = _applicationDbContext.SwiyuIdentity.FirstOrDefault(si => si.UserId == user!.Id);

        if(swiyuVerifiedIdentity != null)
        {
            // User already has a verified Swiyu identity, redirect to complete page
            Response.Redirect("/IdentityAlreadyVerified");
            return;
        }
   
        var presentation = await _verificationService
           .CreateBetaIdVerificationPresentationAsync();

        var verificationResponse = JsonSerializer.Deserialize<CreateVerificationPresentationModel>(presentation);
        // verification_url
        QrCodeUrl = verificationResponse!.verification_url;

        var qrCode = QrCode.EncodeText(verificationResponse!.verification_url, QrCode.Ecc.Quartile);
        QrCodePng = qrCode.ToPng(20, 4, MagickColors.Black, MagickColors.White);

        VerificationId = verificationResponse.id;
    }

    public async Task<IActionResult> OnPost()
    {
        VerificationClaims verificationClaims = null;
        try
        {
            if (VerificationId == null)
            {
                return BadRequest(new { error = "400", error_description = "Missing argument 'VerificationId'" });
            }

            var verificationModel = await RequestSwiyuClaimsAsync(1, VerificationId);

            verificationClaims = _verificationService.GetVerifiedClaims(verificationModel);

            var exists = _applicationDbContext.SwiyuIdentity.FirstOrDefault(c =>
                    c.BirthDate == verificationClaims.BirthDate &&
                    c.BirthPlace == verificationClaims.BirthPlace &&
                    c.GivenName == verificationClaims.GivenName &&
                    c.FamilyName == verificationClaims.FamilyName);

            if (exists != null)
            {
                var user = await _userManager.FindByIdAsync(exists.UserId);

                if (user == null)
                {
                    // This should return a user message with no info what went wrong.
                    throw new ArgumentNullException("error in authentication");
                }

                return Redirect("/IdentityCheckComplete");
            }

        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }

        return Page();
    }

    internal async Task<VerificationManagementModel> RequestSwiyuClaimsAsync(int interval, string verificationId)
    {
        var client = _clientFactory.CreateClient();

        while (true)
        {

            var verificationModel = await _verificationService.GetVerificationStatus(verificationId);

            if (verificationModel != null && verificationModel.state == "SUCCESS")
            {
                return verificationModel;
            }
            else
            {
                await Task.Delay(interval * 1000);
            }
        }
    }
}
