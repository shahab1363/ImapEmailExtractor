using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace EmailExtractor.DatabaseModel
{
    public class MailItem
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string Server { get; private set; }
        [Required]
        public string Folder { get; private set; }
        [Required]
        public uint UniqueId { get; private set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; private set; }

        public string FullName { get; private set; }

        public string Domain { get; private set; }

        public DateTimeOffset MessageDateTime { get; private set; }

        public string MessageEml { get; private set; }

        public string Display => string.IsNullOrWhiteSpace(FullName) ? EmailAddress : $"{FullName} <{EmailAddress}>";

        public static MailItem CreateMailItem(string email, string server, string folder, uint uniqueId,
            string messageEml, DateTimeOffset dateTime)
            => CreateMailItem(email, null, server, folder, uniqueId, messageEml, dateTime);

        public static MailItem CreateMailItem(string email, string fullName, string server, string folder, uint uniqueId,
            string messageEml, DateTimeOffset dateTime)
        {
            var mailItem = new MailItem
            {
                EmailAddress = email,
                FullName = fullName,
                Domain = email.Contains('@')
                    ? email.Substring(email.IndexOf('@')).ToLower(CultureInfo.InvariantCulture)
                    : null,
                Server = server,
                Folder = folder,
                UniqueId = uniqueId,
                MessageEml = messageEml,
                MessageDateTime = dateTime
            };

            return mailItem;
        }
    }
}
