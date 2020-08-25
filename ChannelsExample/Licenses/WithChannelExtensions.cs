using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Channels;
using System.Threading.Tasks;
using Open.ChannelExtensions;
using Xunit;
using Xunit.Abstractions;

namespace ChannelsExample.Licenses
{
    public class WithChannelExtensions
    {
        private readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        
        private readonly ITestOutputHelper _outputHelper;

        public WithChannelExtensions(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private IEnumerable<string> ReadInput()
        {
            using var reader = new StreamReader(File.OpenRead("Licenses/input.txt"));

            string? url;
            while ((url = reader.ReadLine()) != null)
            {
                yield return url.Trim();
            }
        }
        
        [Fact]
        public async Task RunWithChannelsAsync()
        {
            // 1. Read file
            // 2. Download license
            // 3. Check content for known words
            // 4. Write to other file
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            await using var writer = new StreamWriter(File.OpenWrite("Licenses/withchannelextensions-output.txt"));
            
            await Channel
                .CreateBounded<string>(50000)
                .Source(ReadInput())
                .PipeAsync(
                    maxConcurrency: 100,
                    capacity: 500,
                    transform: async url =>
                    {
                        var contents = await _httpClient.GetStringOrNullAsync(url);
                        return (url, contents);
                    })
                .Pipe(
                    maxConcurrency: 10,
                    capacity: 50000,
                    transform: tuple =>
                    {
                        var licenseIdentifier = LicenseStrings.TryIdentify(tuple.Item2 ?? string.Empty);
                        return $"{tuple.Item1};{licenseIdentifier}";
                    })
                .ReadAllAsync(async line =>
                {
                    await writer.WriteLineAsync(line);
                });
            
            stopwatch.Stop();
            _outputHelper.WriteLine("Completed in {0}", stopwatch.Elapsed);
        }
    }
}