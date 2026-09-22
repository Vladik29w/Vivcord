using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vivcord.Server.Extensions;

namespace Vivcord.Server.Controllers.Main
{
    [ApiController]
    public class ApiMainController : ControllerBase
    {
        protected Guid? CurrentUserId => User.GetUserId();

        protected string? CurrentUserDisplayName => User.GetDisplayName();

        protected IActionResult Problem(List<Error> errors)
        {
            if (errors.Count == 0)
                return Problem();

            if (errors.All(error => error.Type == ErrorType.Validation))
                return ValidationProblem(errors);

            var firstError = errors[0];

            return Problem(firstError);
        }

        protected IActionResult Problem(Error error)
        {
            var statusCode = error.Type switch
            {
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError,
            };

            return Problem(statusCode: statusCode, title: error.Description);
        }

        private IActionResult ValidationProblem(List<Error> errors)
        {
            var modelStateDictionary = new ModelStateDictionary();

            foreach (var error in errors)
            {
                modelStateDictionary.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(modelStateDictionary);
        }
    }
}
