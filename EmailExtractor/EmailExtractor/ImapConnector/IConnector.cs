using EmailExtractor.DatabaseModel;
using MailKit;
using System.Collections.Generic;

namespace EmailExtractor.ImapConnector
{
    interface IConnector
    {
        string ServerKey { get; }
        IEnumerable<IMailFolder> Folders(IMailFolder rootFolder, bool recursive);
        List<MailItem> ExtractMailItems(IMailFolder folder, int pageSize, int page, List<uint> existingUids);
    }
}
