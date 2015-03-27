using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace OneNorth.ExpressSubitem.Pipelines
{
    public class ExpressSubitemCustomizationsPipeline
    {
        public void Process(PipelineArgs args)
        {
            // bail out if client page isevent
            if (Context.ClientPage.IsEvent)
                return;

            var current = HttpContext.Current;
            if (current == null) 
                return;

            var handler = current.Handler as Page;
            if (handler == null) 
                return;

            Assert.IsNotNull(handler.Header, "Content Editor <head> tag is missing runat='value'");

            handler.Header.Controls.Add(new LiteralControl("<script type='text/javascript' language='javascript' src='/sitecore/shell/OneNorth/include/ExpressSubitem.js'></script>"));
            handler.Header.Controls.Add(new LiteralControl("<script type='text/javascript' language='javascript' src='/sitecore/shell/OneNorth/include/json2.js'></script>"));

            var customStyles = new HtmlLink();
            customStyles.Href = "/sitecore/shell/OneNorth/include/ExpressSubitem.css";
            customStyles.Attributes.Add("rel", "stylesheet");
            customStyles.Attributes.Add("type", "text/css");

            handler.Header.Controls.Add(customStyles);
        }
    }
}