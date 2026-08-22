using ElectroStock.Application.Interfaces.Services;

namespace ElectroStock.Infrastructure.Services
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;  // BCrypt work factor (higher = slower = more secure)
        
        public string HashPassword(string password)
        {
           return BCrypt.Net.BCrypt.HashPassword(password,WorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
