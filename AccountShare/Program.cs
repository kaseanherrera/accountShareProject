using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Salesforce.Common;
using Salesforce.Force;
using System.Threading.Tasks;
using System.Dynamic;
using System.Windows.Forms;


namespace AccountShare
{
    static class Program
    {
      
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        static string instanceUrl;
        static string AccessToken;
        static string ApiVersion;

        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());
        }
    }

   
}

