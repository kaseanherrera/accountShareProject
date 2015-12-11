using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using Salesforce.Common;
using Salesforce.Force;
using System.Dynamic;
using System.IO;


namespace AccountShare
{
    public partial class Form1 : Form
    {
        //client information 
        static string instanceUrl;
        static string AccessToken;
        static string ApiVersion;
        //account information 
        private string accountName;
        private  string accountId;
        private string oldOwnerName;
        private string newOwnerID;
        private string newOwnerName;
        //varification variables
        private bool accountVerified;
        private bool ownerVerified;
        //constructor
        public Form1(string instanceUrlInput, string AccessTokenInput, string ApiVersionInput)
        {
            // TODO: Complete member initialization
            instanceUrl = instanceUrlInput;
            AccessToken = AccessTokenInput;
            ApiVersion = ApiVersionInput;
            accountVerified = false;
            ownerVerified = false;
   
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        //verify an account 
        private async void button4_Click(object sender, EventArgs e)
        {
            checkAccountString();
            //create a SF client
            var client = new ForceClient(instanceUrl, AccessToken, ApiVersion);
            //create a straing to qquery the account information in salesforce
            string query = String.Format(@"Select Name,OwnerId , Type,SAP_Account_Number__c FROM Account WHERE Id = '{0}'", accountId);
            //let the user know that you are looking up the account number
            userMessage.Text = "Looking up Account Number";
            try
            {
                //run the account query 
                var accountResults = await client.QueryAsync<Account>(query);
                if (accountResults.TotalSize == 0)
                {
                    userMessage.Text = "Account number does not exist in the Salesforce!";
                    return;
                }
                //create a query for the username in salesforce 
                string userNameQuery = String.Format(@"Select Username FROM User WHERE Id = '{0}' ", accountResults.Records[0].OwnerId);
                //run query for username 
                var userNameQueryResults = await client.QueryAsync<User>(userNameQuery);
                //Display the information from the query results to the user
                AccountName.Text = accountResults.Records[0].Name;
                SapId.Text = accountResults.Records[0].SAP_Account_Number__c;
                Owner.Text = userNameQueryResults.Records[0].UserName;
                //set global variables from results 
                oldOwnerName = userNameQueryResults.Records[0].UserName;
                accountName = accountResults.Records[0].Name;
                //ser verified
                accountVerified = true;
            }
            catch (ForceException)
            {
                userMessage.Text = "Invalid User Id";
                return;
            }
        }
        //verify new owner
        private async void button5_Click(object sender, System.EventArgs e)
        {
            checkUserIdString();
            //create a SF client
            var client = new ForceClient(instanceUrl, AccessToken, ApiVersion);

            try
            {
                //let the user know that you are looking for the new owner
                userMessage.Text = "Looking up the new user";
                //create a query string to look up user in salesforce
                string userQuery = String.Format(@"Select Username, IsActive, Email FROM User WHERE Id = '{0}' ", newOwnerID);
                //run query
                var results = await client.QueryAsync<User>(userQuery);
                //check for fail or not exist 
                if (results.TotalSize == 0)
                {
                    userMessage.Text = "There is no user with that ID. Please try again.";
                    return;
                }
                //set global information about user
                newOwnerName = results.Records[0].UserName;
                //display information to the user
                userNameLabel.Text = newOwnerName;
                userEmailLabel.Text = results.Records[0].Email;
                userActiveLabel.Text = results.Records[0].IsActive.ToString();
                //set verified
                ownerVerified = true;
            }
            catch (ForceException)
            {
                userMessage.Text = "Username Error. Please Try Again.";
            }

        }
        //transfer records
        private async void button3_Click(object sender, EventArgs e)
        {
            if (accountVerified && ownerVerified)
            {
                //create a client 
                var client = new ForceClient(instanceUrl, AccessToken, ApiVersion);
                //query string for manual account shares with matching id
                string query = String.Format(@"Select Id, AccountId, UserOrGroupId, AccountAccessLevel, 
                    OpportunityAccessLevel, CaseAccessLevel, ContactAccessLevel, RowCause, 
                    LastModifiedDate, LastModifiedById, IsDeleted FROM AccountShare 
                    WHERE  AccountId = '{0}' 
                    AND  RowCause = 'Manual'", accountId);
                //run query
                var results = await client.QueryAsync<AccountShare>(query);
                //let the user know that you are update the owner
                userMessage.Text = "Updating Owner!";
                //update the owner 
                try
                {
                    var success = await client.UpdateAsync("Account", accountId, new { OwnerId = newOwnerID });
                    //check for susscess
                    if (!string.IsNullOrEmpty(success.Errors.ToString()))
                    {
                        userMessage.Text = "Failed to update record owner";
                        return;
                    }
                }
                catch (Exception a)
                {
                    userMessage.Text = "Failed to update record owner";
                }
              
                //Finsihed update
                userMessage.Text = "Successfully updated record owner";
                //write to the file account id - account name - owner - new owner - new owner id - records before 
                writeToFile(accountId, accountName, newOwnerID);
                //create account share objects from all of the ones we have saved in memory 
                userMessage.Text = "Transferring Rercords.";
                foreach (AccountShare share in results.Records)
                {
                    //create an account share data structure 
                    var newShare = new AccountShare
                    {
                        AccountId = share.AccountId,
                        UserOrGroupId = share.UserOrGroupId,
                        AccountAccessLevel = share.AccountAccessLevel,
                        OpportunityAccessLevel = share.OpportunityAccessLevel,
                        CaseAccessLevel = share.CaseAccessLevel,
                        ContactAccessLevel = share.ContactAccessLevel,
                        RowCause = share.RowCause,
                    };
                    //write the information to file
                    writeToFile(share);
                    //create that same one in salesforce
                    await client.CreateAsync(AccountShare.SObjectTypeName, newShare);

                }
                userMessage.Text = "Transferring Finished";
                //close the varification 
                accountVerified = false;
                ownerVerified = false;
            }
            else
            {
                userMessage.Text = "Please verify account and owner";
            }
        }
        //write the account share information to a file on the user desktop
        private void writeToFile(AccountShare share)
        {
            //path to the user desktop 
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //add the file name and the correct extension 
            path = path + @"\AccountShareLog.txt";

            using (StreamWriter sw = File.AppendText(path))
            {
                string shareString = string.Format(
               @"ID: {0}{8}Account ID: {1}{8}User or group id: {2}{8}Account accessLevel: {3}{8}Opportunity access level: {4}{8}Case  access level: {5}{8}Contact access level: {6}{8}Row Cause: {7}{8}{8}
                ", 
                                 share.Id,
                                 share.AccountId,
                                 share.UserOrGroupId,
                                 share.AccountAccessLevel,
                                 share.OpportunityAccessLevel,
                                 share.CaseAccessLevel,
                                 share.ContactAccessLevel,
                                 share.RowCause,
                                 Environment.NewLine)
                                 ;
               //wirte the string to the file 
                sw.WriteLine(shareString); 
                
            }  
        }
        //write the account and user information to a file 
        
        private void writeToFile(string AccountID, string accountName, string newOwnerID)
        {
            //path to the user desktop 
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //add the file name and the correct extension 
            path = path + @"\AccountShareLog.txt";

            using (StreamWriter sw = File.AppendText(path))
            {
                string InfoString = string.Format(
               @"*******************************************************************
                Date: {0}
                Account ID : {1}
                Account Name: {2}
                New Owner Name: {3}
                Old Owner Name: {4}


                               ",
                                    
                                 System.DateTime.Now,
                                 AccountID,
                                 accountName,
                                 newOwnerName,
                                 oldOwnerName);
                //wirte the string to the file 
                sw.WriteLine(InfoString);

            }  
        }

        //sets the global account variable and checks its length > 0 
        private void checkAccountString()
        {
            //set account ID
            accountId = AccountIdInput.Text.Trim();
            //check if its not empty
            if (accountId.Length == 0)
            {
                userMessage.Text = "Must input an account ID!";
                return;
            }
        }
        //sets the global new user id and checks that it is > 0
        private void checkUserIdString()
        {
            //get the new user ID from the input box 
            newOwnerID = newUserInput.Text.Trim();
            if (newOwnerID.Length == 0)
            {
                userMessage.Text = "Must input a new user ID!";
                return;
            }
        }


        // <---back button 
        private void button2_Click(object sender, EventArgs e)
        {
            //create a new form for login 
            this.Hide();
            new Login().ShowDialog();
        }

        //account share object 
        public class AccountShare
        {
            public const String SObjectTypeName = "AccountShare";
            public String Id { get; set; }
            public String AccountId { get; set; }
            public String UserOrGroupId { get; set; }
            public String AccountAccessLevel { get; set; }
            public String OpportunityAccessLevel { get; set; }
            public String CaseAccessLevel { get; set; }
            public String ContactAccessLevel { get; set; }
            public String RowCause { get; set; }    
        }
        //account object
        public class Account
        {
            public const String SObjectTypeName = "Account";
            public String Name { get; set; }
            public String OwnerId { get; set; }
            public String Type { get; set; }
            public String SAP_Account_Number__c  { get; set; }
        }
        //user object 
        public class User
        {
            public const String SObjectTypeName = "User";
            public String UserName { get; set; }
            public Boolean IsActive  { get; set; }
            public String Email { get; set; }
        }

      
    }
}
