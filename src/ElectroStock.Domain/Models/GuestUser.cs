using ElectroStock.Domain.ValueObjects;
using ElectroStock.Domain.Enums;

namespace ElectroStock.Domain.Models
{
    public class GuestUser:User
    {
        // Parameterless constructor for EF Core materialization
        protected GuestUser() { }
        
        public GuestUser(UserCreationData data) : base(data:data,userType: UserType.Guest) { }

        public override bool HasEmail() => false;
    }
}
