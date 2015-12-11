using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Salesforce.Common;
using Salesforce.Force;
using System.Dynamic;
using System.Windows.Forms;
using System.Text;
using System.Web.Services.Protocols;
using AccountShare.sforce;
using System.Threading.Tasks;
using System.Text.RegularExpressions;




namespace AccountShare
{
    public partial class Login : Form
    {

        string instanceUrl;
        static string AccessToken;
        static string ApiVersion = "v34.0";
        private SforceService binding;

        public Login()
        {
            InitializeComponent();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if(userName.Text.Length == 0 || password.Text.Length == 0){
                     label4.Text = "Must enter a username and password!";
                     return;
                }

                label4.Text = "Verifying salesforce username and password.";
                bool gotIn = login(userName.Text, password.Text);
               

                if (gotIn) {
                    label4.Text = "Success";
                this.Hide();
                new Form1(instanceUrl, AccessToken, ApiVersion).ShowDialog();
                } 

                
    
               

            }
            catch (Exception a)
            {
                //tell the user that something went wrong 
                
                Console.WriteLine(a.Message);
                Console.WriteLine(a.StackTrace);

                var innerException = a.InnerException;
                while (innerException != null)
                {
                    Console.WriteLine(innerException.Message);
                    Console.WriteLine(innerException.StackTrace);

                    innerException = innerException.InnerException;
                }
            }

            label4.Text = "Error logging in to Salesforce. Please check credentials.";

         
          

        }

        private bool login(string username, string password)
        {
            loginButton.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            // Create a service object
            binding = new SforceService();
            // Timeout after a minute
            binding.Timeout = 60000;
            // Try logging in
            LoginResult lr;
            try
            {
                lr = binding.login(username, password);
            }
            // ApiFault is a proxy stub generated from the WSDL contract when
            // the web service was imported
            catch (SoapException e)
            {
           
                // Write the fault code to the console
                Console.WriteLine(e.Code);
                // Write the fault message to the console
                Console.WriteLine("An unexpected error has occurred: " + e.Message);
                // Write the stack trace to the console
                Console.WriteLine(e.StackTrace);
                // Return False to indicate that the login was not successful
                loginButton.Enabled = true;
                Cursor.Current = Cursors.Default;
                return false;
            }
            // Check if the password has expired
            if (lr.passwordExpired)
            {
                Console.WriteLine("An error has occurred. Your password has expired.");
                loginButton.Enabled = true;
                Cursor.Current = Cursors.Default;
                return false;
            }
           
            AccessToken = lr.sessionId;
            String url = Regex.Split(lr.serverUrl, @"/services")[0];
            instanceUrl = url;

            loginButton.Enabled = true;
            Cursor.Current = Cursors.Default;
            return true;
        }


        private void userName_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
