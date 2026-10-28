using Pomona.Models.Shared;
using Pomona.Models.Validations;
using System.ComponentModel.DataAnnotations;

namespace Pomona.Models.Models
{
    public class PersonModel
    {
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
