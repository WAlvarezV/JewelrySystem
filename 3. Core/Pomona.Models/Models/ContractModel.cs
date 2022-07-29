using Pomona.Models.Shared;
using Pomona.Models.Validations;
using System;
using System.ComponentModel.DataAnnotations;

namespace Pomona.Models.Models
{
    public class ContractModel
    {
        [Required(ErrorMessage = Constant.Required)]
        public int? Number { get; set; }

        public DateTime Date { get; set; } = DateTime.Today;

        public DateTime DeliveryDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = Constant.Required)]
        public int? Value { get; set; }
        public int? Payment { get; set; }

        public int? Balance { get; set; }

        public double? Weight { get; set; }
        [Required(ErrorMessage = Constant.Required)]
        //[RegularExpression(Constant.RegExUpperCase, ErrorMessage = Constant.UpperCaseMssg)]
        [StringLengthRange(Min = 15, Max = 1000, Required = true)]
        public string Description { get; set; }
        // Client Information
        [OptionSelection(Option = "Tipo Identificación")]
        public string IdentificationTypeId { get; set; }
        [Required(ErrorMessage = Constant.Required)]
        public string IdentificationNumber { get; set; }
        [Required(ErrorMessage = Constant.Required)]
        public string FullName { get; set; }
        [Required(ErrorMessage = Constant.Required)]
        public string CellPhone { get; set; }

    }
}
