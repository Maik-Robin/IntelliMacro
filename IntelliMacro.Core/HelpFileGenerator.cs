using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    public static class HelpFileGenerator
    {
        public static void GenerateHelpFile(string assemblyName, string destinationDirectory)
        {
            IEnumerable<ICommand> allCommands = LoadCommands(assemblyName);
            Dictionary<string, List<ICommand>> categories = new Dictionary<string, List<ICommand>>();
            foreach (ICommand cmd in allCommands)
            {
                File.WriteAllText(Path.Combine(destinationDirectory, "cmd-" + cmd.Name + ".html"), CreateCommandDescription(cmd));
                string category = cmd.DisplayCategory.Replace("&", "");
                if (!categories.ContainsKey(category))
                    categories.Add(category, new List<ICommand>());
                categories[category].Add(cmd);
            }
            File.WriteAllText(Path.Combine(destinationDirectory, "commands.hhp"),
                @"[OPTIONS]
Compatibility=1.1 or later
Compiled file=CommandHelp.chm
Contents file=commands.hhc
Index file=commands.hhk
Full-text search=Yes
Language=0x409
Title=Command Help");
            StringBuilder content = new StringBuilder();
            content.Append(@"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<meta name=""GENERATOR"" content=""Microsoft&reg; HTML Help Workshop 4.1"">
<!-- Sitemap 1.0 -->
</HEAD><BODY>
<!-- Content -->
<UL>
");
            List<string> categoryNames = new List<string>(categories.Keys);
            categoryNames.Sort();
            foreach (string category in categoryNames)
            {
                content.Append(@"	<LI> <OBJECT type=""text/sitemap"">
		<param name=""Name"" value=""" + category + @""">
		</OBJECT>
	<UL>
");
                List<ICommand> commands = categories[category];
                commands.Sort((c1, c2) => (c1.DisplayName.CompareTo(c2.DisplayName)));
                foreach (ICommand cmd in commands)
                {
                    content.Append(@"		<LI> <OBJECT type=""text/sitemap"">
			<param name=""Name"" value=""" + cmd.DisplayName.Replace("&", "") + @""">
			<param name=""Local"" value=""cmd-" + cmd.Name + @".html"">
			</OBJECT>
");
                }
                content.Append("    </UL>\r\n");
            }
            content.Append("</UL>\r\n<!-- Content -->\r\n</BODY></HTML>");
            File.WriteAllText(Path.Combine(destinationDirectory, "commands.hhc"), content.ToString());
            content.Remove(0, content.Length);
            content.Append(@"<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML//EN"">
<HTML>
<HEAD>
<meta name=""GENERATOR"" content=""Microsoft&reg; HTML Help Workshop 4.1"">
<!-- Sitemap 1.0 -->
</HEAD><BODY>
<UL>
<!-- Content -->
");
            foreach (ICommand cmd in allCommands)
            {
                foreach (string keyword in GetKeywords(cmd))
                {
                    content.Append(@"		<LI> <OBJECT type=""text/sitemap"">
			<param name=""Name"" value=""" + keyword + @""">
			<param name=""Local"" value=""cmd-" + cmd.Name + @".html"">
			</OBJECT>
");
                }
            }
            content.Append("<!-- Content -->\r\n</UL>\r\n</BODY></HTML>");
            File.WriteAllText(Path.Combine(destinationDirectory, "commands.hhk"), content.ToString());
        }

        private static IEnumerable<ICommand> LoadCommands(string assemblyName)
        {
            try
            {
                Assembly a = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, assemblyName));
                string initType = Path.GetFileName(assemblyName);
                if (!initType.EndsWith(".dll")) throw new Exception();
                initType = initType.Substring(0, initType.Length - 3) + "Init";
                Type t = a.GetType(initType, false);
                IMacroPluginInitializer mpi = t.GetConstructor(new Type[0]).Invoke(new object[0]) as IMacroPluginInitializer;
                CommandRegistry cr = new CommandRegistry();
                mpi.InitPlugin(cr);
                return cr.Commands;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new ICommand[0];
            }
        }

        private static IList<string> GetKeywords(ICommand command)
        {
            List<string> result = new List<string>();
            result.Add(command.Name + " " + (command.ReturnsValue ? "function" : "command"));
            // TODO parse tags in description?
            return result;
        }

        public static string CreateCommandDescription(ICommand command)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"">
<html>
<head>
<title>Introduction</title>
<meta http-equiv=""Content-Type"" content=""text/html; charset=iso-8859-1"">
<style type=""text/css"">
body {font-family: verdana,arial,sans-serif; font-size: 8pt;}
.kind {float:right;}
h1{font-size: 12pt; margin-bottom: 0em;}
h2{font-size: 10pt; margin-top: 0em;}
</style>
</head>
<body>");
            sb.Append(@"<b class=""kind"">" + (command.ReturnsValue ? "Function" : "Command") + "</b>");
            sb.Append("<h1>" + command.DisplayName.Replace("&", "") + "</h1>\r\n");
            sb.Append("<h2>" + command.Name + "(");
            bool first = true;
            foreach (ParameterDescription desc in command.ParameterDescriptions)
            {
                if (!first) sb.Append(", ");
                first = false;
                if (desc.Optional) sb.Append("[");
                sb.Append(desc.Name);
                if (desc.Optional) sb.Append("]");
            }
            sb.Append(")</h2>\n");
            sb.Append("<p>" + command.Description.Replace("\r\n", "\n").Replace("\n", "<br />"));
            sb.Append("</p>\r\n</body>\r\n</html>\r\n");
            return sb.ToString();
        }
    }
}
