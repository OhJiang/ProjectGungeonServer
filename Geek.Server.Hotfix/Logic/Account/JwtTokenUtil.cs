using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Geek.Server.App.Common;
using Microsoft.IdentityModel.Tokens;

namespace Server.Logic.Logic.Account
{
	public static class JwtTokenUtil
	{
		private static readonly Logger s_Log = LogManager.GetCurrentClassLogger();
		private const int CUSTOM_TOKEN_EXPIRY_TIME = 30;
		private const string Jwtkey = "PCCjW6pSoOHVNpiboEAmHpoH1lcOci9u";
		private const string AndroidPackage = "com.redfalcon.baseballgoat.android";
		private const string IosPackage = "com.redfalcon.baseballgoat.ios";

		public static string GenerateJwtToken(string userName, int sdkType, long accountId, string platform = Platform.ANDROID)
		{
			var claims = new[]
			{
				new Claim(ClaimTypes.Name, userName),
				new Claim(ClaimTypes.Role, sdkType.ToString()),
				new Claim("AccountId", accountId.ToString())
			};
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Jwtkey));
			var creeds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var token = new JwtSecurityToken(
				issuer: platform == Platform.ANDROID ? AndroidPackage : IosPackage,
				audience: platform == Platform.ANDROID ? AndroidPackage : IosPackage,
				claims: claims,
				expires: DateTime.UtcNow.AddDays(CUSTOM_TOKEN_EXPIRY_TIME),
				signingCredentials: creeds);
			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		public static ClaimsPrincipal ValidateJwtToken(string token, string platform = Platform.ANDROID)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(Jwtkey);
			var validationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidIssuer = platform == Platform.ANDROID ? AndroidPackage : IosPackage,
				ValidateAudience = true,
				ValidAudience = platform == Platform.ANDROID ? AndroidPackage : IosPackage,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};
			var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
			return principal;
		}
	}
}