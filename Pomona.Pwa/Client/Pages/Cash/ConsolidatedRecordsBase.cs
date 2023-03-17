using Pomona.Protos.Cash;
using Pomona.Pwa.Client.Custom;
using Pomona.Pwa.Shared;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Cash
{
    public class ConsolidatedRecordsBase : CustomComponentBase

    {
        protected override async Task OnInitializedAsync()
        {
            var records = await Clients.Cash().GetDailyRecordsAsync(new RecordsRequest { StartDate = DateTime.Today.ToString(Constants.DateSqlParse), EndDate = DateTime.Today.ToString(Constants.DateSqlParse) });
        }
    }
}
