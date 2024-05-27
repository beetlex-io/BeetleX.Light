using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Args
{
    public class ParserException : Exception
    {
        public ParserException(string error) : base(error)
        {

        }
    }
}
