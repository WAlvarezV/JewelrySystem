using Pomona.Protos.Cash;
using Pomona.Pwa.Client.Custom;
using Pomona.Pwa.Shared;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Cash
{
    public class ConsolidatedRecordsBase : CustomComponentBase
    {
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public DateTime MinDate { get; set; } = DateTime.Today;
        public DateTime MaxDate { get; set; } = DateTime.Today;
        public DateTime DateTimeSelected { get; set; }
        public bool ShowConsolidated { get; set; } = true;
        public DailyRecords DailyRecords { get; set; }
        public ConsolidatedRecordsResponse ConsolidatedRecords { get; set; } = new();
        protected override async Task OnInitializedAsync() => await SetInitDates();

        public async Task GetConsolidated()
        {
            ConsolidatedRecords = await Clients.Cash().GetConsolidatedRecordsAsync(new RecordsRequest { StartDate = StartDate.ToString(Constants.DateSqlParse), EndDate = EndDate.ToString(Constants.DateSqlParse) });
        }

        public async Task SetInitDates()
        {
            MinDate = new DateTime(DateTime.Today.Year, 1, 1);
            MaxDate = DateTime.Today;
            StartDate = new DateTime(DateTime.Today.Year, MaxDate.Month, 1);
            EndDate = MaxDate;
            ShowConsolidated = true;
            await GetConsolidated();
        }
        public async Task ShowDailyRecordsByDate(ConsolidatedRecordProto record)
        {
            DateTimeSelected = record.Date.ToDateTime();
            var date = DateTimeSelected.ToString(Constants.DateParse);
            DailyRecords = await Clients.Cash().GetDailyRecordsAsync(new RecordsRequest { StartDate = date, EndDate = date });
            ShowConsolidated = false;
        }
    }
}
