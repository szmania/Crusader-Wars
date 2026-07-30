using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.tests
{
    public class TestConfiguration
    {
        public static string GetTestDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
