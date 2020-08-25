using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Path = System.IO.Path;

namespace ChannelsExample.Blog
{
    public class WithoutChannels
    {
        private readonly ITestOutputHelper _outputHelper;

        public WithoutChannels(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public async Task RunWithoutChannelsAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Get all posts
            var generator = new ThumbnailGenerator();
            var postPaths = Directory.GetFiles(Constants.PostsDirectory);
            foreach (var postPath in postPaths)
            {
                var frontMatter = await generator.ReadFrontMatterAsync(postPath);
                if (frontMatter == null) continue;
                
                var cardImage = await generator.CreateImageAsync(frontMatter);

                await generator.SaveImageAsync(cardImage, Path.GetFileName(postPath) + ".png");
            }

            stopwatch.Stop();
            _outputHelper.WriteLine("Completed in {0}", stopwatch.Elapsed);
        }
        
        [Fact]
        public async Task RunWithoutChannelsButConcurrentAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Get all posts
            var generator = new ThumbnailGenerator();
            var tasks = new List<Task>();
            var postPaths = Directory.GetFiles(Constants.PostsDirectory);
            foreach (var postPath in postPaths)
            {
                tasks.Add(new Func<Task>(async () =>
                {
                    var frontMatter = await generator.ReadFrontMatterAsync(postPath);
                    if (frontMatter == null) return;

                    var cardImage = await generator.CreateImageAsync(frontMatter);

                    await generator.SaveImageAsync(cardImage, Path.GetFileNameWithoutExtension(postPath) + ".png");
                }).Invoke());
            }
            await Task.WhenAll(tasks);

            stopwatch.Stop();
            _outputHelper.WriteLine("Completed in {0}", stopwatch.Elapsed);
        }
    }
}