using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LXR.Recorder
{
    public class MyException : Exception
    {
        string message = "This s a customer exception test number ";

        public MyException(string m):base()
        {
            message += m;
        }

        public override string Message
        {
            get
            {
                return message;
            }
        }
    }
}
