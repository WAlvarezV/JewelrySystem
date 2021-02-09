using Pomona.Models.Validations;

namespace Pomona.Models.Models
{
    public class WatchModel : ItemModel
    {
        public string GenderType { get; set; }
        public string CaseNumber { get; set; }

        [OptionSelection(Option = "Marca")]
        public string BrandId { get; set; }
    }
}
