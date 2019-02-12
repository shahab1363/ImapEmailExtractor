using EmailExtractor.DatabaseModel;
using EmailExtractor.ImapConnector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Crypto.Agreement.Kdf;

namespace EmailExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load Configuration
#if DEBUG
            var environment = "development";
#else
            var environment = "production";
#endif
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{environment}.json", true, true);


#if DEBUG
            configurationBuilder.AddUserSecrets<Program>();
#endif

            var configuration = configurationBuilder.Build();

            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole(conf =>
                {
                    conf.TimestampFormat = "hh:MM:ss ";
                }).SetMinimumLevel(LogLevel.Warning))
                .AddDbContext<MailExtractorContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("Default")),
                    ServiceLifetime.Transient)
                .AddSingleton<IConfiguration>(provider => configuration)
                .AddSingleton<IConnector, Connector>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

            logger.LogWarning("Starting application");

            var context = serviceProvider.GetRequiredService<MailExtractorContext>();
            context.Database.Migrate();

            var connector = serviceProvider.GetService<IConnector>();

            logger.LogWarning($"Connector server key: {connector.ServerKey}");

            var folders = connector.Folders(null, true).ToList();
            logger.LogWarning($"Got {folders.Count} folders");

            var counter = 0;

            int pageSize = 250;

            foreach (var folder in folders)
            {
                var folderStart = DateTime.Now;
                logger.LogWarning($"Scanning folder: {folder.FullName}");

                var folderCounter = 0;
                int page = 0;
                while (true)
                {
                    var pageStart = DateTime.Now;
                    logger.LogWarning($"Starting {folder.FullName} page {page} - Page size: {pageSize}");

                    var existingUids = context.MailItems.AsNoTracking()
                        .Where(x => x.Server.Equals(connector.ServerKey) && x.Folder.Equals(folder.FullName))
                        .Select(x => x.UniqueId).ToList();

                    logger.LogInformation($"Got {existingUids.Count} existing uids in {folder.FullName}");

                    var items = connector.ExtractMailItems(folder, pageSize, page, existingUids);

                    if (items == null)
                        break;

                    foreach (var mailItem in items)
                    {
                        context.MailItems.Add(mailItem);
                        logger.LogInformation($"{counter}\t{mailItem.Display}");
                        counter++;
                        folderCounter++;
                    }

                    context.SaveChanges();

                    logger.LogWarning($"{folder.FullName} page {page} completed [{(DateTime.Now - pageStart).TotalSeconds:#} secs]. {folderCounter} mail items in folder so far.");

                    page++;
                }

                logger.LogWarning($"Saved {counter} mail items so far. {folderCounter} mail items in folder: {folder.FullName} [{(DateTime.Now - folderStart).TotalSeconds:#} secs]");
            }


            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
