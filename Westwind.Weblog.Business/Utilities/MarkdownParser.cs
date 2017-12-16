#region License
/*
 **************************************************************
 *  Author: Rick Strahl 
 *          © West Wind Technologies, 2016
 *          http://www.west-wind.com/
 * 
 * Created: 04/28/2016
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 **************************************************************  
*/
#endregion

using Markdig;
using Westwind.Utilities;

namespace Westwind.Weblog.Business
{

    /// <summary>
    /// Wrapper around the CommonMark.NET parser that provides a cached
    /// instance of the Markdown parser. Hooks up custom processing.
    /// </summary>
    public class MarkdownParserMarkdig : IMarkdownParser
    {
        public static MarkdownPipeline Pipeline;

        public MarkdownParserMarkdig()
        {
            if (Pipeline == null)
            {
                Pipeline = new MarkdownPipelineBuilder()
                //.UsePipeTables()
                //.UseAutoLinks()
                //.UseCitations()
                //.UseEmphasisExtras()
                //.Build();
                //.UseDiagrams()
                .UseEmojiAndSmiley()
                .UseAdvancedExtensions()
                .Build();
            }
        }

        /// <summary>
        /// Parses the actual markdown down to html
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public string Parse(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var html = Markdig.Markdown.ToHtml(markdown,Pipeline);
            html = StripScriptBlocks(html);
            html = ParseFontAwesomeIcons(html);
            return html;
        }

        /// <summary>
        /// Post processing routine that post-processes the HTML and 
        /// replaces @icon- with fontawesome icons
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected string ParseFontAwesomeIcons(string html)
        {
            while (true)
            {
                string iconBlock = StringUtils.ExtractString(html, "@icon-", " ", false, false, true);
                if (string.IsNullOrEmpty(iconBlock))
                    break;

                string icon = iconBlock.Replace("@icon-", "").Trim();
                html = html.Replace(iconBlock, "<i class=\"fa fa-" + icon + "\"></i> ");
            }
            return html;
        }

        protected string StripScriptBlocks(string html)
        {
            return
                html
                .Replace("<script", "&lt;script")
                .Replace("</script", "&lt;/script")
                .Replace("javascript:", "Javascript:");
        }

    }


    public interface IMarkdownParser
    {
        /// <summary>
        /// Returns parsed markdown
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        string Parse(string markdown);
    }

    /// <summary>
    /// Retrieves an instance of a markdown parser
    /// </summary>
    public static class MarkdownParserFactory
    {


    }

    public static class Markdown
    {

        /// <summary>
        /// Retrieves a cached instance of the markdown parser
        /// </summary>        
        /// <param name="RenderLinksAsExternal"></param>
        /// <returns></returns>
        public static IMarkdownParser GetParser(bool RenderLinksAsExternal = false)
        {
            return new MarkdownParserMarkdig();
        }

        /// <summary>
        /// Parses Markdown to HTML
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public static string Parse(string markdown)
        {
            return GetParser().Parse(markdown);
        }
    }

}

