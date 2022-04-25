using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQProducerAPI.Models
{
    public class LoginContext : DbContext
    {
        public LoginContext(DbContextOptions<LoginContext> options)
           : base(options)
        {
        }

        public DbSet<Login> LoginItems { get; set; } = null!;
    }
}
