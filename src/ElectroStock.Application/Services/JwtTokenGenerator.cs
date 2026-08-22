using ElectroStock.Application.Dtos;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using ElectroStock.Application.Helpers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using ElectroStock.Application.Interfaces.Services;

namespace ElectroStock.Application.Services
{
    public class JwtTokenGenerator : ITokenService
    {
        private readonly JWT _jwt;

        /// <summary>
        /// Initializes a new instance of JwtTokenService.
        /// </summary>
        /// <param name="jwt">JWT configuration settings</param>
        public JwtTokenGenerator(IOptions<JWT> jwt)// used IOptions<JWT> not JWT to apply DI 
        {
            _jwt = jwt.Value;
        }

        private JwtSecurityToken GenerateToken(UserDto user)
        {
            var roleClaims = new List<Claim>();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),  //who the token is for (userId) same as ClaimTypes.sub
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()), //an ID for the token itself, can be stored for lookup
                new Claim(JwtRegisteredClaimNames.PhoneNumber,user.PhoneNumber), //Email
                new Claim(ClaimTypes.Role,user.Role.ToString()), //Role for authorization
                new Claim(ClaimTypes.Name,$"{user.FirstName} {user.LastName}") //user's name
            };

            //converting the secret key into binary array
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            var signingCredentials=new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);// it prepares the signature maker (HMac-sha256 using secretKey)

            // creates the token Header.Payload.Signature
            return new JwtSecurityToken(
                issuer:_jwt.Issuer,
                audience:_jwt.Audience,
                claims:claims,
                expires:DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials:signingCredentials
                );
        }
        public TokenDto GetToken(UserDto userDto)
        {
            var jwtToken = GenerateToken(userDto);
            return new TokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresAt = jwtToken.ValidTo,
                TokenType = GetTokenType()
            };
        }
        public string GetTokenType() => "Bearer";
    }
}
