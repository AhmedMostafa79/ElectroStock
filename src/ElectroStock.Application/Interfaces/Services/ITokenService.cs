using ElectroStock.Application.Dtos;

namespace ElectroStock.Application.Interfaces.Services
{
    public interface ITokenService
    {
        public TokenDto GetToken(UserDto userDto);
        public string GetTokenType();
    }
}
