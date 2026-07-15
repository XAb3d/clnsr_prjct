namespace CleanserBlazorUI;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public class AllowedWordsAttribute : ValidationAttribute
{
    //Roles, Admin
    private readonly string[] _allowedWords;

    public AllowedWordsAttribute(params string[] allowedWords)
    {
        _allowedWords = allowedWords;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult($"The field {validationContext.DisplayName} is required.");
        }

        var input = value.ToString();

        if (!_allowedWords.Contains(input, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidationResult($"The field {validationContext.DisplayName} must be one of the following: {string.Join(", ", _allowedWords)}.");
        }
        //
        return ValidationResult.Success;
    }
}