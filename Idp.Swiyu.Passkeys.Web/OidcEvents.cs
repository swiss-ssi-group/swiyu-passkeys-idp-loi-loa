// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Client;

public class OidcEvents : OpenIdConnectEvents
{
    private readonly AssertionService _assertionService;

    public OidcEvents(AssertionService assertionService)
    {
        _assertionService = assertionService;
    }

    public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
        if (context.Properties.Items.ContainsKey("acr_values"))
        {
            context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
        }

        context.TokenEndpointRequest.ClientAssertionType = OidcConstants.ClientAssertionTypes.JwtBearer;
        context.TokenEndpointRequest.ClientAssertion = _assertionService.CreateClientToken();

        return Task.CompletedTask;
    }

    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        var request = _assertionService.SignAuthorizationRequest(context.ProtocolMessage);
        var clientId = context.ProtocolMessage.ClientId;
        var redirectUri = context.ProtocolMessage.RedirectUri;

        context.ProtocolMessage.Parameters.Clear();
        context.ProtocolMessage.ClientId = clientId;
        context.ProtocolMessage.RedirectUri = redirectUri;
        context.ProtocolMessage.SetParameter("request", request);

        return Task.CompletedTask;
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
