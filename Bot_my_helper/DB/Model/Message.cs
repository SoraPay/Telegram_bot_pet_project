using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot_my_helper.DB.Model
{
    public class Message
    {
        public int Id { get; set; }

        public string? MessageSend { get; set; }

        public DateTime DateSending { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
