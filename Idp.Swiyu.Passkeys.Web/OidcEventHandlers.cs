// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Idp.Swiyu.Passkeys.Web;

public static class OidcEventHandlers
{
    public static OpenIdConnectEvents OidcEvents(IConfiguration configuration)
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = async context => await OnRedirectToIdentityProviderHandler(context, configuration),
            OnAuthorizationCodeReceived = async context => await OnAuthorizationCodeReceivedHandler(context, configuration),
        };
    }

    private static async Task OnAuthorizationCodeReceivedHandler(AuthorizationCodeReceivedContext context, IConfiguration configuration)
    {
        // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
        if (context.Properties != null && context.Properties.Items.ContainsKey("acr_values"))
        {
            context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
        }

        if(context.TokenEndpointRequest != null)
        {
            context.TokenEndpointRequest.ClientAssertionType = OidcConstants.ClientAssertionTypes.JwtBearer;
            context.TokenEndpointRequest.ClientAssertion = AssertionService.CreateClientToken(configuration);
        }
    }

    private static async Task OnRedirectToIdentityProviderHandler(RedirectContext context, IConfiguration configuration)
    {
        var request = AssertionService.SignAuthorizationRequest(context.ProtocolMessage, configuration);
        var clientId = context.ProtocolMessage.ClientId;
        var redirectUri = context.ProtocolMessage.RedirectUri;

        context.ProtocolMessage.Parameters.Clear();
        context.ProtocolMessage.ClientId = clientId;
        context.ProtocolMessage.RedirectUri = redirectUri;
        context.ProtocolMessage.SetParameter("request", request);
    }


    //OnPushAuthorization = context =>
    //{
    //    context.ProtocolMessage.Parameters.Add("client_assertion", clientAssertion);
    //    context.ProtocolMessage.Parameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
    //    // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
    //    if (context.Properties.Items.ContainsKey("acr_values"))
    //    {
    //        context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
    //    }
    //    return Task.CompletedTask;
    //},

}
