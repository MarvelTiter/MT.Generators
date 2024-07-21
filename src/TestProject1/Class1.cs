using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    internal interface ITest
    {
        void Log<T>(string message);
    }
    internal class Class1 : ITest
    {
        public void Log<T>(string message)
        {
            throw new NotImplementedException();
        }
    }
}
