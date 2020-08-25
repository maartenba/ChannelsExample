using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Xunit;
using Xunit.Abstractions;

namespace ChannelsExample.Blog
{
    public class WithChannels
    {
        private readonly ITestOutputHelper _outputHelper;

        public WithChannels(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private async Task Run(int concurrency, Func<Task> action)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < concurrency; i++)
            {
                tasks.Add(action.Invoke());
            }
            await Task.WhenAll(tasks);
        }

        private async Task Run<T>(int concurrency, Func<Task> action, Channel<T> channel)
        {
            await Run(concurrency, action);
            channel.Writer.TryComplete();
        }
        
        [Fact]
        public async Task RunWithChannelsAsync()
        {
            // Channels that will be used
            var postPathsChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = false, SingleWriter = true });
            var frontMatterChannel = Channel.CreateBounded<(string?, FrontMatter?)>(new BoundedChannelOptions(100) { SingleReader = false, SingleWriter = false });
            var imagesChannel = Channel.CreateBounded<(string?, FrontMatter?, Image)>(new BoundedChannelOptions(20) { SingleReader = false, SingleWriter = false });

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var generator = new ThumbnailGenerator();

            var tasks = new List<Task>();
            
            tasks.Add(Run(1, async () =>
            {              
                var postPaths = Directory.GetFiles(Constants.PostsDirectory);
                foreach (var postPath in postPaths)
                {
                    await postPathsChannel.Writer.WriteAsync(postPath);
                }
            }, postPathsChannel));
            
            tasks.Add(Run(2, async () =>
            {              
                while (await postPathsChannel.Reader.WaitToReadAsync())
                {
                    while (postPathsChannel.Reader.TryRead(out var postPath))
                    {
                        var frontMatter = await generator.ReadFrontMatterAsync(postPath);
                        await frontMatterChannel.Writer.WriteAsync((postPath, frontMatter));
                    }
                }
            }, frontMatterChannel));
            
            tasks.Add(Run(10, async () =>
            {              
                while (await frontMatterChannel.Reader.WaitToReadAsync())
                {
                    while (frontMatterChannel.Reader.TryRead(out var tuple))
                    {
                        var (postPath, frontMatter) = tuple;
                        if (frontMatter == null) continue;
                        
                        var cardImage = await generator.CreateImageAsync(frontMatter);
                        
                        await imagesChannel.Writer.WriteAsync((postPath, frontMatter, cardImage));
                    }
                }
            }, imagesChannel));
            
            tasks.Add(Run(1, async () =>
            {              
                while (await imagesChannel.Reader.WaitToReadAsync())
                {
                    while (imagesChannel.Reader.TryRead(out var tuple))
                    {
                        var (postPath, _, cardImage) = tuple;
                        await generator.SaveImageAsync(cardImage, Path.GetFileName(postPath) + ".png");
                    }
                }
            }));

            await Task.WhenAll(tasks);
            
            stopwatch.Stop();
            _outputHelper.WriteLine("Completed in {0}", stopwatch.Elapsed);
        }
    }
}