using System.Collections.Generic;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using FigurasQE_AuthenticationService.Models;


namespace FigurasQE_AuthenticationService.Services
{
    public class JwtService
    {
        private readonly IConfiguration Configuration;
        private readonly IJsonSerializer Serializer;
        private readonly IBase64UrlEncoder UrlEncoder;
        private readonly IJwtAlgorithm Algorithm;

        public JwtService(IConfiguration configuration)
        {
            Configuration = configuration;
            Serializer = new JsonNetSerializer();
            UrlEncoder = new JwtBase64UrlEncoder();
            Algorithm = new HMACSHA256Algorithm();
        }

        public Dictionary<string, object> CreatePayload(AuthUser user)
        {
            return new Dictionary<string, object>
            {
                { "sub", user.Id.ToString() },
                { "email", user.Email },
                { "role", user.Role },
                { "iss", Configuration["Jwt:Issuer"] },
                { "aud", Configuration["Jwt:Audience"] },
                { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() }
            };
        }

        public string EncodeToken(AuthUser user)
        {
            var payload = CreatePayload(user);

            IJwtEncoder encoder = new JwtEncoder(Algorithm, Serializer, UrlEncoder);

            var secret = Configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(secret))
                throw new Exception("Jwt:Secret not configured");
            var token = encoder.Encode(payload, secret);

            return token;
        }
    }
}