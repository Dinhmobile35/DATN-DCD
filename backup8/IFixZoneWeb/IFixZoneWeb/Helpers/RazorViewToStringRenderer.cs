using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace IFixZoneWeb.Helpers
{
    public static class RazorViewToStringRenderer
    {
        public static async Task<string> RenderViewAsync(
            Controller controller,
            string viewName,
            object model)
        {
            var serviceProvider = controller.HttpContext.RequestServices;
            var viewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = serviceProvider.GetRequiredService<ITempDataProvider>();

            var actionContext = new ActionContext(
                controller.HttpContext,
                controller.RouteData,
                controller.ControllerContext.ActionDescriptor
            );

            using var sw = new StringWriter();

            var viewResult = viewEngine.FindView(actionContext, viewName, false);
            if (!viewResult.Success)
            {
                throw new FileNotFoundException($"Không tìm thấy view {viewName}");
            }

            var viewDictionary = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(controller.HttpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
