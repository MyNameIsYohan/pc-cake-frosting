using Cake.Core;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;

namespace Overleaf.Cake.Frosting
{
    public partial class BuildContext : FrostingContext
    {
        public ConfigurationModel Config { get; set; }

        public BuildContext(ICakeContext context) : base(context)
        {
            Config = context.GetConfigurationModel();
        }
    }
}
