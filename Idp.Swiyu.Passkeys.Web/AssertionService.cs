// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Client;

public class AssertionService
{
    private readonly IConfiguration _configuration;

    public AssertionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateClientToken()
    {
        var now = DateTime.UtcNow;
        var clientId = _configuration.GetValue<string>("OpenIDConnectSettings:ClientId");
        //var key = _configuration.GetValue<string>("OpenIDConnectSettings:Secrets:Key");
        var authority = _configuration.GetValue<string>("OpenIDConnectSettings:Authority");

        var privatePem = File.ReadAllText(Path.Combine("", "rsa256-private.pem"));
        var publicPem = File.ReadAllText(Path.Combine("", "rsa256-public.pem"));
        var rsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
        var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());
        var signingCredentials = new SigningCredentials(new X509SecurityKey(rsaCertificate), "RS256");

        var token = new JwtSecurityToken(
            clientId,
            authority,
            new List<Claim>()
            {
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                new Claim(JwtClaimTypes.Subject, clientId!),
                new Claim(JwtClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            now,
            now.AddMinutes(1),
            signingCredentials
        );

        token.Header[JwtClaimTypes.TokenType] = "client-authentication+jwt";

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        return tokenHandler.WriteToken(token);
    }

    public string SignAuthorizationRequest(OpenIdConnectMessage message)
    {
        var now = DateTime.UtcNow;
        var clientId = _configuration.GetValue<string>("OpenIDConnectSettings:ClientId");
        //var key = _configuration.GetValue<string>("OpenIDConnectSettings:Secrets:Key");
        var authority = _configuration.GetValue<string>("OpenIDConnectSettings:Authority");

        var privatePem = File.ReadAllText(Path.Combine("", "rsa256-private.pem"));
        var publicPem = File.ReadAllText(Path.Combine("", "rsa256-public.pem"));
        var rsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
        var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());
        var signingCredentials = new SigningCredentials(new X509SecurityKey(rsaCertificate), "RS256");

        var claims = new List<Claim>();
        foreach (var parameter in message.Parameters)
        {
            claims.Add(new Claim(parameter.Key, parameter.Value));
        }

        var token = new JwtSecurityToken(
            clientId,
            authority,
            claims,
            now,
            now.AddMinutes(1),
            signingCredentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        return tokenHandler.WriteToken(token);
    }
}
