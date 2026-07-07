using EasyManufacture.Infrastructure.Legacy;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 在 ApiBehavior 推断参数绑定之前，移除 <see cref="ApsCoreEngine"/> 上不应暴露为 HTTP 的 public 方法。
/// </summary>
public sealed class ApsLegacyActionProvider : IApplicationModelProvider
{
    private static readonly HashSet<string> NonEndpointNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "GetConfigForObj",
        "GetJspreadsheetConfigObj",
        "ExportExcel",
        "ExportExcelOLD",
        "OutputClient",
        "SetDataColor",
        "Translation",
        "AbstractXSSFChartSerie",
        "AutoPlan",
        "Index",
        "Initialize",
        "GetRegister",
        "SeRegister",
    };

    // ControllerApplicationModelProvider = -1000；ApiBehavior = -990；其间移除非法 Action
    public int Order => -995;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        foreach (var controller in context.Result.Controllers)
        {
            if (!typeof(ApsCoreEngine).IsAssignableFrom(controller.ControllerType))
                continue;

            RemoveDuplicateBaseClassActions(controller);

            for (var i = controller.Actions.Count - 1; i >= 0; i--)
            {
                if (ShouldHideFromRouting(controller.Actions[i].ActionMethod))
                    controller.Actions.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// LegacyBusiness 与 ApsCoreEngine.Organize 等同名方法会重复注册路由，导致 AmbiguousMatchException。
    /// 子类已声明时，丢弃基类 ApsCoreEngine 上的同名 Action。
    /// </summary>
    private static void RemoveDuplicateBaseClassActions(ControllerModel controller)
    {
        if (controller.ControllerType == typeof(ApsCoreEngine))
            return;

        var declaredOnDerived = new HashSet<string>(
            controller.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !ShouldHideFromRouting(m))
                .Select(m => m.Name),
            StringComparer.OrdinalIgnoreCase);

        if (declaredOnDerived.Count == 0)
            return;

        for (var i = controller.Actions.Count - 1; i >= 0; i--)
        {
            var method = controller.Actions[i].ActionMethod;
            if (method.DeclaringType == typeof(ApsCoreEngine) && declaredOnDerived.Contains(method.Name))
                controller.Actions.RemoveAt(i);
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context) { }

    private static bool ShouldHideFromRouting(MethodInfo method)
    {
        if (NonEndpointNames.Contains(method.Name))
            return true;

        if (method.ReturnType == typeof(void))
            return true;

        var parameters = method.GetParameters();
        if (parameters.Any(p => p.ParameterType.IsByRef))
            return true;

        var complexParamCount = parameters.Count(p =>
            p.ParameterType is { IsByRef: false } &&
            p.ParameterType != typeof(CancellationToken) &&
            !IsSimpleType(p.ParameterType));

        return complexParamCount > 1;
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(Guid);
    }
}
