using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQProducerAPI.Models
{
    public class Login
    {
        public int ID { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string task { get; set; }
    }
}
