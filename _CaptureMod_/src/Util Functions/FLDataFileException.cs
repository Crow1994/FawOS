using System;
using System.Collections.Generic;
using System.Text;

namespace DAM
{
    class FLDataFileException : Exception
    {
        public FLDataFileException(string msg) : base(msg) { }
    }
}
