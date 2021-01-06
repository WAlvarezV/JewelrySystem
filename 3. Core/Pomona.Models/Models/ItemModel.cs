using Pomona.Models.Shared;
using Pomona.Models.Validations;
using System.ComponentModel.DataAnnotations;

namespace Pomona.Models.Models
{
    public class ItemModel
    {
        [OptionSelection(Option = "Tipo de Artículo")]
        public string ItemTypeId { get; set; }

        [Required(ErrorMessage = Constant.Required + ": Valor de Costo")]
        public string CostValue { get; set; }


        [Required(ErrorMessage = Constant.Required + ": Observación")]
        [RegularExpression(Constant.RegExUpperCase, ErrorMessage = Constant.UpperCaseMssg)]
        [StringLengthRange(Min = 15, Max = 1000, Required = true)]
        public string Observation { get; set; }
    }
}
