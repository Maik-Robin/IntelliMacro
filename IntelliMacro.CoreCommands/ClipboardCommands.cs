using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    class GetClipboardCommand : AbstractCommand
    {
        internal GetClipboardCommand() : base("GetClipboard", true, "&Get clipboard contents", "&Clipboard") { }

        public override string Description
        {
            get
            {
                return "Get the current contents of the system clipboard.\n\n" +
                    "Without parameters, return the contents as an unformatted text.\n" +
                    "With a string parameter, convert to the specified format.\n" +
                    "With a list parameter, convert to all the formats in the list, and return a list.\n" +
                    "With an argument of -1, return all formats that are in the clipboard.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(true, "Format")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[0] == null)
            {
                return Clipboard.GetText();
            }
            else if (parameters[0].IsNumber && parameters[0].Number == -1)
            {
                IDataObject o = Clipboard.GetDataObject();
                List<MacroObject> formats = new List<MacroObject>(),
                    values = new List<MacroObject>();
                foreach (string format in o.GetFormats(false))
                {
                    object data = o.GetData(format);
                    formats.Add(format);
                    values.Add(SerializeData(data));
                }
                return new MacroList(new MacroObject[] { 
                    new MacroList(formats), 
                    new MacroList(values) 
                });
            }
            else if (parameters[0] is MacroList)
            {
                IDataObject o = Clipboard.GetDataObject();
                List<MacroObject> values = new List<MacroObject>();
                for (int i = 0; i < parameters[0].Length; i++)
                {
                    object data = o.GetData(parameters[0][i + 1].String);
                    values.Add(SerializeData(data));
                }
                return new MacroList(values);
            }
            else
            {
                return SerializeData(Clipboard.GetData(parameters[0].String));
            }
        }

        private MacroObject SerializeData(object data)
        {
            if (data == null) return new MacroWrappedObject(null);
            if (data is string) return (MacroObject)(string)data;
            if (data is string[])
                return new MacroList(Array.ConvertAll((string[])data, s => (MacroObject)s));
            else
                return new MacroWrappedObject(data);
        }
    }

    class SetClipboardCommand : AbstractCommand
    {
        internal SetClipboardCommand() : base("SetClipboard", false, "&Set clipboard contents", "&Clipboard") { }

        public override string Description
        {
            get
            {
                return "Set the clipboard content\n\n" +
                    "See GetClipboard for detail about supported Format parameters.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "New content"),
                    new ParameterDescription(true, "Format"),
                    new ParameterDescription(true, "Allow auto-conversions"),
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            DataObject o = new DataObject();
            bool autoConvert = parameters[2] == null || parameters[2].Number != 0;
            if (parameters[1] == null)
            {
                if (parameters[0].String != "")
                    o.SetText(parameters[0].String);
            }
            else if (parameters[1].IsNumber && parameters[1].Number == -1)
            {
                if (parameters[0].Length != 2) throw new MacroErrorException("Need two elements in param");
                return InvokeAction(context, new MacroObject[] { parameters[0][2], parameters[0][MacroObject.ONE], parameters[2] });
            }
            else if (parameters[1] is MacroList)
            {
                if (parameters[0].Length != parameters[1].Length)
                    throw new MacroErrorException("Format list and content list do not have same length.");
                for (int i = 0; i < parameters[0].Length; i++)
                {
                    object data = DeserializeData(parameters[0][i + 1]);
                    if (data != null)
                        o.SetData(parameters[1][i + 1].String, autoConvert, data);
                }
            }
            else
            {
                object data = DeserializeData(parameters[0]);
                if (data != null)
                    o.SetData(parameters[1].String, autoConvert, data);
            }
            try
            {
                Clipboard.SetDataObject(o, true);
            }
            catch (ExternalException ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            SetDelay(1);
            return null;
        }

        private object DeserializeData(MacroObject obj)
        {
            if (obj is MacroList)
            {
                string[] array = new string[obj.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = obj[i + 1].String;
                }
                return array;
            }
            else if (obj is MacroWrappedObject)
            {
                return ((MacroWrappedObject)obj).Wrapped;
            }
            else
            {
                return obj.String;
            }
        }
    }

    class ClearClipboardCommand : AbstractCommand
    {
        internal ClearClipboardCommand() : base("ClearClipboard", false, "&Clear clipboard", "&Clipboard") { }

        public override string Description
        {
            get
            {
                return "Clear the system clipboard.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[0]; }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            Clipboard.Clear();
            SetDelay(1);
            return null;
        }
    }
}
