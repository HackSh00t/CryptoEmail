using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using S22.Imap;
using System.Security.Cryptography;

namespace CryptoEmail
{
    public partial class Form1 : Form
    {
        static Form1 f;
        public Form1()
        {
            InitializeComponent();
            f = this;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var message = new MailMessage(txtEmail.Text, txtRecipient.Text);
            message.Subject = txtSubject.Text;
            message.Body = txtBody.Text;

            using (SmtpClient mailer = new SmtpClient("smtp.gmail.com", 587))
            {
                mailer.Credentials = new NetworkCredential(txtEmail.Text, txtPassword.Text);
                mailer.EnableSsl = true;
                mailer.Send(message);
            }

            txtSubject.Text = null;
            txtBody.Text = null;
            txtBody.Text = null;
        }

        private void btnReceive_Click(object sender, EventArgs e)
        {
            StartReceiving();
            txtBody.Text = "";
        }

        private void StartReceiving()
        {
            Task.Run(() =>
            {
                using (ImapClient client = new ImapClient("imap.gmail.com", 993, txtEmail.Text,
                    txtPassword.Text, AuthMethod.Login, true))
                {
                    if (client.Supports("IDLE") == false)
                    {
                        MessageBox.Show("Server does not support IMAP IDLE");
                        return;
                    }
                    client.NewMessage += new EventHandler<IdleMessageEventArgs>(OnNewMessage);
                    while (true) ;
                }
            });
        }

        static void OnNewMessage(object sender, IdleMessageEventArgs e)
        {
            MessageBox.Show("New message recieved!");
            MailMessage m = e.Client.GetMessage(e.MessageUID, FetchOptions.Normal);
            f.Invoke((MethodInvoker)delegate
            {
                f.txtReceive.AppendText(m.Body + "\n");
            });
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] data = UTF8Encoding.UTF8.GetBytes(txtBody.Text);
                using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                {
                    byte[] keys = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(txtHash.Text));
                    using (TripleDESCryptoServiceProvider tripDes = new TripleDESCryptoServiceProvider() { Key = keys, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7 })
                    {
                        ICryptoTransform transform = tripDes.CreateEncryptor();
                        byte[] results = transform.TransformFinalBlock(data, 0, data.Length);
                        txtBody.Text = Convert.ToBase64String(results, 0, results.Length);
                    }
                }
            }
            catch
            {
                MessageBox.Show("ERROR when Encrypting!");
            }
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] data = Convert.FromBase64String(txtReceive.Text);
                using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                {
                    byte[] keys = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(txtHash.Text));
                    using (TripleDESCryptoServiceProvider tripDes = new TripleDESCryptoServiceProvider() { Key = keys, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7 })
                    {
                        ICryptoTransform transform = tripDes.CreateDecryptor();
                        byte[] results = transform.TransformFinalBlock(data, 0, data.Length);
                        txtReceive.Text = UTF8Encoding.UTF8.GetString(results);
                    }
                }
            }
            catch
            {
                MessageBox.Show("ERROR when Decrypting!");
            }
        }
    }
}
