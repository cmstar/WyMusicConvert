using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace WyMusicConvert
{
    /// <summary>
    /// 一个定制过的<see cref="Parser"/>，提供包括下述功能：
    /// 1. 帮助信息默认输出到<see cref="Console.Out"/>，而非<see cref="Console.Error"/>；
    /// 2. 支持在没有给定 verb 的情况下，自动选择一个默认的 verb；
    /// 3. 支持不完整的 verb 匹配，如 d、do、down 都可以匹配到 download。
    /// </summary>
    /// <remarks>
    /// <see cref="Parser"/>没有定义虚方法，不能通过继承的方式来定制其行为，只能按照其签名定义一个新类型。
    /// ref https://github.com/commandlineparser/commandline/wiki
    /// </remarks>
    public class EnhancedCommandLineParser : IDisposable
    {
        /// <summary>
        /// Gets the singleton instance created with basic defaults.
        /// </summary>
        public static EnhancedCommandLineParser Default { get; } = new EnhancedCommandLineParser();

        private readonly Func<IEnumerable<string>, Type> _defaultVerbFactory;
        private readonly Parser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedCommandLineParser"/> class.
        /// </summary>
        /// <param name="defaultVerbFactory">
        /// A <see cref="Func{T, TResult}"/> delegate used to determine the default verb type 
        /// from the given arguments; if no default verb was found, returns null.
        /// </param>
        public EnhancedCommandLineParser(Func<IEnumerable<string>, Type> defaultVerbFactory = null)
        {
            _defaultVerbFactory = defaultVerbFactory;
            _parser = new Parser(settings => { settings.HelpWriter = Console.Out; });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedCommandLineParser"/> class, configurable with
        /// <see cref="ParserSettings"/> using a delegate.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="Action"/> delegate used to configure aspects and behaviors of the parser.
        /// </param>
        /// <param name="defaultVerbFactory">
        /// A <see cref="Func{T, TResult}"/> delegate used to determine the default verb type 
        /// from the given arguments; if no default verb was found, returns null.
        /// </param>
        public EnhancedCommandLineParser(
            Action<ParserSettings> configuration, Func<IEnumerable<string>, Type> defaultVerbFactory = null)
        {
            _defaultVerbFactory = defaultVerbFactory;
            _parser = new Parser(configuration);
        }

        ~EnhancedCommandLineParser()
        {
            _parser.Dispose();
        }

        /// <summary>
        /// Gets the instance that implements <see cref="ParserSettings"/> in use.
        /// </summary>
        public ParserSettings Settings => _parser.Settings;

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            _parser.Dispose();
        }

        /// <summary>
        /// Parses a string array of command line arguments constructing values in an instance
        /// of type <typeparamref name="T"/>. 
        /// Grammar rules are defined decorating public properties with appropriate attributes.
        /// </summary>
        /// <typeparam name="T">Type of the target instance built with parsed value.</typeparam>
        /// <param name="args">
        /// A <see cref="string"/> array of command line arguments, normally supplied by application 
        /// entry point.
        /// </param>
        /// <returns>
        /// A <see cref="ParserResult{T}"/> containing an instance of type T with parsed values
        /// and a sequence of CommandLine.Error.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if one or more arguments are null.</exception>
        public ParserResult<T> ParseArguments<T>(IEnumerable<string> args)
        {
            var fixedArgs = PrepareArgs(args, typeof(T));
            return _parser.ParseArguments<T>(fixedArgs);
        }

        /// <summary>
        /// Parses a string array of command line arguments constructing values in an instance
        /// of type T. Grammar rules are defined decorating public properties with appropriate
        /// attributes.
        /// </summary>
        /// <typeparam name="T">Type of the target instance built with parsed value.</typeparam>
        /// <param name="factory">
        /// A <see cref="Func{TResult}"/> delegate used to initialize the target instance.
        /// </param>
        /// <param name="args">
        /// A <see cref="string"/> array of command line arguments, normally supplied by application entry point.
        /// </param>
        /// <returns>
        /// A <see cref="ParserResult{T}"/> containing an instance of type T with parsed values
        /// and a sequence of CommandLine.Error.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if one or more arguments are null.</exception>
        public ParserResult<T> ParseArguments<T>(Func<T> factory, IEnumerable<string> args) where T : new()
        {
            var fixedArgs = PrepareArgs(args, typeof(T));
            return _parser.ParseArguments(factory, fixedArgs);
        }

        /// <summary>
        /// Parses a string array of command line arguments for verb commands scenario, constructing
        /// the proper instance from the array of types supplied by types. Grammar rules
        /// are defined decorating public properties with appropriate attributes. The <see cref="VerbAttribute"/>
        /// must be applied to types in the array.
        /// </summary>
        /// <param name="args">
        /// A <see cref="string"/> array of command line arguments, normally supplied by application entry point.
        /// </param>
        /// <param name="types">A <see cref="Type"/> array used to supply verb alternatives.</param>
        /// <returns>
        /// A <see cref="ParserResult{T}"/> containing the appropriate instance with parsed
        /// values as a System.Object and a sequence of CommandLine.Error.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if one or more arguments are null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if types array is empty.</exception>
        /// <remarks>
        /// All types must expose a parameterless constructor. It's strongly recommended to use a generic overload.
        /// </remarks>
        public ParserResult<object> ParseArguments(IEnumerable<string> args, params Type[] types)
        {
            var fixedArgs = PrepareArgs(args, types);
            return _parser.ParseArguments(fixedArgs, types);
        }

        private string[] PrepareArgs(IEnumerable<string> args, params Type[] types)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            // 尝试获取默认的 verb 。
            var arr = args as string[] ?? args.ToArray();
            var argsWithDefaultVerb = TryPrepareDefaultVerb(arr);
            if (argsWithDefaultVerb != null)
                return argsWithDefaultVerb;

            // 处理名称不完整的 verb 。
            arr = FixShortVerb(arr, types);
            return arr;
        }

        // if default verb was specified, return a new array with the defatul verb; otherwise, returns null
        private string[] TryPrepareDefaultVerb(string[] args)
        {
            if (_defaultVerbFactory == null)
                return null;

            var defaultVerb = _defaultVerbFactory(args);
            if (defaultVerb == null)
                return null;

            var verbName = GetVerbName(defaultVerb);
            if (verbName == null)
                return null;

            var len = args.Length;
            if (len > 0 && args[0] == verbName)
                return null;

            var newArgs = new string[args.Length + 1];
            newArgs[0] = verbName;
            Array.Copy(args, 0, newArgs, 1, args.Length);
            return newArgs;
        }

        // returns a new array with the fixed verb
        private static string[] FixShortVerb(string[] args, IEnumerable<Type> verbs)
        {
            if (args.Length == 0)
                return args;

            var verbNames = verbs.Select(GetVerbName).Where(x => x != null).ToArray();
            var verbName = MatchVerbName(verbNames, args[0]);
            if (verbName == null)
                return args;

            var newArgs = new string[args.Length];
            Array.Copy(args, 1, newArgs, 1, args.Length - 1);
            newArgs[0] = verbName;
            return newArgs;
        }

        // returns null if there's no VerbAttribute
        private static string GetVerbName(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(VerbAttribute), true);
            if (attrs.Length == 0)
                return null;

            var attr = (VerbAttribute)attrs[0];
            return attr.Name;
        }

        // 允许 verb 只写前缀部分，如 d、do、down 都可以匹配到 download 。
        private static string MatchVerbName(string[] verbNames, string value)
        {
            string result = null;
            foreach (var verbName in verbNames)
            {
                if (!verbName.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 匹配结果多于1个时，有歧义，作为匹配失败处理。
                if (result != null)
                    return null;

                result = verbName;
            }
            return result;
        }
    }
}
