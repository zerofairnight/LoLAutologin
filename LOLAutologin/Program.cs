using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LOLAutologin
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if( args != null && args.Length > 0 )
            {
                // ...
            }
            else
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
