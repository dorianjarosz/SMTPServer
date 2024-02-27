using MimeKit;
using SMTPReceiver.Data.Entities;
using SMTPReceiver.Repositories;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMTPReceiver.Services
{
    public class SmtpReceiverService : ISmtpReceiverService
    {
        private readonly int port = 7777;
        private readonly IOneSourceRepository _oneSourceRepository;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        Dictionary<string, string> toList = new Dictionary<string, string>();

        public SmtpReceiverService(IOneSourceRepository oneSourceRepository,ILogger<Worker> logger, IConfiguration configuration)
        {
            _oneSourceRepository = oneSourceRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public Task<TcpListener> Start()
        {
            var listener = new TcpListener(IPAddress.Any, port);

            listener.Start();

            _logger.LogInformation($"SMTP Receiver is listening on port {port}");

            return Task.FromResult(listener);
        }

        public async Task HandleClientAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                using (NetworkStream stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                using (var writer = new StreamWriter(stream, Encoding.ASCII))
                {
                    string totCount = "";
                    string emailContent;

                    while ((emailContent = await reader.ReadToEndAsync()) != null)
                    {
                        var message = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(emailContent)));

                        totCount = message.To.Count().ToString();

                        string createdTimestamp = GetHeaderValue(message.Headers, "CreatedTimestamp");

                        var createdTimestampDate = DateTime.Parse(createdTimestamp);

                        string mailDir = _configuration["mailDirectory"];

                        string fileName = GetFileNameFromSessionInfo(mailDir, createdTimestampDate);

                        _logger.LogInformation("Start receiving mail: {0}", fileName);

                        await message.WriteToAsync(fileName);

                        _logger.LogInformation("Saved mail to '{0}'.", fileName);

                        string toEmailSingle="";

                        MailboxAddress from = (MailboxAddress)message.From[0];
                        string fromEmail = from.Address;

                        List<string> toEmail = new List<string>();

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
                                        EmlPath= Path.GetFileName(fileName).Replace("'", "''"),
                                        From= fromEmail,
                                        To= toEmailSingle,
                                        Subject= message.Subject.Replace("'", "''"),
                                        Mode= "SMTP IN - CRASH",
                                        RuleName=ex.Message,
                                        IsEnabled=false
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
                        }
                    }

                    string response = "250 OK";
                    await writer.WriteAsync(response);
                    await writer.FlushAsync();
                }

                client.Close();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "SMTP Receiver: Operation cancelled.");
                throw;
            }
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
