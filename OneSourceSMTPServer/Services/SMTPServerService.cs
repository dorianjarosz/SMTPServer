﻿using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using MimeKit;
using Hangfire;
using OneSourceSMTPServer.Data.Entities;
using OneSourceSMTPServer.Repositories;
using System.Net;
using Hangfire.MAMQSqlExtension;
using OneSourceSharedLibrary;
using Hangfire.Common;

namespace OneSourceSMTPServer.Services
{
    public class SMTPServerService : ISMTPServerService
    {
        private bool serviceStarted = false;
        private readonly int port = 25;
        private readonly IOneSourceRepository _oneSourceRepository;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<SMTPServerService> _logger;
        private readonly IConfiguration _configuration;
        private TcpListener listener;
        private static Queue<MimeMessage> emailMessageQueue = new Queue<MimeMessage>();

        public SMTPServerService(IOneSourceRepository oneSourceRepository, ILogger<SMTPServerService> logger, IConfiguration configuration, IRecurringJobManager recurringJobManager)
        {
            _oneSourceRepository = oneSourceRepository;
            _logger = logger;
            _configuration = configuration;
            _recurringJobManager = recurringJobManager;
        }

        public async Task HandleClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _oneSourceRepository.MigrateDatabaseAsync();

                listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!serviceStarted)
                    {
                        _logger.LogInformation("Starting handling email messages hangfire task.");

                        HangfireJobs.handleEmailMessages = HandleEmailMessages;

                        HangfireJobs.deleteOldEmailMessages = DeleteOldEmailsAndLogs;

                        _recurringJobManager.AddOrUpdate<HangfireJobs>(
                            "handle-email-messages", (job) => job.HandleEmailMessages(),
                            _configuration["SMTPReceiver:HandleEmailIntervalCronExpression"], new RecurringJobOptions
                            {
                                QueueName = "onesource_main_queue"
                            }
                        );

                        _logger.LogInformation("Started handling email messages hangfire task.");

                        _logger.LogInformation("Starting deleting old email and logs hangfire task.");

                        _recurringJobManager.AddOrUpdate<HangfireJobs>(
                            "delete-old-emails-and-logs",
                            (job) => job.DeleteOldEmailMessages(),
                            _configuration["DataRetentionPolicy:DeleteOldLogsAndEmailsCronExpression"], new RecurringJobOptions
                            {
                                QueueName = "onesource_main_queue"
                            }
                        );

                        _logger.LogInformation("Started deleting old email and logs hangfire task.");

                        _logger.LogInformation("Started TCP listener to listen for incoming emails from Test1");

                        _logger.LogInformation("Started listening on TCP.");

                        _logger.LogInformation("Started TCP listener to listen for incoming emails from Test1");

                        _ = EnqueueEmailMessage(cancellationToken);

                        serviceStarted = true;
                    }

                    await Task.Delay(1000 * 60);
                }
            }
            catch (Exception ex)
            {
                var smtpErrorLog = new SMTPLog
                {
                    EmlPath = "N/A",
                    From = "N/A",
                    To = "N/A",
                    Subject = "N/A",
                    Mode = "SMTP IN - CRASH",
                    RuleName = ex.Message + " ------ " + ex.StackTrace,
                    IsEnabled = false
                };

                await _oneSourceRepository.AddAsync(smtpErrorLog);

                _logger.LogError(ex, "SMTP Server: Operation failed.");
            }
            finally
            {
                var smtpErrorLog = new SMTPLog
                {
                    EmlPath = "N/A",
                    From = "N/A",
                    To = "N/A",
                    Subject = "N/A",
                    Mode = "SMTP IN - CRASH",
                    RuleName = "Stopped TCP listener for listening for incoming emails from atosonesource.com.",
                    IsEnabled = false
                };

                await _oneSourceRepository.AddAsync(smtpErrorLog);

                _logger.LogInformation("Stopped TCP listener for listening for incoming emails from atosonesource.com.");
            }
        }

        public void StopListener()
        {
            listener.Stop();
        }

        private async Task EnqueueEmailMessage(CancellationToken cancellationToken)
        {
            listener.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Started listening on TCP.");

                    var client = await listener.AcceptTcpClientAsync();

                    _logger.LogInformation("Intercepted an email message. Enqueuing the message started.");

                    using (NetworkStream networkStream = client.GetStream())
                    {
                        _logger.LogInformation("Opening the network stream.");

                        using (var reader = new StreamReader(networkStream, Encoding.ASCII))
                        {
                            using (var writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true })
                            {
                                await writer.WriteLineAsync("220 OneSourceSMTPServer");

                                _logger.LogInformation("Opening the stream in the stream reader.");

                                var emailBuilder = new StringBuilder();

                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    string line = await reader.ReadLineAsync();

                                    if (line == null)
                                        break;

                                    _logger.LogInformation($"Line that has been read: {line}");

                                    if (line.StartsWith("EHLO") || line.StartsWith("HELO"))
                                    {
                                        await writer.WriteLineAsync("250 Hello");
                                    }
                                    else if (line.StartsWith("MAIL FROM"))
                                    {
                                        await writer.WriteLineAsync("250 OK");
                                    }
                                    else if (line.StartsWith("RCPT TO"))
                                    {
                                        await writer.WriteLineAsync("250 OK");
                                    }
                                    else if (line.StartsWith("DATA"))
                                    {
                                        await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                                        while (true)
                                        {
                                            line = await reader.ReadLineAsync();
                                            if (line == ".")
                                                break;
                                            emailBuilder.AppendLine(line);
                                        }

                                        _logger.LogInformation($"Email subject to parse: {emailBuilder}");

                                        var email = ParseEmail(emailBuilder.ToString());

                                        _logger.LogInformation($"Parsed email subject: {email.Subject}");

                                        _logger.LogInformation($"Enqueued the following email message: {email}");

                                        emailMessageQueue.Enqueue(email);

                                        _logger.LogInformation("Enqueuing the email message ended.");

                                        emailBuilder.Clear();

                                        await writer.WriteLineAsync("250 OK");
                                    }
                                    else if (line.StartsWith("QUIT"))
                                    {
                                        await writer.WriteLineAsync("221 Bye");
                                        break;
                                    }
                                    else
                                    {
                                        _logger.LogInformation("500 Command not recognized.");

                                        await writer.WriteLineAsync("500 Command not recognized");
                                    }
                                }
                            }

                            _logger.LogInformation("Closing the stream in the stream reader.");
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    var smtpErrorLog = new SMTPLog
                    {
                        EmlPath = "N/A",
                        From = "N/A",
                        To = "N/A",
                        Subject = "N/A",
                        Mode = "SMTP IN - CRASH",
                        RuleName = ex.Message + " ------ " + ex.StackTrace,
                        IsEnabled = false
                    };

                    await _oneSourceRepository.AddAsync(smtpErrorLog);

                    listener.Start();

                    _logger.LogError(ex, "Operation cancelled. Resuming the operation.");
                }
                catch (FormatException ex)
                {
                    var smtpErrorLog = new SMTPLog
                    {
                        EmlPath = "N/A",
                        From = "N/A",
                        To = "N/A",
                        Subject = "N/A",
                        Mode = "SMTP IN - CRASH",
                        RuleName = ex.Message + " ------ " + ex.StackTrace,
                        IsEnabled = false
                    };

                    await _oneSourceRepository.AddAsync(smtpErrorLog);

                    _logger.LogError(ex, "Format exception");
                    throw;
                }
                catch (Exception ex)
                {
                    var smtpErrorLog = new SMTPLog
                    {
                        EmlPath = "N/A",
                        From = "N/A",
                        To = "N/A",
                        Subject = "N/A",
                        Mode = "SMTP IN - CRASH",
                        RuleName = ex.Message + " ------ " + ex.StackTrace,
                        IsEnabled = false
                    };

                    await _oneSourceRepository.AddAsync(smtpErrorLog);

                    _logger.LogError(ex, "SMTP Server: Operation failed. Resuming the operation.");

                    listener.Start();
                }
            }
        }

        private async Task HandleEmailMessages()
        {
            while (emailMessageQueue.TryPeek(out var emailMessage))
            {
                string mailDir = _configuration["SMTPReceiver:ReceivedEmailsDirectory"];

                try
                {
                    _logger.LogInformation("HandleEmailMessages job: Started handling email received messages.");

                    emailMessageQueue.Dequeue();

                    var message = emailMessage;

                    string createdTimestamp = GetHeaderValue(message.Headers, "CreatedTimestamp");

                    var createdTimestampDate = !string.IsNullOrWhiteSpace(createdTimestamp) ? DateTime.Parse(createdTimestamp) : DateTime.Now;

                    string fileName = GetFileNameFromSessionInfo(mailDir, createdTimestampDate);

                    _logger.LogInformation("HandleEmailMessages job: Start receiving mail: {0}", fileName);

                    await message.WriteToAsync(fileName);

                    _logger.LogInformation("HandleEmailMessages job: Saved mail to '{0}'.", fileName);

                    string toEmailSingle = "";

                    MailboxAddress from = (MailboxAddress)message.From[0];
                    string fromEmail = from.Address;

                    List<string> toEmail = new List<string>();

                    _logger.LogInformation("HandleEmailMessages job: Saving emails for recipients.");

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
                                    RuleName = ex.Message + " ------ " + ex.StackTrace,
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
                                    RuleName = ex.Message + " ------ " + ex.StackTrace,
                                    IsEnabled = false
                                };

                                await _oneSourceRepository.AddAsync(smtpLog);
                                throw;
                            }
                        }
                    }

                    _logger.LogInformation("HandleEmailMessages job: Saved emails for recipients successfully.");

                    _logger.LogInformation("HandleEmailMessages job: Saving emails for CC recipients.");

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
                                    RuleName = ex.Message + " ------ " + ex.StackTrace,
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
                                    RuleName = ex.Message + " ------ " + ex.StackTrace,
                                    IsEnabled = false
                                };

                                await _oneSourceRepository.AddAsync(smtpLog);
                                throw;
                            }
                        }
                    }

                    _logger.LogInformation("HandleEmailMessages job: Saved emails for CC recipients successfully.");

                    _logger.LogInformation("HandleEmailMessages job: Saving emails for senders.");

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

                    _logger.LogInformation("HandleEmailMessages job: Recipient emails: " + string.Join(", ", toEmail));

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

                        var mappings = await _oneSourceRepository.GetAsync<MappingSMTPReceiver>(m => m.ToEmail.ToLower() == toEmailAux.ToLower());

                        foreach (var mapping in mappings)
                        {
                            string destInstance = mapping.DestinationInstance;

                            if (!string.IsNullOrWhiteSpace(destInstance))
                            {
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

                                        string apiUrl;

                                        if (mapping.DestinationInstanceVersion.ToLower() == "v3")
                                        {
                                            apiUrl = "/api/SMTPReceiver";
                                        }
                                        else
                                        {
                                            apiUrl = "/Admin/SMTPReceiver.aspx?lng=EN&mode=" + mapping.Mode;
                                        }

                                        string url = destInstance + apiUrl;

                                        if (destInstance.Trim() == "52.211.50.245") //dev without https
                                        {
                                            url = destInstance + apiUrl;
                                        }

                                        HttpResponseMessage remoteServerResponse = null;

                                        try
                                        {
                                            _logger.LogInformation("HandleEmailMessages job: Forwarding the email.");

                                            var json = new
                                            {
                                                mode = mapping.Mode,
                                                fromEmail = fromEmail,
                                                toEmail = toEmailAux,
                                                subject = message.Subject,
                                                originalEML = Path.GetFileName(fileName),
                                                MessageContent = contentEncoded
                                            };

                                            if (mapping.DestinationInstanceVersion.ToLower() == "v3")
                                            {
                                                var jsonString = JsonConvert.SerializeObject(json);

                                                StringContent fullcontent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                                                remoteServerResponse = await httpClient.PostAsync(url, fullcontent);
                                            }
                                            else
                                            {
                                                var fullcontent = new MultipartFormDataContent();

                                                var pairs = new List<KeyValuePair<string, string>>
                                                {
                                                    new KeyValuePair<string, string>("MessageContent", json.MessageContent),
                                                    new KeyValuePair<string, string>("fromEmail", json.fromEmail),
                                                    new KeyValuePair<string, string>("toEmail", json.toEmail),
                                                    new KeyValuePair<string, string>("subject", json.subject),
                                                    new KeyValuePair<string, string>("originalEML", json.originalEML)
                                                };

                                                foreach (var pair in pairs)
                                                {
                                                    if (pair.Key != null && pair.Value != null)
                                                    {
                                                        fullcontent.Add(new StringContent(pair.Value), pair.Key);
                                                    }
                                                }

                                                remoteServerResponse = await httpClient.PostAsync(url, fullcontent);
                                            }

                                            if (remoteServerResponse.IsSuccessStatusCode)
                                            {
                                                _logger.LogInformation("HandleEmailMessages job: Successfully forwarded the email.");

                                                string result = remoteServerResponse.Content.ToString();
                                            }
                                            else
                                            {
                                                _logger.LogError("HandleEmailMessages job: "+remoteServerResponse.StatusCode + " has been returned. " + (remoteServerResponse.Content?.ToString() ?? null) + " from the URL: "+ url);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, $"HandleEmailMessages job: An error occured. Sender email: {fromEmail}, recipient email: {toEmailAux}, email file: {Path.GetFileName(fileName)}");
                                            throw;
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
                                    var smtpErrorLog = new SMTPLog
                                    {
                                        EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                        From = fromEmail.Replace("'", "''"),
                                        To = toEmailSingle.Replace("'", "''"),
                                        Subject = message.Subject.Replace("'", "''"),
                                        Mode = "SMTP IN - CRASH",
                                        RuleName = ex.Message + " ------ "+ ex.StackTrace,
                                        IsEnabled = false
                                    };

                                    await _oneSourceRepository.AddAsync(smtpErrorLog);

                                    _logger.LogError(ex, $"HandleEmailMessages job: An error occured. Sender email: {fromEmail}, recipient email: {toEmailAux}, email file: {Path.GetFileName(fileName)}");
                                    throw;
                                }

                            }
                        }
                    }

                    _logger.LogInformation("HandleEmailMessages job: Handling the following email message: " + emailMessage.Subject);
                }
                catch (Exception ex)
                {
                    var smtpErrorLog = new SMTPLog
                    {
                        EmlPath = "N/A",
                        From = "N/A",
                        To = "N/A",
                        Subject = "N/A",
                        Mode = "SMTP IN - CRASH",
                        RuleName = ex.Message + " ------ " + ex.StackTrace,
                        IsEnabled = false
                    };

                    await _oneSourceRepository.AddAsync(smtpErrorLog);

                    _logger.LogError(ex, "HandleEmailMessages job: Service error occured");
                }
            }

            _logger.LogInformation("HandleEmailMessages job: Ended handling email received messages.");
        }

        private async Task DeleteOldEmailsAndLogs()
        {
            try
            {
                _logger.LogInformation("DeleteOldEmailsAndLogs job: Started deleting old emails and logs.");

                string mailDir = _configuration["SMTPReceiver:ReceivedEmailsDirectory"];

                string timeForPersistingDataInMonthsValue = _configuration["DataRetentionPolicy:TimeForPersistingDataInMonths"];

                int timeForPersistingDataInMonths = int.Parse(timeForPersistingDataInMonthsValue);

                var latestDateToPersistData = DateTime.Now.AddMonths(-timeForPersistingDataInMonths);

                var logsToDeltete = await _oneSourceRepository.GetAsync<SMTPLog>(l => l.LastUpdate < latestDateToPersistData);

                _logger.LogInformation("DeleteOldEmailsAndLogs job: Started deleting old SMTP logs.");

                if (logsToDeltete.Count > 0)
                {
                    await _oneSourceRepository.RemoveRangeAsync(logsToDeltete);
                }

                _logger.LogInformation("DeleteOldEmailsAndLogs job: Finished deleting old SMTP logs.");

                _logger.LogInformation("DeleteOldEmailsAndLogs job: Started deleting old EML email files.");

                string[] files = Directory.GetFiles(mailDir);

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);

                    if (fi.CreationTime < latestDateToPersistData)
                    {
                        fi.Delete();
                    }
                }

                _logger.LogInformation("DeleteOldEmailsAndLogs job: Finished deleting old EML email files.");
            }
            catch (Exception ex)
            {
                var smtpErrorLog = new SMTPLog
                {
                    EmlPath = "N/A",
                    From = "N/A",
                    To = "N/A",
                    Subject = "N/A",
                    Mode = "SMTP IN - CRASH",
                    RuleName = ex.Message + " ------ " + ex.StackTrace,
                    IsEnabled = false
                };

                await _oneSourceRepository.AddAsync(smtpErrorLog);

                _logger.LogError(ex, "DeleteOldEmailsAndLogs job: Service error occured");
            }
        }

        private MimeMessage ParseEmail(string rawData)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(rawData.Replace("\r\n", "\n").Replace("\n", "\r\n")));
            return MimeMessage.Load(stream);
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
            var fullName = Path.Combine(mailDir, fileName);
            return fullName;
        }
    }
}
