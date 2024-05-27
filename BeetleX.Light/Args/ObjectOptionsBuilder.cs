using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Args
{
    class ObjectOptionsBuilder
    {
        public ObjectOptionsBuilder(Type type)
        {
            mObjectType = type;
        }
        private Type mObjectType { get; set; }
        public List<OptionAttribute> Options { get; set; } = new List<OptionAttribute>();

        public void Init()
        {
            foreach (var item in mObjectType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var option = item.GetCustomAttribute<OptionAttribute>(false);
                if (option != null)
                {
                    option.Property = item;
                    Options.Add(option);
                    if (string.IsNullOrEmpty(option.ShortName))
                    {
                        option.ShortName = item.Name;
                        option.LongName = item.Name;
                    }
                    if (string.IsNullOrEmpty(option.LongName))
                    {
                        option.LongName = option.ShortName;
                    }
                }
            }
            if (Options.Count == 0)
            {
                throw new ParserException($"{mObjectType.Name} object does not have a configuration option!");
            }
        }

        public object Builder(Dictionary<string, string> args)
        {
            object result = Activator.CreateInstance(mObjectType);
            foreach (var p in Options)
            {
                p.SetValue(args, result);
            }

            return result;
        }

        public override string ToString()
        {
            string result = "";
            foreach (var p in Options)
            {
                result += p + "\r\n";
            }
            return result;
        }
    }
}
