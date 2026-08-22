using ElectroStock.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.ValueObjects
{
    public class UserCreationData
    {
        public string PhoneNumber { get;  set; }
        public string FirstName { get;  set; }
        public string LastName { get;  set; }
        public UserRole Role { get;  set; }
        
    }
}
