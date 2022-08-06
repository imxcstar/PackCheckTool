using PackCheckTool;
using SharpCompress.Archives;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Readers;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var ofiles = new List<FileInfo>();
var upass = "";
var coding = Encoding.Default;
var commandType = CommandType.AddFile;
foreach (var item in args)
{
    switch (item)
    {
        case CommandType.AddFile:
        case CommandType.SetPass:
        case CommandType.SetCoding:
            commandType = item;
            continue;
        default:
            break;
    }

    switch (commandType)
    {
        case CommandType.SetPass:
            upass = item;
            commandType = CommandType.AddFile;
            break;
        case CommandType.AddFile:
            ofiles.Add(new FileInfo(item));
            break;
        case CommandType.SetCoding:
            coding = Encoding.GetEncoding(item);
            commandType = CommandType.AddFile;
            break;
        default:
            break;
    }
}

if (!ofiles.Any())
{
    Console.WriteLine(Assembly.GetExecutingAssembly().GetName());
    Console.WriteLine($"Usage: PackCheckTool [option] <file> <file2>...");
    Console.WriteLine("<Option>");
    Console.WriteLine("  -p <pass> : set password");
    Console.WriteLine("  -c <coding> : set coding");
    Console.WriteLine("  -f <file> <file2>... : load files");
    return;
}

var pack = ArchiveFactory.Open(ofiles, new ReaderOptions()
{
    Password = upass,
    ArchiveEncoding = new ArchiveEncoding(coding, coding)
});

var files = new ConcurrentBag<TreeNodeRelationEntityBase<PackFileInfo>>();
pack.Entries.AsParallel().ForAll(item =>
{
    var itemKey = item.Key.Trim('/');
    var n = itemKey.Split('/');
    files.Add(new TreeNodeRelationEntityBase<PackFileInfo>()
    {
        OTag = new PackFileInfo(item)
        {
            Name = n.Last(),
            Path = string.Join('/', n.Take(n.Length - 1)),
            IsDir = item.IsDirectory,
            LastModifiedTime = item.LastModifiedTime,
            Size = item.Size
        },
        ID = itemKey.GetHashCode().ToString(),
        Name = n.Last(),
        OrderID = 0,
        ParentIDs = n.Length == 1 ? new string[] { } : new[] { string.Join('/', n.Take(n.Length - 1)).GetHashCode().ToString() }
    });
});

var treeFiles = files.ToTreeList();

var pwd = "";
var pwdo = treeFiles;

while (true)
{
    Console.Write($"{pwd}>");
    var tcommand = Console.ReadLine();
    if (string.IsNullOrEmpty(tcommand))
        continue;
    var commands = tcommand?.Split(' ');
    if (commands == null)
        continue;
    switch (commands.First().ToLower())
    {
        case "l":
        case "ls":
        {
            Console.WriteLine();

            pwdo = treeFiles;

            if (!string.IsNullOrEmpty(pwd))
            {
                var rpathID = pwd.GetHashCode().ToString();
                pwdo = treeFiles.AllTreeList.AsParallel().FirstOrDefault(x => x.EntityData.ID == rpathID && x.EntityData.OTag.IsDir);
            }

            if (pwdo == null)
                break;

            var ns = pwdo.ChildNodes.OrderByDescending(x => x.EntityData.Name).OrderByDescending(x => x.EntityData.OTag.IsDir);
            Console.WriteLine($"\t{"LastModifiedTime",19}{"Size",18}\tName");
            Console.WriteLine($"\t{"----------------",19}{"----",18}\t----");
            foreach (var item in ns)
                Console.WriteLine($"\t{item.EntityData.OTag.LastModifiedTime,19}{(item.EntityData.OTag.IsDir ? "-" : item.EntityData.OTag.Size),18}\t{item.EntityData.Name}");
            Console.WriteLine();
        }
            break;

        case "cd":
        {
            var v = tcommand[(tcommand.IndexOf(' ') + 1)..];
            if (v == "..")
            {
                var pwds = pwd.Split("/");
                v = pwd.Contains('/') ? string.Join('/', pwds.Take(pwds.Length - 1)) : "";
            }
            else if (!string.IsNullOrEmpty(pwd))
            {
                var tp = pwd + "/" + v;
                var rpathID = tp.GetHashCode().ToString();
                if (treeFiles.AllTreeList.AsParallel().FirstOrDefault(x => x.EntityData.ID == rpathID && x.EntityData.OTag.IsDir) == null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"\t\"{tp}\" path does not exist!");
                    Console.WriteLine();
                    break;
                }

                v = tp;
            }
            else
            {
                var rpathID = v.GetHashCode().ToString();
                if (treeFiles.AllTreeList.AsParallel().FirstOrDefault(x => x.EntityData.ID == rpathID && x.EntityData.OTag.IsDir && !x.EntityData.ParentIDs.Any()) == null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"\t\"{v}\" path does not exist!");
                    Console.WriteLine();
                    break;
                }
            }

            pwd = v;
        }
            break;

        case "cat":
        {
            var v = tcommand[(tcommand.IndexOf(' ') + 1)..];
            if (!string.IsNullOrEmpty(pwd))
                v = pwd + "/" + v;
            var rpathID2 = v.GetHashCode().ToString();
            var f = treeFiles.AllTreeList.AsParallel().FirstOrDefault(x => x.EntityData.ID == rpathID2 && !x.EntityData.OTag.IsDir);
            if (f == null)
            {
                Console.WriteLine();
                Console.WriteLine($"\t\"{v}\" file does not exist!");
                Console.WriteLine();
                break;
            }

            var vaa = f.EntityData.OTag;

            using (var fs = f.EntityData.OTag.OpenStream())
            {
                if (fs == null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"\t\"{v}\" file open error!");
                    Console.WriteLine();
                    break;
                }

                using (var t = new StreamReader(fs))
                {
                    Console.WriteLine();
                    Console.WriteLine(t.ReadToEnd());
                    Console.WriteLine();
                }
            }
        }

            break;
        case "down":
        {
            var v = tcommand[(tcommand.IndexOf(' ') + 1)..];
            if (!string.IsNullOrEmpty(pwd))
                v = pwd + "/" + v;
            var rpathID2 = v.GetHashCode().ToString();
            var f = treeFiles.AllTreeList.AsParallel().FirstOrDefault(x => x.EntityData.ID == rpathID2);
            if (f == null)
            {
                Console.WriteLine();
                Console.WriteLine($"\t\"{v}\" file does not exist!");
                Console.WriteLine();
                break;
            }

            Console.Write("out path:");
            var outPath = Console.ReadLine();
            if(!Directory.Exists(outPath))
            {
                Console.WriteLine();
                Console.WriteLine($"\t\"{outPath}\" path does not exist!");
                Console.WriteLine();
                break;
            }
            
            var vaa = f.EntityData.OTag;

            vaa.ArchiveEntry.WriteToDirectory(outPath, new ExtractionOptions() { ExtractFullPath = true });
        }
            break;

        case "cls":
        case "clear":
            Console.Clear();
            break;

        case "exit":
            return;

        default:
            break;
    }
}