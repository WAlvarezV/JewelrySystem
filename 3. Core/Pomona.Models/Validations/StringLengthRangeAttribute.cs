using System.ComponentModel.DataAnnotations;

namespace Pomona.Models.Validations
{
    class StringLengthRangeAttribute : ValidationAttribute
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public bool Required { get; set; }

        public StringLengthRangeAttribute()
        {
            Min = 0;
            Max = int.MaxValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string strValue = value as string;
            bool valid = !Required;
            if (!string.IsNullOrWhiteSpace(strValue))
            {
                int len = strValue.Length;
                valid = (len >= Min && len <= Max);
            }

            return valid ? ValidationResult.Success : new ValidationResult($"El valor ingresado debe tener entre {Min} y {Max} caracteres.");
        }
    }
}