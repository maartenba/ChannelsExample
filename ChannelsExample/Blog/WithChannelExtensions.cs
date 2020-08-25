using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Open.ChannelExtensions;
using Xunit;
using Xunit.Abstractions;

namespace ChannelsExample.Blog
{
    public class WithChannelExtensions
    {
        private readonly ITestOutputHelper _outputHelper;

        public WithChannelExtensions(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public async Task RunWithChannelsAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var generator = new ThumbnailGenerator();
            
            await Channel
                .CreateBounded<string>(50000)
                .Source(Directory.GetFiles(Constants.PostsDirectory))
                .PipeAsync(
                    maxConcurrency: 2,
                    capacity: 100,
                    transform: async postPath =>
                    {
                        var frontMatter = await generator.ReadFrontMatterAsync(postPath);
                        return (postPath, frontMatter);
                    })
                .Filter(tuple => tuple.Item2 != null)
                .PipeAsync(
                    maxConcurrency: 10,
                    capacity: 20,
                    transform: async tuple =>
                    {
                        var (postPath, frontMatter) = tuple;
                        var cardImage = await generator.CreateImageAsync(frontMatter!);
                        
                        return (postPath, frontMatter, cardImage);
                    })
                .ReadAllAsync(async tuple =>
                {
                    var (postPath, _, cardImage) = tuple;
                    await generator.SaveImageAsync(cardImage, Path.GetFileName(postPath) + ".png");
                });

            stopwatch.Stop();
            _outputHelper.WriteLine("Completed in {0}", stopwatch.Elapsed);
        }
    }
}