using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Path = System.IO.Path;

namespace ChannelsExample.Blog
{
    public class ThumbnailGenerator
    {
        private readonly IDeserializer _yamlDeserializer;

        public ThumbnailGenerator()
        {
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public async Task<FrontMatter?> ReadFrontMatterAsync(string postPath)
        {
            var frontMatterYaml = await File.ReadAllTextAsync(postPath);
            if (frontMatterYaml == null) return null;
            
            var temp = frontMatterYaml.Split("---");
            if (temp.Length < 2) return null;

            var frontMatter = _yamlDeserializer
                .Deserialize<FrontMatter>(temp[1]);
                    
            // Cleanup front matter
            frontMatter.Title = frontMatter.Title.Replace("&amp;", "&");
            return frontMatter;
        }

        public Task<Image> CreateImageAsync(FrontMatter frontMatter)
        {
            // Image parameters
            var cardWidth = 876;
            var cardHeight = 438;
            var textPadding = 25;
            var titleSize = 42;
            var authorSize = 28;
            var titleLocation = new PointF(textPadding, cardHeight / 3.6f);
            var authorLocation = new PointF(textPadding, cardHeight / 4 + authorSize * 2);
            var font = Environment.OSVersion.Platform == PlatformID.Unix
                ? SystemFonts.Find("DejaVu Sans")
                : SystemFonts.Find("Segoe UI");
            
            // Create image
            var cardImage = new Image<Rgba32>(cardWidth, cardHeight);
            
            // Draw background image
            DrawImage(cardImage, 0, 0, cardWidth, cardHeight, Image.Load(Constants.BackgroundImage));

            // Title
            DrawText(cardImage, titleLocation.X, titleLocation.Y, cardWidth - textPadding - textPadding - textPadding - textPadding, Color.White, font.CreateFont(titleSize, FontStyle.Bold), 
                frontMatter.Title);
                    
            // Author & date
            DrawText(cardImage, authorLocation.X, authorLocation.Y, cardWidth - textPadding - textPadding - textPadding - textPadding, Color.White, font.CreateFont(authorSize, FontStyle.Italic), 
                (frontMatter.Author ?? "") + (frontMatter.Date?.ToString(" | MMMM dd, yyyy", CultureInfo.InvariantCulture) ?? ""));

            return Task.FromResult(cardImage as Image);
        }

        public async Task SaveImageAsync(Image image, string fileName)
        {
            await using var outputStream = File.OpenWrite(Path.Combine(Constants.OutputDirectory, fileName));
            image.Save(outputStream, PngFormat.Instance);
        }

        private static void DrawImage(Image image, float x, float y, int width, int height, Image other)
        {
            other.Mutate(ctx =>
                ctx.Resize(width, height));

            image.Mutate(ctx => 
                ctx.DrawImage(other, new Point(0, 0), opacity: 1f));
        }

        private static void DrawText(Image image, float x, float y, int width, Color color, Font font, string text)
        {
            var textGraphicsOptions = new TextGraphicsOptions();
            textGraphicsOptions.GraphicsOptions.Antialias = true;
            textGraphicsOptions.TextOptions.WrapTextWidth = width;

            var location = new PointF(x, y);
            
            image.Mutate(ctx => ctx
                .DrawText(textGraphicsOptions, text, font, color, location));
        }
    }
}