using Pomona.Models.Shared;
using Pomona.Models.Validations;
using System;
using System.ComponentModel.DataAnnotations;

namespace Pomona.Models.Models
{
    public class ItemModel
    {
        public int Id { get; set; }
        public int Reference { get; set; }

        [Required(ErrorMessage = Constant.Required + ": Valor de Costo")]
        public int CostValue { get; set; }
        public DateTime DateOfEntry { get; set; } = DateTime.Now;
        public DateTime? DateOfSale { get; set; }
        public int SaleValue { get; set; }

        // [Required(ErrorMessage = Constant.Required + ": Observación")]
        // [RegularExpression(Constant.RegExUpperCase, ErrorMessage = Constant.UpperCaseMssg)]
        [StringLengthRange(Min = 15, Max = 1000, Required = true)]
        public string Description { get; set; }

        public bool Active { get; set; } = true;

        public int ItemTypeId { get; set; }

        public int? ProviderId { get; set; }
    }
}
