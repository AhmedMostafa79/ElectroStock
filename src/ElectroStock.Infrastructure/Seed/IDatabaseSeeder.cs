using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Infrastructure.Seed
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync();
    }
}
