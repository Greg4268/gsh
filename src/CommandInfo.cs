using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace src
{
    public struct CommandInfo
    {
        public string Command; 
        public string[] Args;
        public string Operator;
        [Description("file path parsed to use for redirect")]
        public string RedirectFileName; 
    }
}