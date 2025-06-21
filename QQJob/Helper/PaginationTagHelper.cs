using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace QQJob.Helper
{
    public class PaginationTagHelper:TagHelper
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchStatus { get; set; }

        public override void Process(TagHelperContext context,TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class","rts__pagination d-block mx-auto pt-40 max-content paging");

            var ul = new TagBuilder("ul");
            ul.AddCssClass("d-flex gap-2");

            // Previous button
            var prevLi = new TagBuilder("li");
            var prevLink = new TagBuilder("a");
            prevLink.AddCssClass(CurrentPage == 1 ? "inactive" : "");
            prevLink.Attributes["onclick"] = CurrentPage > 1
                ? $"pagingSelect({CurrentPage - 1})"
                : "";
            prevLink.InnerHtml.AppendHtml("<i class='rt-chevron-left'></i>");
            prevLi.InnerHtml.AppendHtml(prevLink);
            ul.InnerHtml.AppendHtml(prevLi);

            // Previous page number
            if(CurrentPage > 1)
            {
                var prevPageLi = new TagBuilder("li");
                var prevPageLink = new TagBuilder("a");
                prevPageLink.Attributes["onclick"] = $"pagingSelect({CurrentPage - 1})";
                prevPageLink.InnerHtml.Append((CurrentPage - 1).ToString());
                prevPageLi.InnerHtml.AppendHtml(prevPageLink);
                ul.InnerHtml.AppendHtml(prevPageLi);
            }

            // Current page
            var currentPageLi = new TagBuilder("li");
            var currentPageLink = new TagBuilder("a");
            currentPageLink.AddCssClass("active");
            currentPageLink.InnerHtml.Append(CurrentPage.ToString());
            currentPageLi.InnerHtml.AppendHtml(currentPageLink);
            ul.InnerHtml.AppendHtml(currentPageLi);

            // Next page number
            if(CurrentPage < TotalPages)
            {
                var nextPageLi = new TagBuilder("li");
                var nextPageLink = new TagBuilder("a");
                nextPageLink.Attributes["onclick"] = $"pagingSelect({CurrentPage + 1})";
                nextPageLink.InnerHtml.Append((CurrentPage + 1).ToString());
                nextPageLi.InnerHtml.AppendHtml(nextPageLink);
                ul.InnerHtml.AppendHtml(nextPageLi);
            }

            // Next button
            var nextLi = new TagBuilder("li");
            var nextLink = new TagBuilder("a");
            nextLink.AddCssClass(CurrentPage == TotalPages ? "inactive" : "");
            nextLink.Attributes["onclick"] = CurrentPage < TotalPages
                ? $"pagingSelect({CurrentPage + 1})"
                : "";
            nextLink.InnerHtml.AppendHtml("<i class='rt-chevron-right'></i>");
            nextLi.InnerHtml.AppendHtml(nextLink);
            ul.InnerHtml.AppendHtml(nextLi);

            // Add UL to DIV
            output.Content.AppendHtml(ul);
        }
    }
}
