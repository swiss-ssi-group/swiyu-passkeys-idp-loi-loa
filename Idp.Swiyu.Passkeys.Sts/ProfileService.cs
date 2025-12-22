using System.Security.Claims;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Idp.Swiyu.Passkeys.Sts;

public class ProfileService: IProfileService
{
	public async Task GetProfileDataAsync(ProfileDataRequestContext context)
	{
        // context.Subject is the user for whom the result is being made
        // context.Subject.Claims is the claims collection from the user's session cookie at login time
        // context.IssuedClaims is the collection of claims that your logic has decided to return in the response

        var loa = new Claim("loa", Consts.LOA_100);
        var loi = new Claim("loi", Consts.LOI_100);
        var amrClaim = context.Subject.Claims.FirstOrDefault(c => c.Type == "amr");

        // TODO
        //if (amrClaim != null && amrClaim.Value == "pwd")
        //{
            
        //}
        if (context.Caller == IdentityServerConstants.ProfileDataCallers.ClaimsProviderAccessToken)
        {
            // access_token
            context.IssuedClaims.Add(loa);
            context.IssuedClaims.Add(loi);
        }

        if (context.Caller == IdentityServerConstants.ProfileDataCallers.ClaimsProviderIdentityToken)
        {
            // id_token
            context.IssuedClaims.Add(loa);
            context.IssuedClaims.Add(loi);

            var oid = context.Subject.Claims.FirstOrDefault(t => t.Type == "oid");

            if(oid != null)
            {
                context.IssuedClaims.Add(new Claim("oid", oid.Value));
            }
        }

        if (context.Caller == IdentityServerConstants.ProfileDataCallers.UserInfoEndpoint)
        {
            // user_info endpoint
            var oid = context.Subject.Claims.FirstOrDefault(t => t.Type == "oid");

            if (oid != null)
            {
                context.IssuedClaims.Add(new Claim("oid", oid.Value));
            }
        }


        return;
	}

	public Task IsActiveAsync(IsActiveContext context)
	{
		context.IsActive = true;
		return Task.CompletedTask;
	}
}