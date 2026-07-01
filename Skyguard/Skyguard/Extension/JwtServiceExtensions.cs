using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Skyguard.Extension
{
    public static class JwtServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettings = config.GetSection("JwtSettings");

            string secretKey = jwtSettings["SecretKey"]
                               ?? config["jwt:JwtSettings:SecretKey"]
                               ?? "qtmWCY3GispBq5oLcKo44JfIpk1VrjOHPu5bxZFtZZBGOvF1BZGtABImTIWwusdFeCaX1sU3Iu0cpPT6pgVkWkxmaDTboDfa5Dg1WvFbZl3F0EgO1LhQaqlvjmeKmayPRWBJQeegMHUOBzVpBQHUSVyqTU5FvIx1SuT8eCdYqmGgYK52ogW6JAzCXCq0dc96pj8vVu2MwdZ6ED8U3BE4Bh6pSX1aEePDiys4lBfApLro4Jx7wxKi0EiRcJIpkX3wBYdFpqyLotFpjsQOP0F1VuGbYQAjK1I2q14dH5Lb9xqrvHcO297LL7kTDXsX8twHmIDbjXhiKuumRtldloZmPElu0DPPbzfrbjwxGml54pgLjuhZ0hTiQSbr8LCWuoHYblUgMQ3r6WYV9TqHiqHUkjK3I8WpsJ4JkMrwPmfT6otxhrfke6pmKtluGzuRPOpzCXUL8DDzk7mxHXgz4jgDOIgXIuifWfWZlx1YWgv5GqTDv7RWlm8WzzB8XTT7rvpp";

            string issuer = jwtSettings["Issuer"] ?? "SkyGuardAuth";
            string audience = jwtSettings["Audience"] ?? "SkyGuardUsers";

            var key = Encoding.UTF8.GetBytes(secretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}
