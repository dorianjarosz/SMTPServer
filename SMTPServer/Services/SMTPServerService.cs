﻿using Hangfire;
using MimeKit;
using SMTPServer.Data.Entities;
using SMTPServer.Repositories;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMTPServer.Services
{
    public class SMTPServerService : ISMTPServerService
    {
        private readonly int port = 26;
        private readonly IOneSourceRepository _oneSourceRepository;
        private readonly ILogger<SMTPServerService> _logger;
        private readonly IConfiguration _configuration;

        private Queue<MimeMessage> emailMessageQueue = new Queue<MimeMessage>();

        public SMTPServerService(IOneSourceRepository oneSourceRepository, ILogger<SMTPServerService> logger, IConfiguration configuration)
        {
            _oneSourceRepository = oneSourceRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task HandleClientAsync(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(IPAddress.Any, port);

            try
            {
                RecurringJob.AddOrUpdate(
                    "emailMessagesHandler",
                    () => HandleEmailMessages(CancellationToken.None),
                    "*/5 * * * * *"
                );

                listener.Start();

                _logger.LogInformation("Started TCP listener to listen for incoming emails from atosonesource.com.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    await EnqueueEmailMessage(client);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Server: Operation failed.");
            }
            finally
            {
                listener.Stop();

                _logger.LogInformation("Stopped TCP listener for listening for incoming emails from atosonesource.com.");
            }
        }

        private async Task EnqueueEmailMessage(TcpClient client)
        {
            _logger.LogInformation("Intercepted an email message. Enqueuing the message started.");

            using (NetworkStream stream = client.GetStream())
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    string emailContent;

                    while ((emailContent = await reader.ReadToEndAsync()) != null)
                    {
                        var message = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(emailContent)));

                        emailMessageQueue.Enqueue(message);
                    }
                }

                client.Close();
            }

            _logger.LogInformation("Enqueuing the email message ended.");
        }

        public async Task HandleEmailMessages(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Started handling email received messages.");

            while (emailMessageQueue.TryPeek(out var emailMessage))
            {
                var message = emailMessage;

                string createdTimestamp = GetHeaderValue(message.Headers, "CreatedTimestamp");

                var createdTimestampDate = DateTime.Parse(createdTimestamp);

                string mailDir = _configuration["mailDirectory"];

                string fileName = GetFileNameFromSessionInfo(mailDir, createdTimestampDate);

                _logger.LogInformation("Start receiving mail: {0}", fileName);

                await message.WriteToAsync(fileName);

                _logger.LogInformation("Saved mail to '{0}'.", fileName);

                string toEmailSingle = "";

                MailboxAddress from = (MailboxAddress)message.From[0];
                string fromEmail = from.Address;

                List<string> toEmail = new List<string>();

                _logger.LogInformation("Saving emails for recipients.");

                foreach (InternetAddress mailAux in message.To)
                {
                    if (mailAux.GetType() == new GroupAddress("test").GetType())
                    {
                        GroupAddress grp = (GroupAddress)mailAux;
                        try
                        {
                            if (grp != null && grp.Members != null && grp.Members.Count > 0)
                            {
                                foreach (MailboxAddress mlAux in grp.Members)
                                {
                                    MailboxAddress frm = mlAux;
                                    toEmailSingle = frm.Address; //For use in exception if it's necessary
                                    if (!toEmail.Contains(frm.Address))
                                    {
                                        toEmail.Add(frm.Address);
                                    }

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var smtpLog = new SMTPLog
                            {
                                EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                From = fromEmail,
                                To = toEmailSingle,
                                Subject = message.Subject.Replace("'", "''"),
                                Mode = "SMTP IN - CRASH",
                                RuleName = ex.Message,
                                IsEnabled = false
                            };

                            await _oneSourceRepository.AddAsync(smtpLog);
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            MailboxAddress frm = (MailboxAddress)mailAux;
                            toEmailSingle = frm.Address; //For use in exception if it's necessary
                            if (!toEmail.Contains(frm.Address))
                            {
                                toEmail.Add(frm.Address);
                            }
                        }
                        catch (Exception ex)
                        {
                            var smtpLog = new SMTPLog
                            {
                                EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                From = fromEmail,
                                To = toEmailSingle,
                                Subject = message.Subject.Replace("'", "''"),
                                Mode = "SMTP IN - CRASH",
                                RuleName = ex.Message,
                                IsEnabled = false
                            };

                            await _oneSourceRepository.AddAsync(smtpLog);
                            throw;
                        }
                    }
                }

                _logger.LogInformation("Saved emails for recipients successfully.");

                _logger.LogInformation("Saving emails for CC recipients.");

                foreach (InternetAddress iaAux in message.Cc)
                {
                    if (iaAux.GetType() == new GroupAddress("test").GetType())
                    {
                        GroupAddress grp = (GroupAddress)iaAux;
                        try
                        {
                            if (grp != null && grp.Members != null && grp.Members.Count > 0)
                            {
                                foreach (MailboxAddress mlAux in grp.Members)
                                {
                                    MailboxAddress frm = mlAux;
                                    toEmailSingle = frm.Address; //For use in exception if it's necessary
                                    if (!toEmail.Contains(frm.Address))
                                    {
                                        toEmail.Add(frm.Address);
                                    }

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var smtpLog = new SMTPLog
                            {
                                EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                From = fromEmail,
                                To = toEmailSingle,
                                Subject = message.Subject.Replace("'", "''"),
                                Mode = "SMTP IN - CRASH",
                                RuleName = ex.Message,
                                IsEnabled = false
                            };

                            await _oneSourceRepository.AddAsync(smtpLog);
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            MailboxAddress frm = (MailboxAddress)iaAux;
                            toEmailSingle = frm.Address; //For use in exception if it's necessary
                            if (!toEmail.Contains(frm.Address))
                            {
                                toEmail.Add(frm.Address);
                            }
                        }
                        catch (Exception ex)
                        {
                            var smtpLog = new SMTPLog
                            {
                                EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                From = fromEmail.Replace("'", "''"),
                                To = toEmailSingle.Replace("'", "''"),
                                Subject = message.Subject.Replace("'", "''"),
                                Mode = "SMTP IN - CRASH",
                                RuleName = ex.Message,
                                IsEnabled = false
                            };

                            await _oneSourceRepository.AddAsync(smtpLog);
                            throw;
                        }
                    }
                }

                _logger.LogInformation("Saved emails for CC recipients successfully.");

                _logger.LogInformation("Saving emails for senders.");

                string htmlBody = message.HtmlBody;

                if (htmlBody == null)
                {
                    htmlBody = message.TextBody;
                }

                if (toEmail.Count() == 0)
                {
                    toEmail.Add("");
                }

                byte[] buff = await File.ReadAllBytesAsync(fileName);
                string contentEncoded = Convert.ToBase64String(buff);

                foreach (string toEmailAux in toEmail)
                {
                    var smtpLog = new SMTPLog
                    {
                        EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                        From = fromEmail.Replace("'", "''"),
                        To = toEmailAux.Replace("'", "''"),
                        Subject = message.Subject.Replace("'", "''"),
                        Mode = "SMTP RECEIVED - PRE",
                        RuleName = "",
                        IsEnabled = false
                    };

                    await _oneSourceRepository.AddAsync(smtpLog);

                    var mappings = await _oneSourceRepository.GetAllAsync<MappingSMTPReceiver>();

                    foreach (var mapping in mappings)
                    {
                        var fullcontent = new MultipartFormDataContent();
                        var pairs = new List<KeyValuePair<string, string>>
                                    {
                                        new KeyValuePair<string, string>("fromEmail", fromEmail),
                                        new KeyValuePair<string, string>("toEmail", toEmailAux),
                                        new KeyValuePair<string, string>("subject", message.Subject),
                                        new KeyValuePair<string, string>("originalEML", Path.GetFileName(fileName)),
                                        new KeyValuePair<string, string>("menuEntryName", mapping.MenuEntryName),
                                        new KeyValuePair<string, string>("section", mapping.Section),
                                        new KeyValuePair<string, string>("category", mapping.Category),
                                        new KeyValuePair<string, string>("dataAccess", mapping.DataAccess),
                                        new KeyValuePair<string, string>("messageContent", contentEncoded)
                                    };

                        foreach (var keyValuePair in pairs)
                        {
                            if (keyValuePair.Key != null && keyValuePair.Value != null)
                            {
                                fullcontent.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                            }
                        }

                        string destInstance = "";

                        //if toEmail condition match
                        if (!string.IsNullOrWhiteSpace(mapping.ToEmail) && mapping.ToEmail == toEmailAux)
                        {
                            destInstance = mapping.DestinationInstance;

                            //if containsString condition match
                            if (!string.IsNullOrEmpty(mapping.ContainsString) && htmlBody != null && htmlBody.ToLower().Contains(mapping.ContainsString))
                            {
                                destInstance = mapping.DestinationInstance;
                            }
                            else if (mapping.ContainsString != null && mapping.ContainsString == "")
                            {
                                destInstance = mapping.DestinationInstance;
                            }
                            else if (mapping.ContainsString == null)
                            {
                                destInstance = mapping.DestinationInstance;
                            }
                            else
                            {
                                destInstance = "";
                            }

                            if (mapping.DiscardInternal != null && mapping.DiscardInternal == true && htmlBody != null && (htmlBody.Contains("Owning GBU") || htmlBody.Contains("Leading GBU")))
                            {
                                destInstance = "";
                            }

                        }

                        if (!string.IsNullOrWhiteSpace(destInstance))
                        {
                            //Indicate the process mode (MIP1, BODYSAVER, FILESAVER, BACKUPS, ...)

                            try
                            {
                                bool isEnabled = mapping.IsEnabled;


                                if (isEnabled)
                                {
                                    var smtpLog2 = new SMTPLog
                                    {
                                        EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                        From = fromEmail.Replace("'", "''"),
                                        To = toEmailAux.Replace("'", "''"),
                                        Subject = message.Subject.Replace("'", "''"),
                                        Mode = mapping.Mode,
                                        RuleName = mapping.Id + ": " + mapping.Description.Replace("'", "''") + " > " + mapping.DestinationInstance.Replace("'", "''"),
                                        IsEnabled = true
                                    };

                                    await _oneSourceRepository.AddAsync(smtpLog2);

                                    var httpClient = new HttpClient();

                                    string url = destInstance + "/api/SMTPReceiver?mode=" + mapping.Mode;

                                    if (destInstance.Trim() == "52.211.50.245") //dev without https
                                    {
                                        url = destInstance + "/api/SMTPReceiver?mode=" + mapping.Mode;
                                    }

                                    HttpResponseMessage remoteServerResponse = null;

                                    try
                                    {

                                        remoteServerResponse = httpClient.PostAsync(url, fullcontent).Result;
                                        if (remoteServerResponse.IsSuccessStatusCode)
                                        {
                                            string result = remoteServerResponse.Content.ToString();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"An error occured. Sender email: {fromEmail}, recipient email: {toEmailAux}, email file: {Path.GetFileName(fileName)}");
                                    }
                                }
                                else
                                {
                                    var smtpLog3 = new SMTPLog
                                    {
                                        EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                        From = fromEmail.Replace("'", "''"),
                                        To = toEmailAux.Replace("'", "''"),
                                        Subject = message.Subject.Replace("'", "''"),
                                        Mode = mapping.Mode,
                                        RuleName = "*** " + mapping.Id + ": " + mapping.Description.Replace("'", "''") + " > " + mapping.DestinationInstance.Replace("'", "''"),
                                        IsEnabled = false
                                    };

                                    await _oneSourceRepository.AddAsync(smtpLog3);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"An error occured. Sender email: {fromEmail}, recipient email: {toEmailAux}, email file: {Path.GetFileName(fileName)}");
                                throw;
                            }

                        }
                    }

                    emailMessageQueue.Dequeue();
                }

                _logger.LogInformation("Handling the following email message: " + emailMessage.Subject);
            }

            _logger.LogInformation("Ended handling email received messages.");
        }

        private string GetHeaderValue(IEnumerable<Header> headers, string headerName)
        {
            foreach (var header in headers)
            {
                if (header.Field.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Value;
                }
            }
            return null;
        }
        private string GetFileNameFromSessionInfo(string mailDir, DateTime createdTimestampDate)
        {
            var fileName = createdTimestampDate.ToString("yyyy-MM-dd_HHmmss_fff") + ".eml";
            var fullName = System.IO.Path.Combine(mailDir, fileName);
            return fullName;
        }
    }
}
