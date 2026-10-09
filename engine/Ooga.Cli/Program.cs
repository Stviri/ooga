using System.Text;
using Ooga.Cli;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

return CliApp.Run(args, Console.In, Console.Out, Console.Error, color: !Console.IsErrorRedirected);
