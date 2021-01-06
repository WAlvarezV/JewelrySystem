using System.ComponentModel.DataAnnotations;

namespace Pomona.Models.Validations
{
    class OptionSelectionAttribute : ValidationAttribute
    {
        public string Option { get; set; } = string.Empty;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string strValue = value as string;
            bool valid = false;
            if (!string.IsNullOrWhiteSpace(strValue))
                valid = !strValue.Equals("0");

            return valid ? ValidationResult.Success : new ValidationResult($"Debe seleccionar una opción{(string.IsNullOrWhiteSpace(Option) ? "." : $" de: {Option}.")}");
        }
    }
}