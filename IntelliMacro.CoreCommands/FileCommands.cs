using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace IntelliMacro.CoreCommands
{
    class FindFilesCommand : AbstractCommand
    {
        internal FindFilesCommand() : base("FindFiles", true, "&Find Files", "&Filesystem") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "pattern"),
                    new ParameterDescription(true, "basepath"),
                    new ParameterDescription(true, "type"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Find files or directories.\n" +
                    "Pattern is a path pattern for the files, i. e. * is special but not ?, and parts will be separated by |. If pattern is empty, return a list of drives.\n" +
                    "If basepath is not given, the current directory is used.\n" +
                    "If type is absent or 0, both files and directories are returned, if it is 1, only directories are returned, if it is 2, only files are returned.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[0].String == "")
            {
                List<MacroObject> drives = new List<MacroObject>();
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    drives.Add(di.RootDirectory.Name);
                }
                return new MacroList(drives);
            }
            FileNode baseNode = new FileNode(parameters[1] == null ? "." : parameters[1].String);
            string pattern = parameters[0].String;
            if (parameters[2] != null && parameters[2].Number == 1)
            {
                pattern += "&directory=1";
            }
            else if (parameters[2] != null && parameters[2].Number == 2)
            {
                pattern += "&file=1";
            }
            List<MacroObject> result = new List<MacroObject>();
            foreach (FileNode fn in PathParser.ParsePath(pattern, baseNode))
            {
                result.Add(fn.FilePath);
            }
            return new MacroList(result);
        }
    }

    class FileInfoCommand : AbstractPathNodeInfoCommand<FileNode>
    {
        internal FileInfoCommand() : base("FileInfo", "File &Information", "&Filesystem") { }

        public override string Description
        {
            get
            {
                return "Obtain a parameter of a file and return it.\n" +
                    base.Description;
            }
        }

        protected override FileNode GetPathNode(MacroObject path, MacroContext context)
        {
            return new FileNode(path.String);
        }
    }

    class ChangeDirCommand : AbstractCommand
    {
        internal ChangeDirCommand() : base("ChangeDir", false, "C&hange directory", "&Filesystem") { }

        public override string Description
        {
            get
            {
                return "Change the current directory.\n" +
                    "If create is 1, create it (including parents) if it does not exist.";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "directory"),
                    new ParameterDescription(true, "create"),
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            try
            {
                string directory = parameters[0].String;
                if (parameters[1] != null && parameters[1].Number == 1 && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                Directory.SetCurrentDirectory(directory);
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            return null;
        }
    }

    class DeleteFileCommand : AbstractCommand
    {
        internal DeleteFileCommand() : base("DeleteFile", false, "&Delete File", "&Filesystem") { }

        public override string Description
        {
            get
            {
                return "Delete a file.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[] { new ParameterDescription(false, "file") }; }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            File.Delete(parameters[0].String);
            return null;
        }
    }

    class MoveFileCommand : AbstractCommand
    {
        bool copy;
        internal MoveFileCommand(bool copy)
            : base(copy ? "CopyFile" : "MoveFile", false, copy ? "&Copy File" : "&Move File", "&Filesystem")
        {
            this.copy = copy;
        }

        public override string Description
        {
            get
            {
                return (copy ? "Copy" : "Move") + " a file.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "source"),
                    new ParameterDescription(false, "destination")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            string source = parameters[0].String;
            string destination = parameters[1].String;
            try
            {
                if (copy)
                    File.Copy(source, destination);
                else
                    File.Move(source, destination);
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            return null;
        }
    }
    class FileNode : IPathNode<FileNode>
    {
        string path;

        internal FileNode(string path)
        {
            this.path = Path.GetFullPath(path);
        }

        internal string FilePath { get { return path; } }

        public string NodeName
        {
            get { return Path.GetFileName(path); }
        }

        public IEnumerable<string> ParameterNames
        {
            get
            {
                return new string[] {
                    "directory", 
                    "file", 
                    "size",
                    "created",
                    "modified",
                    "accessed",
                    "attributes",

                    // drives
                    "filesystem",
                    "disktype",
                    "availablespace",
                    "freespace",
                    "disksize",
                    "label",
                };
            }
        }

        public string GetParameter(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "directory":
                    return Directory.Exists(path) ? "1" : "";
                case "file":
                    return File.Exists(path) ? "1" : "";
                case "type":
                    return Directory.Exists(path) ? "Directory" : File.Exists(path) ? "File" : "";
                case "size":
                    return File.Exists(path) ? "" + new FileInfo(path).Length : "";
                case "created":
                    return "" + File.GetCreationTime(path).ToString();
                case "modified":
                    return "" + File.GetLastWriteTime(path).ToString();
                case "accessed":
                    return "" + File.GetLastAccessTime(path).ToString();
                case "attributes":
                    return new FileInfo(path).Attributes.ToString();
                case "filesystem":
                    if (path.Length > 3) return "";
                    return new DriveInfo(path.Substring(0, 1)).DriveFormat;
                case "disktype":
                    if (path.Length > 3) return "";
                    return new DriveInfo(path.Substring(0, 1)).DriveType.ToString();
                case "availablespace":
                    if (path.Length > 3) return "";
                    return "" + new DriveInfo(path.Substring(0, 1)).AvailableFreeSpace;
                case "freespace":
                    if (path.Length > 3) return "";
                    return "" + new DriveInfo(path.Substring(0, 1)).TotalFreeSpace;
                case "disksize":
                    if (path.Length > 3) return "";
                    return "" + new DriveInfo(path.Substring(0, 1)).TotalSize;
                case "label":
                    if (path.Length > 3) return "";
                    return "" + new DriveInfo(path.Substring(0, 1)).VolumeLabel;
                default: return "";
            }
        }

        public IEnumerable<FileNode> Children
        {
            get
            {
                List<FileNode> result = new List<FileNode>();
                if (Directory.Exists(path))
                {
                    foreach (String fse in Directory.GetFileSystemEntries(path))
                    {
                        if (fse == "." || fse == "..") continue;
                        result.Add(new FileNode(Path.Combine(path, fse)));
                    }
                }
                return result;
            }
        }

        public IPathRoot<FileNode> Parent
        {
            get { return new FileNode(Directory.GetParent(path).Name); }
        }
    }

    class SaveFileCommand : AbstractCommand
    {
        public SaveFileCommand() : base("SaveFile", false, "&Save to file", "&Filesystem") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "filename"),
                    new ParameterDescription(false, "content"),
                    new ParameterDescription(true, "encoding"),
                    new ParameterDescription(true, "append"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Save a text, an image object or a serialized object to a file.\n" +
                    "encoding specifies the text encoding or codepage; the system default is used when absent.\n" +
                    "For images, encoding is an image format (BMP, GIF, Icon, JPEG, PNG, TIFF), default is PNG.\n" +
                    "If encoding is \"=\" or \":\" or \"::\", save an object in a serialized format that can be read with LoadFile again (see Serialize command).\n" +
                    "If append is 1, append to a given file (text files only)";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            byte[] content;
            if (parameters[2] != null && parameters[2].String == "=")
            {
                MemoryStream ms = new MemoryStream();
                new BinaryFormatter().Serialize(ms, parameters[1]);
                content = ms.ToArray();
            }
            else if (parameters[2] != null && parameters[2].String == ":")
            {
                content = Encoding.ASCII.GetBytes(parameters[1].ToObjectNotation(false));
            }
            else if (parameters[2] != null && parameters[2].String == "::")
            {
                content = Encoding.ASCII.GetBytes(parameters[1].ToObjectNotation(true));
            }
            else if (MacroWrappedObject.Unwrap(parameters[1]) is Image)
            {
                Image img = (Image)MacroWrappedObject.Unwrap(parameters[1]);
                MemoryStream ms = new MemoryStream();
                ImageFormat format = ImageFormat.Png;
                if (parameters[2] != null)
                {
                    PropertyInfo propertyInfo = typeof(ImageFormat).GetProperty(parameters[2].String, BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (propertyInfo == null)
                    {
                        throw new MacroErrorException("Unsupported image format");
                    }
                    format = (ImageFormat)propertyInfo.GetGetMethod().Invoke(null, null);

                }
                img.Save(ms, format);
                content = ms.ToArray();
            }
            else
            {
                Encoding enc;
                try
                {
                    if (parameters[2] == null)
                    {
                        enc = Encoding.Default;
                    }
                    else if (parameters[2].IsNumber)
                    {
                        enc = Encoding.GetEncoding((int)parameters[2].Number);
                    }
                    else
                    {
                        enc = Encoding.GetEncoding(parameters[2].String);
                    }
                }
                catch (ArgumentException)
                {
                    throw new MacroErrorException("Unsupported encoding");
                }
                catch (NotSupportedException)
                {
                    throw new MacroErrorException("Unsupported encoding");
                }
                content = enc.GetBytes(parameters[1].String);
            }
            try
            {
                FileMode fileMode = parameters[3] != null && parameters[3].Number == 1 ? FileMode.Append : FileMode.Create;
                FileStream fs = new FileStream(parameters[0].String, fileMode, FileAccess.Write);
                fs.Write(content, 0, content.Length);
                fs.Close();
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            return null;
        }
    }

    class LoadFileCommand : AbstractCommand
    {
        public LoadFileCommand() : base("LoadFile", true, "&Load from file", "&Filesystem") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "filename"),
                    new ParameterDescription(true, "encoding"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Load text from a file\n" +
                    "filename specifies the name of the file.\n" +
                    "Optional encoding specifies the encoding to use for reading.\n" +
                    "If encoding is \"=\" or \":\" or \"::\", read an object that has been saved with the SaveFile or Serialize command.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            try
            {
                using (FileStream fileStream = new FileStream(parameters[0].String, FileMode.Open, FileAccess.Read))
                {
                    TextReader tr;
                    if (parameters[1] != null)
                    {
                        Encoding encoding;
                        if (parameters[1].IsNumber)
                            encoding = Encoding.GetEncoding((int)parameters[1].Number);
                        else if (parameters[1].String == "=")
                            return (MacroObject)new BinaryFormatter().Deserialize(fileStream);
                        else if (parameters[1].String == ":" || parameters[1].String == "::")
                            return MacroObject.FromObjectNotation(new StreamReader(fileStream, Encoding.ASCII).ReadToEnd());
                        else
                            encoding = Encoding.GetEncoding(parameters[1].String);
                        tr = new StreamReader(fileStream, encoding);
                    }
                    else
                    {
                        tr = new StreamReader(fileStream, Encoding.Default, true);
                    }
                    return tr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.ToString());
            }
        }
    }
}
