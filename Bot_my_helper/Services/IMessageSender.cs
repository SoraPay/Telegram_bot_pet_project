using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot_my_helper.Services
{
    public interface IMessageSender
    {
        Task SendLogToTelegram(string text);
    }
}
