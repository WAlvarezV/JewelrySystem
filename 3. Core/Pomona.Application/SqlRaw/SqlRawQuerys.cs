namespace Pomona.Application.SqlRaw
{
    internal class SqlRawQuerys
    {
        public static string GetConsolidatedDailyRecordsQuery(string startDate, string endDate)
        {
            var query = $@"SELECT
                            ROW_NUMBER() OVER( ORDER BY  tb.Date) AS Id,
	                        CONVERT(VARCHAR,tb.Date,103) + '' AS Date,
	                        tb.RecordType, 
	                        tb.PaymentMethod,
	                        SUM(tb.Value) as Value
                        FROM (
                        SELECT 
	                        CAST([Date] AS date) AS Date,
	                        RecordType, 
	                        CASE WHEN PaymentMethod ='EFECTIVO' THEN 'EFECTIVO' ELSE 'OTRO' END AS PaymentMethod, 
	                        SUM(Value) as Value
                        FROM DailyRecords
                        WHERE CAST([Date] AS date) BETWEEN '{startDate}' AND '{endDate}'
                        GROUP BY CAST([Date] AS date),RecordType, PaymentMethod) tb
                        GROUP BY tb.Date,tb.RecordType, tb.PaymentMethod";
            return query;
        }
    }
}
