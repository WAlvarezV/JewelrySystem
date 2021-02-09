using Microsoft.AspNetCore.Components;
using Pomona.Protos;
using Pomona.Protos.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Shared
{
    public partial class GenericPagination
    {
        private int itemsPerPage = 10;
        [Parameter] public Filter Filter { get; set; }
        [Parameter] public int CurrentPage { get; set; } = 1;
        [Parameter] public int TotalAmountPages { get; set; }
        [Parameter] public int Radius { get; set; } = 3;
        [Parameter] public EventCallback<Pagination> SelectedPage { get; set; }
        List<LinkModel> links;

        [Parameter]
        public int ItemsPerPage
        {
            get { return itemsPerPage; }
            set
            {
                itemsPerPage = value;
                SelectedPage.InvokeAsync(new Pagination { Page = 1, Records = itemsPerPage, Filter = Filter });
            }
        }

        private async Task SelectedPageInternal(LinkModel link)
        {
            if (link.Page == CurrentPage)
            {
                return;
            }

            if (!link.Enabled)
            {
                return;
            }
            CurrentPage = link.Page;
            await SelectedPage.InvokeAsync(new Pagination { Page = CurrentPage, Records = itemsPerPage, Filter = Filter });
        }

        protected override void OnParametersSet()
        {
            LoadPages();
            base.OnParametersSet();
        }

        private void LoadPages()
        {
            links = new List<LinkModel>();
            var isPreviousPageLinkEnabled = CurrentPage != 1;
            var previousPage = CurrentPage - 1;
            links.Add(new LinkModel(previousPage, isPreviousPageLinkEnabled, "Anterior"));

            for (int i = 1; i <= TotalAmountPages; i++)
            {
                if (i >= CurrentPage - Radius && i <= CurrentPage + Radius)
                {
                    links.Add(new LinkModel(i) { Active = CurrentPage == i });
                }
            }

            var isNextPageLinkEnabled = CurrentPage != TotalAmountPages;
            var nextPage = CurrentPage + 1;
            links.Add(new LinkModel(nextPage, isNextPageLinkEnabled, "Siguiente"));
        }

        class LinkModel
        {
            public LinkModel(int page)
                : this(page, true) { }

            public LinkModel(int page, bool enabled)
                : this(page, enabled, page.ToString())
            { }

            public LinkModel(int page, bool enabled, string text)
            {
                Page = page;
                Enabled = enabled;
                Text = text;
            }

            public string Text { get; set; }
            public int Page { get; set; }
            public bool Enabled { get; set; } = true;
            public bool Active { get; set; } = false;
        }
    }
}
