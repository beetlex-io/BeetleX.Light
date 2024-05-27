using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Args
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute()
        {

        }
        public OptionAttribute(string shortName)
        {
            this.ShortName = shortName;
        }

        public string Describe { get; set; }

        public string ShortName { get; set; }

        public string LongName { get; set; }

        public bool Required { get; set; } = true;

        internal PropertyInfo Property { get; set; }

        internal void SetValue(Dictionary<string, string> args, object source)
        {
            string value = null;
            string envshort = "env_" + ShortName;
            string envlong = "env_" + LongName;

            if (args.ContainsKey(envshort))
            {
                value = args[envshort];
            }

            if (args.ContainsKey(envlong))
            {
                value = args[envlong];
            }
            if (args.ContainsKey(ShortName))
            {
                value = args[ShortName];
            }
            if (args.ContainsKey(LongName))
            {
                value = args[LongName];
            }

            if (!Required && string.IsNullOrEmpty(value))
                return;
            if (Required && string.IsNullOrEmpty(value))
            {
                throw new ParserException($"{ShortName} parameter required!");
            }

            object data = null;
            try
            {
                data = Convert.ChangeType(value, Property.PropertyType);
            }
            catch (Exception e_)
            {
                throw new ParserException($"{ShortName} convert data error  {e_.Message}!");
            }
            Property.SetValue(source, data);
        }

        public override string ToString()
        {
            if (Required)
                return $"<{ShortName}|{LongName}>\t{Describe}";
            else
                return $"[{ShortName}|{LongName}]\t{Describe}";
        }
    }
}
