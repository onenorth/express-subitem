using System;
using System.Linq;
using OneNorth.ExpressSubitem.Common;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Pipelines;

namespace OneNorth.ExpressSubitem.Pipelines
{
    public class CloneItem
    {
        /// <summary>
        /// Refresh a newly created clone's ItemAggregate Fields
        /// </summary>
        /// <param name="args"></param>
        public virtual void Execute(CopyItemsArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (args.Copies != null && args.Copies.Any())
            {
                Array.ForEach(args.Copies, Utils.RefreshFieldValues);
            }
        }
    }
}