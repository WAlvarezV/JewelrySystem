namespace JewerlySystem.Common.Application.Emailing;

public interface IEmailTemplateService
{
    string GenerateHtml(string templatePath, Dictionary<string, string> values);
}
