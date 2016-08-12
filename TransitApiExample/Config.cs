using iSynaptic.Commons;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TransitAPIExample
{


    public class Config
    {
        public Dictionary<int, string> Directions;
        public string ApiUrl;
        public int RoutesTTLSeconds;
        public int DirectionsTTLSeconds;
        public int StopsTTLSeconds;
        public int DeparturesTTLSeconds;
    }

    public class AppSettingsConfigLoader
    {
        private class AppSettings
        {
            public Config Application;
        }

        public static Config Load() {
            return JsonConvert.DeserializeObject<AppSettings>(LocalFileLoader.Load("appsettings.json").Value).Application;
        }
    }

    public class LocalFileLoader
    {
        public static Result<string, string> Load(string path)
        {
            var pathSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
            var pathArray = PlatformServices.Default.Application.ApplicationBasePath.Split(pathSeparator).ToArray();

            var projectName = nameof(TransitAPIExample);
            var indexOfProject = pathArray
                .Where(x => x.StartsWith(projectName, StringComparison.OrdinalIgnoreCase))
                .MaybeFirst()
                .Select(x => Array.IndexOf(pathArray, x))
                .Or(-1).Value;

            var workingDirectory = string.Join("/", pathArray.Take(indexOfProject + 1));
            try
            {
                using (Stream s = File.Open($"{workingDirectory}/{path}", FileMode.Open))
                {
                    using (TextReader reader = new StreamReader(s))
                    {
                        return reader.ReadToEnd().ToResult();
                    }
                }
            }
            catch (Exception ex)
            {
                return Result.NoValue.Fail().Observe($"Unable to find file: {path}. {ex.GetType().Name}: {ex.Message}.");
            }
            
        }
    }
}
