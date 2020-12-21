using System;
using System.Diagnostics;

namespace Transport
{
    public static class Log
    {
        private static Action<string>? _infoCallback;
        private static Action<string>? _warningCallback;
        private static Action<string>? _errorCallback;
        private static Action<Exception>? _exceptionCallback;

        public static void InitForConsole()
        {
            Init(
              info =>
              {
                  Console.ForegroundColor = ConsoleColor.Gray;
                  Console.WriteLine(info);
                  Console.ForegroundColor = ConsoleColor.Gray;
              },

              warn =>
              {
                  Console.ForegroundColor = ConsoleColor.Yellow;
                  Console.WriteLine(warn);
                  Console.ForegroundColor = ConsoleColor.Gray;
              },

              error =>
              {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine(error);
                  Console.ForegroundColor = ConsoleColor.Gray;
              },

              exn =>
              {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine(exn.Message);
                  Console.WriteLine(exn.StackTrace);
                  Console.ForegroundColor = ConsoleColor.Gray;
              }
            );
        }

        public static void Init(Action<string> info, Action<string> warn, Action<string> error, Action<Exception> exn)
        {
            _infoCallback = info;
            _warningCallback = warn;
            _errorCallback = error;
            _exceptionCallback = exn;
        }

        public static void Info(object? value)
        {
            _infoCallback?.Invoke(value == null ? "NULL" : value.ToString()!);
        }

        public static void Info(string fmt, params object[] args)
        {
            _infoCallback?.Invoke(string.Format(fmt, args));
        }

        public static void Warn(string fmt, params object[] args)
        {
            _warningCallback?.Invoke(string.Format(fmt, args));
        }

        public static void Warn(object? value)
        {
            _warningCallback?.Invoke(value == null ? "NULL" : value.ToString()!);
        }

        public static void Error(string fmt, params object[] args)
        {
            _errorCallback?.Invoke(string.Format(fmt, args));
        }

        public static void Error(object? value)
        {
            _errorCallback?.Invoke(value == null ? "NULL" : value.ToString()!);
        }

        public static void Exception(Exception exn)
        {
            _exceptionCallback?.Invoke(exn);
        }
    }
}