// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Tera.Game;

namespace Tera.Data
{
    public class BasicTeraData
    {
        public string ResourceDirectory { get; private set; }
        public IEnumerable<Server> Servers { get; private set; }
        public string Language { get; private set; }
        private readonly Func<string, TeraData> _dataForRegion;
        private readonly string _overridesDirectory;

        public TeraData DataForRegion(string region)
        {
            return _dataForRegion(region);
        }

        public BasicTeraData(string overridesDirectory,string language)
        {
            ResourceDirectory = FindResourceDirectory();
            Language = language;
            _overridesDirectory = overridesDirectory;
            _dataForRegion = Helpers.Memoize<string, TeraData>(region => new TeraData(this, region));
            LoadServers();
        }

        private void LoadServers()
        {
            var defaultServers = GetServers(Path.Combine(ResourceDirectory, "servers.txt"));

            //handle overrides
            var serversOverridePath = Path.Combine(_overridesDirectory, "server-overrides.txt");
            if (!File.Exists(serversOverridePath))//create the default file if it doesn't exist
                File.WriteAllText(serversOverridePath, Properties.Resources.server_overrides);
            var overriddenServers = GetServers(serversOverridePath).ToList();

            Servers = overriddenServers.Concat(defaultServers.Where(ds => overriddenServers.All(os => os.Ip != ds.Ip)));
        }

        private static string FindResourceDirectory()
        {
            var directory = Path.GetDirectoryName(typeof(BasicTeraData).Assembly.Location);
            while (directory != null)
            {
                var resourceDirectory = Path.Combine(directory, @"res\");
                if (Directory.Exists(resourceDirectory))
                    return resourceDirectory;
                directory = Path.GetDirectoryName(directory);
            }
            throw new InvalidOperationException("Could not find the resource directory");
        }

        private static IEnumerable<Server> GetServers(string filename)
        {
            return File.ReadAllLines(filename)
                       .Where(s => !s.StartsWith("#") && !string.IsNullOrWhiteSpace(s))
                       .Select(s => s.Split(new[] { ' ' }, 3))
                       .Select(parts => new Server(parts[2], parts[1], parts[0]));
        }
    }
}
