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


namespace AccountShare
{
    public partial class Form1 : Form
    {


        static string instanceUrl;
        static string AccessToken;
        static string ApiVersion;
        public string AccountID;
        public string newOwnerID;
        public string accountName;
       
        public Form1()
        {
            InitializeComponent();
        }

        public Form1(string instanceUrlInput, string AccessTokenInput, string ApiVersionInput)
        {
            // TODO: Complete member initialization
            instanceUrl = instanceUrlInput;
            AccessToken = AccessTokenInput;
            ApiVersion = ApiVersionInput;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


        private async void button4_Click(object sender, EventArgs e)
        {
            
            //get the account number from the account number input box
            string accountId = AccountIdInput.Text;
            //check if its not empty
            if(accountId.Length == 0){
                userMessage.Text = "Must input an account ID!";
                return;
            }
          
            userMessage.Text = "Looking up Account Number";
            var client = new ForceClient(instanceUrl, AccessToken, ApiVersion);
            //queryfor the account in salesforce
            string query = String.Format(@"Select Name,OwnerId , Type,SAP_Account_Number__c FROM Account WHERE Id = '{0}'",
                                               accountId);
          

            try
            {
                var results = await client.QueryAsync<Account>(query);
                //if the results is empty return with message
                if (results.TotalSize == 0)
                {
                    userMessage.Text = "Account number does not exist in the Salesforce!";
                    return;
                }




                //query for the username
                string userNameQuery = String.Format(@"Select Username FROM User WHERE Id = '{0}' ",
                                      results.Records[0].OwnerId);

                var userNameQueryResults = await client.QueryAsync<User>(userNameQuery);

                //get the results Account Name, SAP ID, and Owner
                AccountName.Text = results.Records[0].Name;
                SapId.Text = results.Records[0].SAP_Account_Number__c;
                Owner.Text = userNameQueryResults.Records[0].UserName;
                AccountID = accountId;
                accountName = results.Records[0].Name;
                
            }
            catch(ForceException)
            {
                userMessage.Text = "Invalid User Id";
                return;
            }
            
       
           

        }
     

        private async void button5_Click(object sender, System.EventArgs e)
        {
           //get the new user ID from the input box 
            string newUserId = newUserInput.Text;
            if (newUserId.Length == 0)
            {
                userMessage.Text = "Must input a new user ID!";
                return;
            }

            userMessage.Text = "Looking up the new user";
            var client = new ForceClient(instanceUrl, AccessToken, ApiVersion);
            //query the user table and make user the user exist
            string userQuery = String.Format(@"Select Username, IsActive, Email FROM User WHERE Id = '{0}' ",
                                 newUserId);

            var results = await client.QueryAsync<User>(userQuery);

            if (results.TotalSize == 0)
            {
                userMessage.Text = "There is no user with that ID. Please try again.";
                return;
            }

            //set the username, email and title label
            userNameLabel.Text = results.Records[0].UserName;
            userEmailLabel.Text = results.Records[0].Email;
            userActiveLabel.Text = results.Records[0].IsActive.ToString();
            newOwnerID = newUserId;

  
        }



        private async void button3_Click(object sender, EventArgs e)
        {
            //create a client 
            var client = new ForceClient(instanceUrl, AccessToken, ApiVersion);
            //query for the manual types and account id match
            string query = String.Format(@"Select Id, AccountId, UserOrGroupId, AccountAccessLevel, 
                                OpportunityAccessLevel, CaseAccessLevel, ContactAccessLevel, RowCause, 
                                LastModifiedDate, LastModifiedById, IsDeleted FROM AccountShare 
                            WHERE  RowCause = 'Manual' 
                            AND AccountId = '{0}'",
                            AccountID);

            var results = await client.QueryAsync<AccountShare>(query);
            //if there is no account share for this account, leave
            if (results.TotalSize == 0)
            {
                userMessage.Text = "There are no accountshare records for this account.";
                return;
            }

            //update the owner
            userMessage.Text = "Updating Owner!";
            var success = await client.UpdateAsync("Account", AccountID, new { OwnerId = newOwnerID});
            //check for susscess
            if (!string.IsNullOrEmpty(success.Errors.ToString()))
            {
                userMessage.Text ="Failed to update record owner";
                return;
            }
            
            //message to the user 
            userMessage.Text = "Successfully updated record owner";
            
            //write to the file account id - account name - owner - new owner - new owner id - records before 
            writeToFile(AccountID, accountName, newOwnerID);
           
            //create account share objects from all of the ones we have saved in memory 
            userMessage.Text = "Transferring Rercords.";
            foreach (AccountShare share in results.Records)
            {

               // share.print();
                var newShare = new AccountShare { 
                AccountId = share.AccountId,
                UserOrGroupId = share.UserOrGroupId,
                AccountAccessLevel = share.AccountAccessLevel,
                OpportunityAccessLevel = share.OpportunityAccessLevel,
                CaseAccessLevel = share.CaseAccessLevel,
                ContactAccessLevel = share.ContactAccessLevel,
                RowCause = share.RowCause,
               // IsDeleted = share.IsDeleted
                };
                writeToFile(share);
                //newShare.print();
                await client.CreateAsync(AccountShare.SObjectTypeName, newShare);
               
            }
            userMessage.Text = "Transferring Finished.";
        }

        //write the account share information to a file on the user desktop
        //TODO
        private object writeToFile(AccountShare share)
        {
            throw new NotImplementedException();
        }

        //write the account and user information to a file 
        //TODO 
        private void writeToFile(string AccountID, string accountName, string newOwnerID)
        {
            throw new NotImplementedException();
        }


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
          //  public String LastModifiedDate { get; set; }
          //  public String LastModifiedById { get; set; }
           // public String IsDeleted { get; set; }

            public void print()
            {
                Console.WriteLine(Id);
                Console.WriteLine(AccountId);
                Console.WriteLine(UserOrGroupId);
                Console.WriteLine(AccountAccessLevel);
                Console.WriteLine(OpportunityAccessLevel);
                Console.WriteLine(CaseAccessLevel);
                Console.WriteLine(ContactAccessLevel);
                Console.WriteLine(RowCause);
              //  Console.WriteLine(LastModifiedById);
             //   Console.WriteLine(LastModifiedDate);
              //  Console.WriteLine(IsDeleted);
            }
        }



        public class Account
        {
            public const String SObjectTypeName = "Account";
            public String Name { get; set; }
            public String OwnerId { get; set; }
            public String Type { get; set; }
            public String SAP_Account_Number__c  { get; set; }

        }

        public class User
        {
            public const String SObjectTypeName = "User";
            public String UserName { get; set; }
            public Boolean IsActive  { get; set; }
            public String Email { get; set; }

        }

     


    }
}
