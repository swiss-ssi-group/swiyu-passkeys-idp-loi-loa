using Duende.IdentityModel;
using Idp.Swiyu.Passkeys.Sts.Data;
using Idp.Swiyu.Passkeys.Sts.SwiyuServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Security.Claims;
using Idp.Swiyu.Passkeys.Sts.Models;

namespace Idp.Swiyu.Passkeys.Sts.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RegisterController : ControllerBase
{
    private readonly VerificationService _verificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _applicationDbContext;

    public RegisterController(VerificationService verificationService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext)
    {
        _verificationService = verificationService;
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
    }

    [HttpGet("verification-response")]
    public async Task<ActionResult> VerificationResponseAsync([FromQuery] string? id)
    {
        try
        {
            if (id == null)
            {
                return BadRequest(new { error = "400", error_description = "Missing argument 'id'" });
            }

            var verificationModel = await _verificationService.GetVerificationStatus(id);

            if (verificationModel != null && verificationModel.state == "SUCCESS")
            {
                // In a business app we can use the data from the verificationModel
                // Verification data:
                // Use: wallet_response/credential_subject_data
                var verificationClaims = _verificationService.GetVerifiedClaims(verificationModel);

                var user = await _userManager.FindByEmailAsync(GetEmail(User.Claims)!);

                var exists = _applicationDbContext.SwiyuIdentity.FirstOrDefault(c =>
                    c.BirthDate == verificationClaims.BirthDate &&
                    c.BirthPlace == verificationClaims.BirthPlace &&
                    c.GivenName == verificationClaims.GivenName &&
                    c.FamilyName == verificationClaims.FamilyName);

                if (exists != null)
                {
                    throw new Exception("swiyu already in use and connected to an account...");
                }

                if (user != null && user.SwiyuIdentityId == null)
                {
                    var swiyuIdentity = new SwiyuIdentity
                    {
                        UserId = user.Id,
                        BirthDate = verificationClaims.BirthDate,
                        FamilyName = verificationClaims.FamilyName,
                        BirthPlace = verificationClaims.BirthPlace,
                        GivenName = verificationClaims.GivenName,
                        Email = user.Email!
                    };

                    _applicationDbContext.SwiyuIdentity.Add(swiyuIdentity);

                    // Save to DB
                    user.SwiyuIdentityId = swiyuIdentity.Id;

                    await _applicationDbContext.SaveChangesAsync();
                }
            }

            return Ok(verificationModel);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "400", error_description = ex.Message });
        }
    }

    public static string? GetEmail(IEnumerable<Claim> claims)
    {
        var email = claims.FirstOrDefault(t => t.Type == ClaimTypes.Email);

        if (email != null)
        {
            return email.Value;
        }

        email = claims.FirstOrDefault(t => t.Type == JwtClaimTypes.Email);

        if (email != null)
        {
            return email.Value;
        }

        email = claims.FirstOrDefault(t => t.Type == "preferred_username");

        if (email != null)
        {
            var isNameAndEmail = IsEmailValid(email.Value);
            if (isNameAndEmail)
            {
                return email.Value;
            }
        }

        return null;
    }

    public static bool IsEmailValid(string email)
    {
        if (!MailAddress.TryCreate(email, out var mailAddress))
            return false;

        // And if you want to be more strict:
        var hostParts = mailAddress.Host.Split('.');
        if (hostParts.Length == 1)
            return false; // No dot.
        if (hostParts.Any(p => p == string.Empty))
            return false; // Double dot.
        if (hostParts[^1].Length < 2)
            return false; // TLD only one letter.

        if (mailAddress.User.Contains(' '))
            return false;
        if (mailAddress.User.Split('.').Any(p => p == string.Empty))
            return false; // Double dot or dot at end of user part.

        return true;
    }
}
