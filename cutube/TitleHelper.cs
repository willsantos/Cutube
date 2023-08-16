using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace cutube;

public class TitleHelper
{
    private string Title { get; set; }
    
    public TitleHelper(string title)
    {
        Title = title;
    }

    public static string FormatTitle(string title)
    {
        var formattedTitle = new string(
            title.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c)!= UnicodeCategory.NonSpacingMark)
                .ToArray());

        formattedTitle = Regex.Replace(formattedTitle, "[^a-zA-Z0-9\\s]", "");
        
        return formattedTitle;
    }
    
}