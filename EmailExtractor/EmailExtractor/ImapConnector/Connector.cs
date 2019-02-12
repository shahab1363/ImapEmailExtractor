using EmailExtractor.DatabaseModel;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using MimeKit;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace EmailExtractor.ImapConnector
{
    public class Connector : IConnector
    {
        private readonly ILogger<Connector> logger;
        //private readonly ImapClient client;
        private readonly string username;
        private readonly string password;
        private readonly int port;
        private readonly bool imapSecure;
        private readonly string server;
        public string ServerKey => $"{username}@{server}";

        public Connector(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<Connector>();

            logger.LogTrace($"Constructor Called");

            logger.LogDebug($"Imap:Credentials : {configuration["Imap:Credentials"]}");

            var userpass = configuration["Imap:Credentials"].Split("::", 2, StringSplitOptions.RemoveEmptyEntries);

            username = userpass[0];
            logger.LogDebug($"Imap:Credentials->Username : {username}");
            password = userpass[1];
            logger.LogDebug($"Imap:Credentials->Password : {password}");

            server = configuration["Imap:Server"];
            logger.LogDebug($"Imap:Server : {server}");

            if (!bool.TryParse(configuration["Imap:SSL"], out imapSecure))
                imapSecure = false;
            if (!Int32.TryParse(configuration["Imap:Port"], out port))
                port = imapSecure ? 993 : 143;

            logger.LogDebug($"Imap:SSL : {imapSecure}");
            logger.LogDebug($"Imap:Port : {port}");
        }

        private void Connect(ImapClient client)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            if (!client.IsConnected)
            {
                logger.LogTrace($"Connecting");
                client.Connect(server, port, imapSecure);
                logger.LogInformation($"Connected");

            }

            if (!client.IsAuthenticated)
            {
                logger.LogTrace($"Authenticating");
                client.Authenticate(username, password);
                logger.LogInformation($"Authenticated");
            }
        }


        public IEnumerable<IMailFolder> Folders(IMailFolder rootFolder, bool recursive)
            => Folders(rootFolder, recursive, null);

        private IEnumerable<IMailFolder> Folders(IMailFolder rootFolder, bool recursive, ImapClient client)
        {
            if (client == null)
            {
                client = new ImapClient();
                Connect(client);
            }

            IMailFolder parentFolder = rootFolder ?? client.GetFolder(client.PersonalNamespaces[0]);

            foreach (var folder in parentFolder.GetSubfolders(false))
            {
                logger.LogTrace($"return folder: {folder.FullName}");
                yield return folder;

                if (recursive)
                {
                    var internalFolders = Folders(folder, recursive, client);
                    foreach (var internalFolder in internalFolders)
                    {
                        logger.LogTrace($"return folder: {internalFolder.FullName}");
                        yield return internalFolder;
                    }
                }
            }
        }

        public List<MailItem> ExtractMailItems(IMailFolder folder, int pageSize, int page, List<uint> existingUids)
        {
            try
            {
                using (var client = new ImapClient())
                {
                    Connect(client);
                    if (!folder.IsOpen)
                    {
                        logger.LogTrace("Open Folder");
                        folder.Open(FolderAccess.ReadOnly);
                    }

                    logger.LogInformation($"Fetch summaries [{folder.FullName}] - \tPage: {page} - Page Size: {pageSize}");
                    var summaries = folder.Fetch(page * pageSize, (page + 1) * pageSize - 1, MessageSummaryItems.UniqueId);
                    logger.LogInformation($"Got summaries [{folder.FullName}] - Count: {summaries.Count}");

                    if (summaries.Count == 0)
                        return null;

                    var result = new List<MailItem>();
                    logger.LogTrace("Extract Mail Addresses");

                    foreach (var summary in summaries)
                    {
                        if (!existingUids.Contains(summary.UniqueId.Id))
                        {
                            var message = folder.GetMessage(summary.UniqueId);
                            result.AddRange(CreateMailItems(logger, message, this, folder, summary.UniqueId));
                        }
                    }

                    if (folder.IsOpen)
                    {
                        logger.LogTrace("Close Folder");
                        folder.Close(false);
                    }

                    return result;
                }

            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error in folder: {folder.FullName} \r\n {e.Message}");
                return null;
            }
        }

        private static IEnumerable<MailItem> CreateMailItems(ILogger logger, MimeMessage message, IConnector connector, IMailFolder folder, UniqueId uniqueId)
        {
            string messageEml = null;
            try
            {
                var stream = new MemoryStream();
                message.WriteTo(stream);
                stream.Position = 0;
                //var reader = new StreamReader(stream,Encoding.UTF8, true);
                var reader = new StreamReader(stream);
                messageEml = reader.ReadToEnd();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Serialization failed");
            }

            DateTimeOffset date = DateTimeOffset.MinValue;
            try
            {
                date = message.Date;
            }
            catch 
            {
            }

            foreach (var address in message.From)
            {
                if (address is MailboxAddress mailboxAddress)
                    yield return MailItem.CreateMailItem(mailboxAddress.Address, mailboxAddress.Name,
                        connector.ServerKey, folder.FullName, uniqueId.Id, messageEml, date);
            }
        }


    }
}
