using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace SleepySlugcat
{
    public static class Utils
    {


        static List<string> todoMessages = new();
        static List<string> logMessages = new();


        public static bool InlineLog(string desc)
        {
            if (logMessages.Any((el) => el==desc)) return true;           
            UnityEngine.Debug.Log("InlineLog: " + desc);
            logMessages.Add(desc);
            return true;
        }

        public static bool TODO(string desc, bool outv = true, bool once = true)
        {
            if (once && todoMessages.Any((el) => el == desc)) return outv;
            UnityEngine.Debug.Log("TODO: " + desc);
            todoMessages.Add(desc);
            return outv;
        }
        public static string GetLogFor(object target)
        {
            var properties =
                from property in target.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                select new
                {
                    Name = property.Name,
                    Value = property.GetValue(target, null)
                };

            var builder = new System.Text.StringBuilder();

            foreach (var property in properties)
            {
                builder
                    .Append(property.Name)
                    .Append(" = ")
                    .Append(property.Value)
                    .AppendLine();
            }

            return builder.ToString();
        }
    }
}