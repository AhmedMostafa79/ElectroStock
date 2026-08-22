using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;

namespace ElectroStock.Application.Mappings
{
    public static class UserMappingExtension
    {

        public static UserDto ToDto(this User user)
        {
            if (user == null)
                return null;

            return user switch
            {
                RegisteredUser registered => new UserDto
                {
                    Id = registered.Id,
                    FirstName = registered.FirstName,
                    LastName = registered.LastName,
                    PhoneNumber = registered.Phone,
                    Role = registered.Role,
                    UserType = registered.UserType,
                    CreatedAt = registered.CreatedAt,
                    Email = registered.Email,
                    UserStatus = registered.Status
                },
                GuestUser guest => new UserDto
                {

                    Id = guest.Id,
                    FirstName = guest.FirstName,
                    LastName = guest.LastName,
                    PhoneNumber = guest.Phone,
                    Role = guest.Role,
                    UserType = guest.UserType,
                    CreatedAt = guest.CreatedAt,
                    Email = null,
                    UserStatus = null
                }
            };

        }
        public static UserCreationData ToCreationData(this RegisterUserDto userDto)
        {
            if (userDto == null)
                return null;

            return new UserCreationData
            {
                FirstName=userDto.FirstName,
                LastName=userDto.LastName,
                PhoneNumber=userDto.PhoneNumber,
                Role=userDto.Role,
            };
        }
    }
}
