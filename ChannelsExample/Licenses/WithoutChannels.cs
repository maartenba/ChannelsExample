using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ChannelsExample.Licenses
{
    public class WithoutChannels
    {
        private readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        
        private readonly ITestOutputHelper _outputHelper;

        public WithoutChannels(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public async Task RunWithoutChannelsAsync()
        {
            // 1. Read file
            // 2. Download license
            // 3. Check content for known words
            // 4. Write to other file

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using var reader = new StreamReader(File.OpenRead("Licenses/input.txt"));
            await using var writer = new StreamWriter(File.OpenWrite("Licenses/withoutchannels-output.txt"));
                
            string? url;
            while ((url = await reader.ReadLineAsync()) != null)
            {
                url = url.Trim();

                _outputHelper.WriteLine("Checking {0}...", url);
                    
                var contents = await _httpClient.GetStringOrNullAsync(url);
                var licenseIdentifier = LicenseStrings.TryIdentify(contents ?? string.Empty);

                await writer.WriteLineAsync($"{url};{licenseIdentifier}");
            }

            stopwatch.Stop();
            _outputHelper.WriteLine("Completed in {0}", stopwatch.Elapsed);
        }
    }
}