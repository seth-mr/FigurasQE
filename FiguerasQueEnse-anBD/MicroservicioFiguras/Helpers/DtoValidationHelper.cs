using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MicroservicioFiguras.Helpers;

public static class DtoValidationHelper
{
    public static bool TryValidate<T>(T? dto, out List<string> errors)
    {
        if (dto is null)
        {
            errors = new List<string> { "Request body cannot be null." };
            return false;
        }

        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        errors = validationResults
            .SelectMany(result =>
            {
                var message = result.ErrorMessage ?? "Validation failed";
                return result.MemberNames.Any()
                    ? result.MemberNames.Select(member => $"{member}: {message}")
                    : new[] { message };
            })
            .ToList();
        return isValid;
    }
}
