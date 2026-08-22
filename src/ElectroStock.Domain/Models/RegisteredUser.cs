using ElectroStock.Domain.Enums;
using ElectroStock.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
    public class RegisteredUser:User
    {
        // Parameterless constructor for EF Core materialization
        protected RegisteredUser() { }
        
        [Required]
        [MaxLength(255)]
        public string Email { get; private set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash {  get; private set; }

        public UserStatus Status { get; private set; }

        public RegisteredUser(UserCreationData data,string email,
            string passwordHash): base(data,UserType.Registered) {

            ValidateEmail(email);
            Email =email;

            

            ChangePassword(passwordHash);
            Status = UserStatus.Active;

        }
        public void Activate() => Status = UserStatus.Active;
        public void Deactivate() => Status = UserStatus.InActive;

        public override bool HasEmail() => true;

        public void ChangePassword(string newHash)
        {
            if (string.IsNullOrWhiteSpace(newHash))
                throw new ArgumentException("Password cannot be empty!");

            PasswordHash = newHash;
        }

        /// <summary>
        /// Validates email format using Gmail-style regex pattern
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <exception cref="ArgumentException">Thrown when email is invalid</exception>
        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty");

            // This regex matches Gmail's acceptance criteria
            string pattern = @"^[a-zA-Z0-9](?!.*\.\.)[a-zA-Z0-9.]*[a-zA-Z0-9]@gmail\.com$";
            
            if (!Regex.IsMatch(email.Trim(), pattern))
                throw new ArgumentException("Invalid email format");

            // Additional length checks
            var parts = email.Split('@');
            if (parts[0].Length > 64)
                throw new ArgumentException("Email local part too long");
            if (parts[1].Length > 255)
                throw new ArgumentException("Email domain too long");
        }
    }
}
