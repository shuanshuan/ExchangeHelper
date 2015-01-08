using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Exchange.WebServices.Data;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Configuration;




namespace ExchangeMeetingTester
{
    public partial class ExHelper : Form
    {
        ExchangeService service;
        public ExHelper()
        {
            InitializeComponent();
            try
            {
                this.tbEmail.Text = ConfigurationManager.AppSettings["Email"];
                this.tbPwd.Text = ConfigurationManager.AppSettings["Password"];
                string folders = ConfigurationManager.AppSettings["Folders"];
                this.ddFolder.DataSource = folders.Split(',');
                this.ddFolder.SelectedIndex = 0;
            }
            catch (FieldAccessException)
            {
                this.rtbMessage.AppendText("Access App.config is denied! \n");
            }
            catch (FormatException)
            {
                this.rtbMessage.AppendText("App.config is not well formated! \n");

            }

            this.btAction.Enabled = false;
        }

        private void btAction_Click(object sender, EventArgs e)
        {
            //FolderId fId = new FolderId(WellKnownFolderName.Inbox, new Mailbox(email));
            string value = this.ddFolder.SelectedItem.ToString();
            WellKnownFolderName fname = (WellKnownFolderName)Enum.Parse(typeof(WellKnownFolderName), value);
            Folder folder = Folder.Bind(service, fname);
            folder.Empty(DeleteMode.HardDelete, true);
            this.rtbMessage.AppendText("All the items in this folder has been deleted! \n");
        }

        private void ReadMeetings(DateTime startDate, DateTime endDate, FolderId fId)
        {
            //FolderId fId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email));
            CalendarFolder calendar = CalendarFolder.Bind(service, fId, new PropertySet());
            CalendarView cView = new CalendarView(startDate, endDate);
            cView.PropertySet = new PropertySet(AppointmentSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End);

            FindItemsResults<Appointment> appointments = calendar.FindAppointments(cView);

            foreach (Appointment a in appointments)
            {
                rtbMessage.AppendText("Subject: " + a.Subject.ToString() + " \n");
                rtbMessage.AppendText("Start: " + a.Start.ToString() + " \n");
                rtbMessage.AppendText("End: " + a.End.ToString() + "\n");
            }
        }

        private ExchangeService ServiceIni(string email, string password)
        {
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2010_SP2);
            service.Credentials = new WebCredentials(email, password);
            service.UseDefaultCredentials = false;
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;
            service.Timeout = int.Parse(ConfigurationManager.AppSettings["TimeOut"]);
            service.AutodiscoverUrl(email, RedirectionUrlValidationCallback);
            return service;
        }


        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }

        private static bool CertificateValidationCallBack(
                object sender,
                System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain != null && chain.ChainStatus != null)
                {
                    foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus)
                    {
                        if ((certificate.Subject == certificate.Issuer) &&
                           (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot))
                        {
                            // Self-signed certificates with an untrusted root are valid. 
                            continue;
                        }
                        else
                        {
                            if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
                            {
                                // If there are any other errors in the certificate chain, the certificate is invalid,
                                // so the method returns false.
                                return false;
                            }
                        }
                    }
                }

                // When processing reaches this line, the only errors in the certificate chain are 
                // untrusted root errors for self-signed certificates. These certificates are valid
                // for default Exchange server installations, so return true.
                return true;
            }
            else
            {
                // In all other cases, return false.
                return false;
            }
        }

        private void roomList_Click(object sender, EventArgs e)
        {

            // Return all the room lists in the organization.
            EmailAddressCollection myRoomLists = service.GetRoomLists();

            // Display the room lists.
            foreach (EmailAddress address in myRoomLists)
            {
                string t = string.Format("Email Address: {0} Mailbox Type: {1} \n", address.Address, address.MailboxType);
                this.rtbMessage.AppendText(t);
            }




        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            string dis = "disConnect";
            if (this.btConnect.Text != dis)
            {
                this.btConnect.Text = dis;
                Connecting();
            }
            else {
                this.btConnect.Text = "Connect";
                this.btAction.Enabled = false;
                service = null;
            }
            
        }

        private void Connecting()
        {

            this.rtbMessage.AppendText("Connecting \n");
            string email = this.tbEmail.Text;
            string pwd = this.tbPwd.Text;
            try
            {
                service = ServiceIni(email, pwd);
                this.rtbMessage.AppendText("Connected! \n");
                this.btAction.Enabled = true;
            }
          //  catch (FormatException)
         //   {
           //     MessageBox.Show("It's not a well format email address!");
         //   }
            catch (TimeoutException)
            {
                MessageBox.Show("Connection Time out!");
            }
            catch (AutodiscoverLocalException)
            {
                MessageBox.Show("Bad username or password!");
            }
        }

  

    }
}
