using System;
using System.Collections.Generic;
using ServiceSourcing.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Services
{
    public interface ITwilioService
    {
        string SendText(SMS sms);
    }
}
