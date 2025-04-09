using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot_my_helper.DB.Model
{
    public class User
    {
        public int Id { get; set; }
        public long IdTelegram { get; set; }
        public string? UserName { get; set; }
        public DateTime DateFirstTouch { get; set; }

        public ICollection<Message>? Messages { get; set; }
    }
}
