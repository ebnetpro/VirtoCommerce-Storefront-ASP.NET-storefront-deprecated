﻿using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.StaticContent;
using VirtoCommerce.Storefront.Model.StaticContent.Services;

namespace VirtoCommerce.Storefront.Controllers
{
    [OutputCache(CacheProfile = "StaticContentCachingProfile")]
    public class StaticContentController : StorefrontControllerBase
    {
        private readonly IStaticContentService _staticContentService;

        public StaticContentController(WorkContext context, IStorefrontUrlBuilder urlBuilder, IStaticContentService staticContentService)
            : base(context, urlBuilder)
        {
            _staticContentService = staticContentService;
        }

        public ActionResult GetContentPage(ContentItem page)
        {
            WorkContext.CurrentPageSeo = new SeoInfo
            {
                Language = page.Language,
                MetaDescription = page.Title,
                Title = page.Title,
                Slug = page.Url
            };

            var blogArticle = page as BlogArticle;
            if (blogArticle != null)
            {
                WorkContext.CurrentPageSeo.ImageUrl = blogArticle.ImageUrl;
                WorkContext.CurrentPageSeo.MetaDescription = blogArticle.Excerpt ?? blogArticle.Title;

                WorkContext.CurrentBlogArticle = blogArticle;
                WorkContext.CurrentBlog = WorkContext.Blogs.SingleOrDefault(x => x.Name.EqualsInvariant(blogArticle.BlogName));
                var layout = string.IsNullOrEmpty(blogArticle.Layout) ? WorkContext.CurrentBlog.Layout : blogArticle.Layout;
                return View("article", layout, WorkContext);
            }

            var contentPage = page as ContentPage;
            SetCurrentPage(contentPage);
            return View("page", page.Layout, WorkContext);
        }

        // GET: /pages/{page}
        public ActionResult GetContentPageByName(string page)
        {
            var contentPage = WorkContext.Pages
                .OfType<ContentPage>()
                .Where(x => string.Equals(x.Url, page, StringComparison.OrdinalIgnoreCase))
                .FindWithLanguage(WorkContext.CurrentLanguage);

            if (contentPage != null)
            {
                SetCurrentPage(contentPage);
                return View("page", WorkContext);
            }

            throw new HttpException(404, "Page not found. Page URL: '" + page + "'.");
        }

        // GET: /blogs/{blog}, /blog, /blog/category/category, /blogs/{blog}/category/{category}, /blogs/{blog}/tag/{tag}, /blog/tag/{tag}
        public ActionResult GetBlog(string blog = null, string category = null, string tag = null)
        {
            var context = WorkContext;
            context.CurrentBlog = WorkContext.Blogs.FirstOrDefault();
            if (!string.IsNullOrEmpty(blog))
            {
                context.CurrentBlog = WorkContext.Blogs.FirstOrDefault(x => x.Name.EqualsInvariant(blog));
            }
            WorkContext.CurrentBlogSearchCritera.Category = category;
            WorkContext.CurrentBlogSearchCritera.Tag = tag;
            if (context.CurrentBlog != null)
            {
                context.CurrentPageSeo = new SeoInfo
                {
                    Language = context.CurrentBlog.Language,
                    MetaDescription = context.CurrentBlog.Name,
                    Title = context.CurrentBlog.Name,
                    Slug = context.RequestUrl.AbsolutePath
                };
                return View("blog", context.CurrentBlog.Layout, WorkContext);
            }
            throw new HttpException(404, blog);
        }

        [HttpPost]
        public ActionResult Search(StaticContentSearchCriteria request)
        {
            WorkContext.CurrentStaticSearchCriteria = request;

            var allContentItems = _staticContentService.LoadStoreStaticContent(WorkContext.CurrentStore).ToList();
            var scopedContentItems = allContentItems.Where(i => !string.IsNullOrEmpty(i.Url) && i.Url.StartsWith(request.ContentScope, StringComparison.OrdinalIgnoreCase));
            var matchedContentItems = scopedContentItems.Where(i =>
                !string.IsNullOrEmpty(i.Content) && i.Content.IndexOf(request.Keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                !string.IsNullOrEmpty(i.Title) && i.Title.IndexOf(request.Keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            WorkContext.Pages = new MutablePagedList<ContentItem>(matchedContentItems.Where(x => x.Language.IsInvariant || x.Language == WorkContext.CurrentLanguage));

            return View("search", request.Layout, WorkContext);
        }

        private void SetCurrentPage(ContentPage contentPage)
        {
            WorkContext.CurrentPage = contentPage;
            WorkContext.CurrentPageSeo = new SeoInfo
            {
                Language = contentPage.Language,
                Title = contentPage.Title,
                Slug = contentPage.Permalink
            };
        }
    }
}