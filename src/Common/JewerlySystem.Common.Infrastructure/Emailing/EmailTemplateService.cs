using System.Text;
using JewerlySystem.Common.Application.Emailing;

namespace JewerlySystem.Common.Infrastructure.Emailing;

public class EmailTemplateService : IEmailTemplateService
{
    public string GenerateHtml(string templatePath, Dictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            throw new ArgumentException("The template path cannot be null or empty.", nameof(templatePath));
        }

        string fullPath = templatePath;
        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(AppContext.BaseDirectory, templatePath);
        }
        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(AppContext.BaseDirectory, "Emailing", "HtmlTemplates", Path.GetFileName(templatePath));
        }
        if (!File.Exists(fullPath))
        {
            // Fallback to project source relative path if running in dev
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Common", "JewerlySystem.Common.Infrastructure", "Emailing", "HtmlTemplates", Path.GetFileName(templatePath));
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"The HTML template was not found at path: {templatePath} or resolved: {fullPath}");
        }

        string template = File.ReadAllText(fullPath, Encoding.UTF8);

        if (values != null)
        {
            foreach (var kvp in values)
            {
                template = template.Replace("{{" + kvp.Key + "}}", kvp.Value);
            }
        }

        return template;
    }
}
