# ImapEmailExtractor
A simple and minimalistic app to extract sender email address / names from all emails in imap server and store them in a database.

* Use [MailKit](https://github.com/jstedfast/MailKit) to connect to imap server and download emails.
* Use EF Core to persist data.
* Support storing data for different username + server combinations in a single database.
* Cross platform, made with .net core 2.2.
