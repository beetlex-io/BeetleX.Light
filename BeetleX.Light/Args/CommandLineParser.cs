using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Args
{
    public class CommandLineParser
    {
        private Dictionary<string, string> mProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<Type, ObjectOptionsBuilder> mBuilders = new Dictionary<Type, ObjectOptionsBuilder>();

        private CommandLineParser()
        {

        }

        private ObjectOptionsBuilder GetBuilder(Type type)
        {
            if (mBuilders.TryGetValue(type, out ObjectOptionsBuilder result))
            {
                return result;
            }
            result = new ObjectOptionsBuilder(type);
            result.Init();
            mBuilders[type] = result;
            return result;
        }

        public T GetOption<T>() where T : new()
        {
            var builder = GetBuilder(typeof(T));
            return (T)builder.Builder(mProperties);

        }

        public static CommandLineParser GetCommandLineParser(string[] args, int start = 1)
        {
            CommandLineParser result = new CommandLineParser();
            for (int i = start; i < args.Length; i = i + 2)
            {
                result.mProperties[args[i]] = args[i + 1];
            }
            var evn = System.Environment.GetEnvironmentVariables();
            foreach (var key in evn.Keys)
            {
                result.mProperties["env_" + key] = evn[key].ToString();
            }
            return result;
        }

        public static CommandLineParser GetCommandLineParser()
        {
            return GetCommandLineParser(System.Environment.GetCommandLineArgs());
        }


        public string Help<T>()
        {

            var builder = GetBuilder(typeof(T));

            return builder.ToString();
        }

    }
}
