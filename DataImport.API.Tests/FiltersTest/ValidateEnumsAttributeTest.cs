

namespace DataImport.API.Tests.FiltersTest;

public class ValidateEnumsAttributeTest
{
    private enum Color
    {
        Red = 1,
        Green = 2
    }

    private static ActionExecutingContext CreateContext(
        IDictionary<string, object?> arguments,
        ModelStateDictionary? modelState = null)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            modelState ?? new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            arguments,
            controller: new object());
    }

    [Fact]
    public void OnActionExecuting_DefinedEnum_DoesNotSetResult()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["color"] = Color.Red });

        new ValidateEnumsAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_UndefinedEnum_ReturnsBadRequest()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["color"] = (Color)99 });

        new ValidateEnumsAttribute().OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var details = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Contains("color", details.Errors.Keys);
        Assert.Contains("'99' is not a valid Color.", details.Errors["color"]);
    }

    [Fact]
    public void OnActionExecuting_NullArgument_DoesNotSetResult()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["color"] = null });

        new ValidateEnumsAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_NonEnumArguments_DoesNotSetResult()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["id"] = 5,
            ["name"] = "abc"
        });

        new ValidateEnumsAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_InvalidModelState_ReturnsBadRequest()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("name", "Name is required.");
        var context = CreateContext(new Dictionary<string, object?>(), modelState);

        new ValidateEnumsAttribute().OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var details = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Contains("Name is required.", details.Errors["name"]);
    }

    [Fact]
    public void OnActionExecuting_InvalidModelState_SkipsEnumCheck()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("name", "Name is required.");
        var context = CreateContext(
            new Dictionary<string, object?> { ["color"] = (Color)99 },
            modelState);

        new ValidateEnumsAttribute().OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var details = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.DoesNotContain("color", details.Errors.Keys);
    }
}