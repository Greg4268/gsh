using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src
{
    public struct CommandInfo
    {
        public string Command; 
        public string[] Args;
        public string Operator;
        public string RedirectFileName; 
    }
}