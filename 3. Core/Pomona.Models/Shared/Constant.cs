namespace Pomona.Models.Shared
{
    internal class Constant
    {
        #region Data Anotations Messages
        public const string Format = "El campo no tiene el formato correcto";
        public const string Required = "Este campo es requerido";
        public const string RequiredOption = "Debe selecionar una opción";
        public const string UpperCaseMssg = "Solo letras mayúsculas, No se permiten caracteres especiales.";
        #endregion

        #region Data Anotations Regular Expressions
        public const string RegExUpperCase = @"^[A-ZÑÁÉÍÓÚ\s]+$";
        public const string RegExUserName = @"^[A-ZÑ]+$";
        #endregion
    }
}
