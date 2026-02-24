using System.ComponentModel;
namespace src
{
    public struct CommandReturnStruct
    {
        public string[] Output;
        [Description("-1=Sentinel,0=Pass,1=Fail")]
        public int ReturnCode; 
        public string Error; 
    }
}