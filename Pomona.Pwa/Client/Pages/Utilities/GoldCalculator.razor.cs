using Pomona.Pwa.Client.Custom;
using System;
using System.Threading.Tasks;

namespace Pomona.Pwa.Client.Pages.Utilities
{
    public class GoldCalculatorBase : CustomComponentBase
    {
        public double BaseValue { get; set; }
        public double DryWeight { get; set; }
        public double WetWeight { get; set; }
        public string Result { get; set; }

        public async Task CalculateLaw()
        {
            var law = (((DryWeight - WetWeight) * 23.03) / DryWeight) - 2.1912;
            var result = ((law * -1000) - 10).ToString().Substring(0, 3);
            var gram = (Math.Round((BaseValue / 1000) * (law * -1), 0, MidpointRounding.ToZero) - 5) * 1000;
            var total = Math.Round((gram) * DryWeight, 0, MidpointRounding.ToEven).ToString("C0");
            Result = @$"<h4>Resultado Ley Oro</h4>
                        Ley aproximada: <b>{result}</b>
                        <br/>Valor gramo sugerido de compra: <b>${gram:C0}</b>
                        <br/>Valor a pagar: <b>${total}</b>";

        }

    }
}
