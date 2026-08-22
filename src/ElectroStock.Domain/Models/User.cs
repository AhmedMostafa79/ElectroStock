using ElectroStock.Domain.Enums;
using ElectroStock.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
    public abstract class User
    {
        // Parameterless constructor for EF Core materialization (TPH)
        protected User() { }
        
        [Key]
        public Guid Id {  get; private set; }

        [Required]
        [MaxLength(20)]
        public string Phone {  get; private set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; private set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; private set; }

        [Required]
        public UserRole Role { get; private set; }
       
        [Required]
        public UserType UserType { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public User(UserCreationData data, UserType userType)
        {
            Id = Guid.NewGuid();

            ValidatePhoneNumber(data.PhoneNumber);
            Phone = data.PhoneNumber;

            FirstName = data.FirstName;
            LastName = data.LastName;
            Role = data.Role;
            UserType = userType;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Validates phone number is exactly 11 digits
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <exception cref="ArgumentException">Thrown when phone number is invalid</exception>
        private void ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone Number cannot be empty");

            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
            int phoneNumberLength = 11;
            if ((cleaned.Length != phoneNumberLength))
                throw new ArgumentException("Phone number must be exactly 11 digits");
        }
        public abstract bool HasEmail();
    }
}
