using Microsoft.ML;

namespace Tuvankienthuc.ML
{
    public class ModelDeployer
    {
        public string Deploy(ITransformer model)
        {
            var path = $"MLModels/model_{DateTime.Now:yyyyMMddHHmm}.zip";
            return path;
        }
    }

}
